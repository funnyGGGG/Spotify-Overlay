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
    static byte[] overlayPng = null;
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

    // Das Bild des echten Overlays fuer die OBS-Seite (sieht dadurch genauso aus wie die App)
    public static void SetOverlay(Bitmap bmp)
    {
        if (bmp == null) return;
        try
        {
            using (bmp)
            using (MemoryStream ms = new MemoryStream())
            {
                bmp.Save(ms, ImageFormat.Png);
                byte[] data = ms.ToArray();
                lock (gate) { overlayPng = data; }
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
                    else if (path == "/overlay.png")
                    {
                        byte[] op;
                        lock (gate) { op = overlayPng; }
                        if (op == null) { data = new byte[0]; type = "text/plain"; status = "404 Not Found"; }
                        else { data = op; type = "image/png"; }
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
html,body{margin:0;background:transparent;font-family:'Segoe UI',Arial,sans-serif;overflow:hidden}
#w{display:inline-block}
#o{display:block;border-radius:3.2% / 14%}
#f{text-align:center;font-size:11px;color:#8a8a8a;margin-top:6px;letter-spacing:.2px}
</style></head><body>
<div id='w'><img id='o'><div id='f'>Spotify Overlay by funny.games</div></div>
<script>
var q=location.search,busy=false;
function g(k,d){var m=new RegExp('[?&]'+k+'=([^&]*)').exec(q);return m?decodeURIComponent(m[1]):d}
var z=parseFloat(g('scale','1'));if(z>0&&z!=1)document.getElementById('w').style.zoom=z;
function tick(){if(busy)return;busy=true;var n=new Image();
n.onload=function(){document.getElementById('o').src=n.src;busy=false};
n.onerror=function(){busy=false};
n.src='/overlay.png?'+Date.now()}
setInterval(tick,150);tick();
</script></body></html>";
}
