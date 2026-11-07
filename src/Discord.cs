// Spotify Overlay - funny.games
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

// Discord Rich Presence ueber die lokale Pipe (discord-ipc-0 bis 9)
class DiscordRpc
{
    // kurzer Status, wird in den Einstellungen angezeigt
    public static string Status = "-";

    NamedPipeClientStream pipe;
    string connectedId = "";
    string lastKey = "";
    long lastEnd = 0;
    DateTime lastTry = DateTime.MinValue;
    bool busy = false;
    int level = 0;
    readonly object gate = new object();

    static string Esc(string s)
    {
        StringBuilder sb = new StringBuilder();
        foreach (char c in s)
        {
            if (c == '\\') sb.Append("\\\\");
            else if (c == '"') sb.Append("\\\"");
            else if (c < 32) sb.Append(' ');
            else sb.Append(c);
        }
        return sb.ToString();
    }

    static string Cut(string s)
    {
        if (s == null) s = "";
        if (s.Length > 120) s = s.Substring(0, 120);
        if (s.Length < 2) s = s + "  ";
        return s;
    }

    static long Now()
    {
        return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
    }

    public void Reset() { lastKey = "?"; }

    string Frame(string activity)
    {
        return "{\"cmd\":\"SET_ACTIVITY\",\"args\":{\"pid\":" + Process.GetCurrentProcess().Id +
               (activity == null ? "" : ",\"activity\":" + activity) + "},\"nonce\":\"" + Guid.NewGuid() + "\"}";
    }

    // Stufe 0: alles, 1: ohne Aktivitaetstyp, 2: ohne Typ und ohne Button (falls eine Discord-Version etwas ablehnt)
    string Activity(int lvl, string title, string artist, bool playing, double pos, double dur, string cover)
    {
        StringBuilder a = new StringBuilder("{");
        if (lvl == 0) a.Append("\"type\":2,");
        a.Append("\"details\":\"").Append(Esc(Cut(title))).Append("\"");
        a.Append(",\"state\":\"").Append(Esc(Cut(playing ? artist : Lang.T("dc_paused")))).Append("\"");
        if (playing)
        {
            long start = Now() - (long)pos;
            a.Append(",\"timestamps\":{\"start\":").Append(start);
            if (dur > 0) a.Append(",\"end\":").Append(start + (long)dur);
            a.Append("}");
        }
        string img = (lvl <= 1 && !string.IsNullOrEmpty(cover)) ? cover : "logo";
        a.Append(",\"assets\":{\"large_image\":\"").Append(Esc(img)).Append("\",\"large_text\":\"").Append(Esc(Cut(title))).Append("\"}");
        string gh = Contact.GitHub;
        if (lvl <= 1 && gh.Length > 0)
            a.Append(",\"buttons\":[{\"label\":\"Get Spotify Overlay\",\"url\":\"").Append(Esc(gh)).Append("\"}]");
        a.Append("}");
        return a.ToString();
    }

    // Die Anwendungs-ID ist oeffentlich und steckt in der Datei discord_app.txt (wird eingebettet).
    // So muss niemand eine eigene Discord-App anlegen. Wer will, kann in den Einstellungen eine eigene eintragen.
    public static string DefaultId
    {
        get
        {
            try
            {
                using (Stream rs = Assembly.GetExecutingAssembly().GetManifestResourceStream("discord_app.txt"))
                {
                    if (rs != null)
                        using (StreamReader r = new StreamReader(rs)) return r.ReadToEnd().Trim();
                }
            }
            catch { }
            return "";
        }
    }

    public void Update(bool on, string clientId, string title, string artist, bool playing, double pos, double dur, bool hasTrack, bool showCover)
    {
        bool idOk = Regex.IsMatch(clientId, "^[0-9]{15,25}$");
        bool active = on && idOk && hasTrack;

        if (!on) Status = "off";
        else if (!idOk) Status = "no valid Discord application ID in this build (see assets/discord_app.txt)";
        else if (!hasTrack) Status = "waiting for a song";

        string key = active ? title + "|" + artist + "|" + playing + "|" + clientId + "|" + showCover : "off";
        long end = (playing && dur > 0) ? Now() + (long)(dur - pos) : 0;

        bool changed = key != lastKey;
        if (!changed && playing && dur > 0 && Math.Abs(end - lastEnd) > 3) changed = true;   // vorgespult
        if (!changed) return;
        if ((DateTime.Now - lastTry).TotalSeconds < 3) return;
        if (busy) return;

        // nur beim Abschalten etwas senden, wenn schon verbunden
        if (!active && pipe == null) { lastKey = key; return; }

        lastTry = DateTime.Now;
        lastKey = key;
        lastEnd = end;
        busy = true;

        string id = clientId;
        bool isActive = active;
        ThreadPool.QueueUserWorkItem(delegate { Push(id, isActive, title, artist, playing, pos, dur, showCover); });
    }

    // ---- Album-Cover (oeffentliche Bild-Adresse) ueber die iTunes-Suche, ohne Anmeldung ----
    static Dictionary<string, string> covers = new Dictionary<string, string>();

    static bool Similar(string a, string b)
    {
        a = (a ?? "").ToLower().Trim();
        b = (b ?? "").ToLower().Trim();
        if (a.Length == 0 || b.Length == 0) return false;
        return a.Contains(b) || b.Contains(a) || a.Substring(0, Math.Min(6, a.Length)) == b.Substring(0, Math.Min(6, b.Length));
    }

    static string CoverFor(string artist, string title)
    {
        string k = artist + "|" + title;
        lock (covers) { string c; if (covers.TryGetValue(k, out c)) return c; }
        string url = "";
        try
        {
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
            HttpWebRequest rq = (HttpWebRequest)WebRequest.Create("https://itunes.apple.com/search?media=music&entity=song&limit=1&term=" + Uri.EscapeDataString(artist + " " + title));
            rq.UserAgent = "SpotifyOverlay";
            rq.Timeout = 4000;
            string json;
            using (WebResponse rs = rq.GetResponse())
            using (StreamReader sr = new StreamReader(rs.GetResponseStream()))
                json = sr.ReadToEnd();
            List<object> res = Json.Get(Json.Parse(json), "results") as List<object>;
            if (res != null && res.Count > 0)
            {
                object r = res[0];
                if (Similar(Json.Str(r, "trackName"), title) && Similar(Json.Str(r, "artistName"), artist))
                    url = Json.Str(r, "artworkUrl100").Replace("100x100bb", "512x512bb");
            }
        }
        catch { }
        lock (covers) { covers[k] = url; }
        return url;
    }

    static string ErrorOf(string reply)
    {
        Match m = Regex.Match(reply, "\"message\"\\s*:\\s*\"([^\"]*)\"");
        return m.Success ? m.Groups[1].Value : reply;
    }

    void Push(string id, bool active, string title, string artist, bool playing, double pos, double dur, bool showCover)
    {
        lock (gate)
        {
            try
            {
                string[] frames = new string[3];
                if (active)
                {
                    string cover = showCover ? CoverFor(artist, title) : "";
                    for (int i = 0; i < 3; i++) frames[i] = Frame(Activity(i, title, artist, playing, pos, dur, cover));
                }
                else frames[0] = frames[1] = frames[2] = Frame(null);

                Connect(id);
                string reply = "";
                bool ok = false;
                for (int l = level; l < 3 && !ok; l++)
                {
                    Write(1, frames[l]);
                    reply = Read();
                    if (!reply.Contains("\"evt\":\"ERROR\"")) { ok = true; level = l; }
                }
                if (!ok) throw new IOException(ErrorOf(reply));
                if (active) Status = "OK - activity sent";
            }
            catch (Exception ex)
            {
                Drop();
                lastKey = "?";   // naechster Versuch
                Status = "error: " + ex.Message;
            }
            busy = false;
        }
    }

    void Connect(string id)
    {
        if (pipe != null && pipe.IsConnected && connectedId == id) return;
        Drop();
        for (int i = 0; i < 10; i++)
        {
            try
            {
                NamedPipeClientStream p = new NamedPipeClientStream(".", "discord-ipc-" + i, PipeDirection.InOut);
                p.Connect(250);
                pipe = p;
                break;
            }
            catch { }
        }
        if (pipe == null) throw new IOException("Discord not found - is the Discord desktop app running?");
        Write(0, "{\"v\":1,\"client_id\":\"" + Esc(id) + "\"}");
        string r = Read();
        if (!r.Contains("READY")) throw new IOException(ErrorOf(r));
        connectedId = id;
    }

    void Write(int op, string json)
    {
        byte[] body = Encoding.UTF8.GetBytes(json);
        byte[] buf = new byte[8 + body.Length];
        BitConverter.GetBytes(op).CopyTo(buf, 0);
        BitConverter.GetBytes(body.Length).CopyTo(buf, 4);
        body.CopyTo(buf, 8);
        pipe.Write(buf, 0, buf.Length);
        pipe.Flush();
    }

    void ReadFull(byte[] buf, int len)
    {
        int got = 0;
        while (got < len)
        {
            int n = pipe.Read(buf, got, len - got);
            if (n <= 0) throw new IOException("Discord closed the connection (wrong application ID?)");
            got += n;
        }
    }

    string Read()
    {
        byte[] head = new byte[8];
        ReadFull(head, 8);
        int len = BitConverter.ToInt32(head, 4);
        if (len < 0 || len > 65536) throw new IOException("invalid reply");
        byte[] body = new byte[len];
        ReadFull(body, len);
        return Encoding.UTF8.GetString(body);
    }

    void Drop()
    {
        try { if (pipe != null) pipe.Dispose(); } catch { }
        pipe = null;
        connectedId = "";
    }

    public void Close()
    {
        try
        {
            lock (gate)
            {
                if (pipe != null && pipe.IsConnected) Write(1, Frame(null));
                Drop();
            }
        }
        catch { }
    }
}
