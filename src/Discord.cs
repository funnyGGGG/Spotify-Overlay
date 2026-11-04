// Spotify Overlay - funny.games
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
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
    bool noType = false;
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

    string Activity(bool withType, string title, string artist, bool playing, double pos, double dur)
    {
        StringBuilder a = new StringBuilder("{");
        if (withType) a.Append("\"type\":2,");
        a.Append("\"details\":\"").Append(Esc(Cut(title))).Append("\"");
        a.Append(",\"state\":\"").Append(Esc(Cut(playing ? artist : Lang.T("dc_paused")))).Append("\"");
        if (playing)
        {
            long start = Now() - (long)pos;
            a.Append(",\"timestamps\":{\"start\":").Append(start);
            if (dur > 0) a.Append(",\"end\":").Append(start + (long)dur);
            a.Append("}");
        }
        a.Append(",\"assets\":{\"large_image\":\"logo\",\"large_text\":\"Spotify Overlay\"}}");
        return a.ToString();
    }

    public void Update(bool on, string clientId, string title, string artist, bool playing, double pos, double dur, bool hasTrack)
    {
        bool idOk = Regex.IsMatch(clientId, "^[0-9]{15,25}$");
        bool active = on && idOk && hasTrack;

        if (!on) Status = "off";
        else if (clientId.Length == 0) Status = "no application ID entered";
        else if (!idOk) Status = "application ID must be only digits (15-25)";
        else if (!hasTrack) Status = "waiting for a song";

        string key = active ? title + "|" + artist + "|" + playing + "|" + clientId : "off";
        long end = (playing && dur > 0) ? Now() + (long)(dur - pos) : 0;

        bool changed = key != lastKey;
        if (!changed && playing && dur > 0 && Math.Abs(end - lastEnd) > 3) changed = true;   // vorgespult
        if (!changed) return;
        if ((DateTime.Now - lastTry).TotalSeconds < 4) return;
        if (busy) return;

        // nur beim Abschalten etwas senden, wenn schon verbunden
        if (!active && pipe == null) { lastKey = key; return; }

        lastTry = DateTime.Now;
        lastKey = key;
        lastEnd = end;
        busy = true;

        string id = clientId;
        string withType = null, without = null;
        if (active)
        {
            withType = Frame(Activity(true, title, artist, playing, pos, dur));
            without = Frame(Activity(false, title, artist, playing, pos, dur));
        }
        else
        {
            withType = without = Frame(null);
        }
        bool isActive = active;
        ThreadPool.QueueUserWorkItem(delegate { Push(id, noType ? without : withType, without, isActive); });
    }

    static string ErrorOf(string reply)
    {
        Match m = Regex.Match(reply, "\"message\"\\s*:\\s*\"([^\"]*)\"");
        return m.Success ? m.Groups[1].Value : reply;
    }

    void Push(string id, string json, string fallback, bool active)
    {
        lock (gate)
        {
            try
            {
                Connect(id);
                Write(1, json);
                string reply = Read();
                if (reply.Contains("\"evt\":\"ERROR\""))
                {
                    if (json != fallback)
                    {
                        // manche Discord-Versionen kennen "type" nicht -> ohne erneut versuchen
                        noType = true;
                        Write(1, fallback);
                        reply = Read();
                    }
                    if (reply.Contains("\"evt\":\"ERROR\"")) throw new IOException(ErrorOf(reply));
                }
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
