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
    const string Page = @"<!doctype html><html><head><meta charset='utf-8'><title>Spotify Overlay</title>
<style>
html,body{margin:0;background:transparent;font-family:'Segoe UI',Arial,sans-serif;color:#fff;overflow:hidden}
#c{display:flex;align-items:center;padding:12px;background:rgba(24,24,24,.82);border-radius:14px;width:fit-content;max-width:96vw;transition:opacity .4s}
#c.off{opacity:0}
img{width:72px;height:72px;border-radius:10px;object-fit:cover;margin-right:14px}
#t{font-size:20px;font-weight:700;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;max-width:420px}
#a{font-size:15px;color:#b3b3b3;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;max-width:420px}
#bar{height:4px;background:#404040;border-radius:2px;margin-top:8px}
#fill{height:4px;background:#1db954;border-radius:2px;width:0}
</style></head><body>
<div id='c' class='off'><img id='i'><div><div id='t'></div><div id='a'></div><div id='bar'><div id='fill'></div></div></div></div>
<script>
var q=location.search,ver=-1;
function g(k,d){var m=new RegExp('[?&]'+k+'=([^&]*)').exec(q);return m?decodeURIComponent(m[1]):d}
var z=parseFloat(g('scale','1'));if(z>0&&z!=1)document.getElementById('c').style.zoom=z;
if(g('bar','1')=='0')document.getElementById('bar').style.display='none';
function tick(){var x=new XMLHttpRequest();x.open('GET','/state?'+Date.now());x.onload=function(){try{
var s=JSON.parse(x.responseText),c=document.getElementById('c');
if(!s.active){c.className='off';return}
document.getElementById('t').textContent=s.title;
document.getElementById('a').textContent=s.artist;
if(s.cover!=ver){ver=s.cover;document.getElementById('i').src='/cover.png?'+ver}
document.getElementById('fill').style.width=(s.dur>0?Math.min(100,s.pos/s.dur*100):0)+'%';
c.className='';
}catch(e){}};x.send()}
setInterval(tick,1000);tick();
</script></body></html>";
}
