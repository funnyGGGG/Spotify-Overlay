// Spotify Overlay - funny.games
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

// Fuer Streamer: Der aktuelle Song als Textdatei (OBS: "Text aus Datei lesen")
// und eine kleine Webseite auf 127.0.0.1 (OBS: "Browser"-Quelle). Alles bleibt auf dem eigenen PC.
static class Streamer
{
    public const int Port = 8899;

    static readonly object gate = new object();
    static string title = "", artist = "";
    static bool playing = false, active = false;
    static double pos = 0, dur = 0;
    static byte[] coverPng = null;
    static int coverVer = 0;
    static string lastText = null;
    static TcpListener listener = null;
    static DateTime nextTry = DateTime.MinValue;

    public static string Url { get { return "http://127.0.0.1:" + Port + "/"; } }

    public static string Folder
    {
        get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SpotifyOverlay"); }
    }

    public static string FilePath { get { return Path.Combine(Folder, "current_song.txt"); } }

    public static bool WebRunning { get { return listener != null; } }

    // Wird vom Overlay 4x pro Sekunde aufgerufen (Oberflaechen-Thread), die Arbeit ist klein
    public static void Update(string t, string a, bool pl, bool act, double p, double d)
    {
        lock (gate)
        {
            title = t ?? "";
            artist = a ?? "";
            playing = pl;
            active = act;
            pos = p;
            dur = d;
        }

        if (Config.StreamFile)
        {
            string text = "";
            if (act)
            {
                string f = string.IsNullOrEmpty(Config.StreamFormat) ? "{artist} - {title}" : Config.StreamFormat;
                text = f.Replace("{artist}", artist).Replace("{title}", title);
            }
            WriteFile(text);
        }

        if (Config.StreamWeb) StartServer();
        else if (listener != null) StopServer();
    }

    public static void SetCover(Image img)
    {
        if (img == null) return;
        try
        {
            using (Bitmap b = new Bitmap(img))
            using (MemoryStream ms = new MemoryStream())
            {
                b.Save(ms, ImageFormat.Png);
                byte[] data = ms.ToArray();
                lock (gate) { coverPng = data; coverVer++; }
            }
        }
        catch { }
    }

    public static void Stop()
    {
        StopServer();
        if (Config.StreamFile) WriteFile("");
    }

    static void WriteFile(string text)
    {
        if (text == lastText) return;
        try
        {
            Directory.CreateDirectory(Folder);
            File.WriteAllText(FilePath, text, new UTF8Encoding(false));
            lastText = text;
        }
        catch { }
    }

    // ---------- kleiner Webserver (nur dieser PC, keine Administrator-Rechte noetig) ----------
    static void StartServer()
    {
        if (listener != null || DateTime.Now < nextTry) return;
        nextTry = DateTime.Now.AddSeconds(5);
        try
        {
            TcpListener l = new TcpListener(IPAddress.Loopback, Port);
            l.Start();
            listener = l;
            Thread t = new Thread(delegate() { Serve(l); });
            t.IsBackground = true;
            t.Start();
        }
        catch { listener = null; }
    }

    static void StopServer()
    {
        TcpListener l = listener;
        listener = null;
        if (l != null) { try { l.Stop(); } catch { } }
    }

    static void Serve(TcpListener l)
    {
        while (listener == l)
        {
            try
            {
                using (TcpClient c = l.AcceptTcpClient())
                {
                    c.ReceiveTimeout = 2000;
                    c.SendTimeout = 2000;
                    NetworkStream ns = c.GetStream();
                    byte[] buf = new byte[4096];
                    int n = 0;
                    try { n = ns.Read(buf, 0, buf.Length); } catch { }
                    if (n <= 0) continue;
                    string first = Encoding.ASCII.GetString(buf, 0, n).Split('\n')[0];
                    string[] parts = first.Split(' ');
                    string path = parts.Length > 1 ? parts[1] : "/";
                    int q = path.IndexOf('?');
                    if (q >= 0) path = path.Substring(0, q);

                    byte[] data;
                    string type;
                    string status = "200 OK";
                    if (path == "/state")
                    {
                        data = Encoding.UTF8.GetBytes(StateJson());
                        type = "application/json; charset=utf-8";
                    }
                    else if (path == "/cover.png")
                    {
                        byte[] cp;
                        lock (gate) { cp = coverPng; }
                        if (cp == null) { data = new byte[0]; type = "text/plain"; status = "404 Not Found"; }
                        else { data = cp; type = "image/png"; }
                    }
                    else
                    {
                        data = Encoding.UTF8.GetBytes(Page);
                        type = "text/html; charset=utf-8";
                    }
                    string head = "HTTP/1.1 " + status + "\r\nContent-Type: " + type + "\r\nContent-Length: " + data.Length +
                                  "\r\nCache-Control: no-store\r\nAccess-Control-Allow-Origin: *\r\nConnection: close\r\n\r\n";
                    byte[] hb = Encoding.ASCII.GetBytes(head);
                    ns.Write(hb, 0, hb.Length);
                    if (data.Length > 0) ns.Write(data, 0, data.Length);
                }
            }
            catch
            {
                if (listener != l) break;
            }
        }
    }

    static string Esc(string s)
    {
        StringBuilder sb = new StringBuilder();
        foreach (char ch in s)
        {
            if (ch == '"') sb.Append("\\\"");
            else if (ch == '\\') sb.Append("\\\\");
            else if (ch < 32) sb.Append("\\u" + ((int)ch).ToString("x4"));
            else sb.Append(ch);
        }
        return sb.ToString();
    }

    static string StateJson()
    {
        string t, a;
        bool pl, act;
        double p, d;
        int cv;
        lock (gate) { t = title; a = artist; pl = playing; act = active; p = pos; d = dur; cv = coverVer; }
        CultureInfo ci = CultureInfo.InvariantCulture;
        return "{\"active\":" + (act ? "true" : "false") +
               ",\"playing\":" + (pl ? "true" : "false") +
               ",\"title\":\"" + Esc(t) + "\"" +
               ",\"artist\":\"" + Esc(a) + "\"" +
               ",\"pos\":" + p.ToString("0.0", ci) +
               ",\"dur\":" + d.ToString("0.0", ci) +
               ",\"cover\":" + cv + "}";
    }

    // Die Webseite fuer die OBS-Browserquelle. Parameter: ?scale=1.5 (Groesse), &bar=0 (ohne Fortschrittsbalken)
    const string Page = @"<!doctype html><html><head><meta charset='utf-8'><title>Spotify Overlay by funny.games</title>
<style>
html,body{margin:0;background:transparent;font-family:'Segoe UI',Arial,sans-serif;color:#fff;overflow:hidden}
#c{box-sizing:border-box;width:340px;padding:12px 14px 8px;background:#181818;border-radius:14px}
#r{display:flex;align-items:center}
#i{width:64px;height:64px;border-radius:10px;object-fit:cover;margin-right:12px;background:#2a2a2a;flex:none}
#m{min-width:0;flex:1}
#t{font-size:16px;font-weight:700;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}
#a{font-size:13px;color:#a5a5a5;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;margin-top:2px}
#s{display:flex;align-items:center;margin-top:6px;font-size:11px;color:#a5a5a5}
#d{width:8px;height:8px;border-radius:50%;background:#1db954;margin-right:6px}
#b{margin-top:10px}
#bar{height:4px;background:#3e3e3e;border-radius:2px}
#fill{height:4px;background:#e87828;border-radius:2px;width:0}
#tm{display:flex;justify-content:space-between;font-size:11px;color:#a5a5a5;margin-top:4px}
#f{text-align:center;font-size:10.5px;color:#7d7d7d;margin-top:8px;letter-spacing:.2px}
</style></head><body>
<div id='c'>
<div id='r'><img id='i'><div id='m'><div id='t'></div><div id='a'></div><div id='s'><span id='d'></span>Spotify</div></div></div>
<div id='b'><div id='bar'><div id='fill'></div></div><div id='tm'><span id='p'>0:00</span><span id='e'>0:00</span></div></div>
<div id='f'>Spotify Overlay by funny.games</div>
</div>
<script>
var q=location.search,ver=-1,pos=0,dur=0,play=false,ts=0;
function $(x){return document.getElementById(x)}
function g(k,d){var m=new RegExp('[?&]'+k+'=([^&]*)').exec(q);return m?decodeURIComponent(m[1]):d}
var z=parseFloat(g('scale','1'));if(z>0&&z!=1)$('c').style.zoom=z;
if(g('bar','1')=='0')$('b').style.display='none';
function fm(s){s=Math.max(0,Math.floor(s));var m=Math.floor(s/60),r=s%60;return m+':'+(r<10?'0':'')+r}
function draw(){var p=pos+(play?(Date.now()-ts)/1000:0);if(dur>0&&p>dur)p=dur;
$('fill').style.width=(dur>0?p/dur*100:0)+'%';$('p').textContent=fm(p);$('e').textContent=fm(dur)}
function tick(){var x=new XMLHttpRequest();x.open('GET','/state?'+Date.now());x.onload=function(){try{
var s=JSON.parse(x.responseText);pos=s.pos;dur=s.dur;play=s.playing;ts=Date.now();
if(!s.active){$('t').textContent='Spotify Overlay';$('a').textContent='Waiting for Spotify ...';
$('i').style.visibility='hidden';$('b').style.visibility='hidden';return}
$('i').style.visibility='visible';$('b').style.visibility='visible';
$('t').textContent=s.title;$('a').textContent=s.artist;
if(s.cover!=ver){ver=s.cover;$('i').src='/cover.png?'+ver}
draw();
}catch(e){}};x.send()}
setInterval(tick,1000);setInterval(draw,250);tick();
</script></body></html>";
}
