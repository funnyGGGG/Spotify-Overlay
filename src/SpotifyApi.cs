// Spotify Overlay - funny.games
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

// Anmeldung (PKCE) und Playlists ueber die Spotify Web API
static class SpotifyApi
{
    public const string Redirect = "http://127.0.0.1:8898/callback";
    const string Scopes = "playlist-read-private playlist-read-collaborative user-modify-playback-state user-read-playback-state";

    static string accessToken = "";
    static DateTime expires = DateTime.MinValue;

    // ---- Refresh-Token verschluesselt in der Registry ----
    static string LoadRefresh()
    {
        try
        {
            using (RegistryKey k = Registry.CurrentUser.OpenSubKey(Config.Key))
            {
                string v = k == null ? null : k.GetValue("SpAuth") as string;
                if (string.IsNullOrEmpty(v)) return "";
                byte[] raw = ProtectedData.Unprotect(Convert.FromBase64String(v), null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(raw);
            }
        }
        catch { return ""; }
    }

    static void SaveRefresh(string token)
    {
        using (RegistryKey k = Registry.CurrentUser.CreateSubKey(Config.Key))
        {
            if (string.IsNullOrEmpty(token)) { k.DeleteValue("SpAuth", false); return; }
            byte[] enc = ProtectedData.Protect(Encoding.UTF8.GetBytes(token), null, DataProtectionScope.CurrentUser);
            k.SetValue("SpAuth", Convert.ToBase64String(enc));
        }
    }

    public static bool LoggedIn { get { return LoadRefresh().Length > 0; } }

    public static void Logout()
    {
        accessToken = "";
        SaveRefresh("");
    }

    static string Base64Url(byte[] b)
    {
        return Convert.ToBase64String(b).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    static string ReadBody(WebException ex)
    {
        try
        {
            if (ex.Response == null) return ex.Message;
            using (StreamReader sr = new StreamReader(ex.Response.GetResponseStream())) return sr.ReadToEnd();
        }
        catch { return ex.Message; }
    }

    // Fehlertext aus der Antwort von Spotify zusammensetzen
    static string Explain(int status, string body)
    {
        string msg = "";
        try
        {
            object o = Json.Parse(body);
            object err = Json.Get(o, "error");
            if (err is string) msg = (string)err;
            else msg = Json.Str(err, "message");
            string desc = Json.Str(o, "error_description");
            if (desc.Length > 0) msg = msg + " - " + desc;
        }
        catch { }
        if (msg.Length == 0) msg = "HTTP " + status;
        else msg = msg + " (HTTP " + status + ")";

        string hint = "";
        if (status == 401) hint = Lang.T("pl_h401");
        else if (status == 403) hint = Lang.T("pl_h403");
        else if (msg.ToLower().Contains("redirect") || msg.ToLower().Contains("client")) hint = Lang.T("pl_hredir");
        return hint.Length > 0 ? msg + "\n" + hint : msg;
    }

    static string Post(string url, string form)
    {
        ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
        try
        {
            using (WebClient wc = new WebClient())
            {
                wc.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                return wc.UploadString(url, form);
            }
        }
        catch (WebException ex)
        {
            HttpWebResponse r = ex.Response as HttpWebResponse;
            throw new IOException(Explain(r == null ? 0 : (int)r.StatusCode, ReadBody(ex)));
        }
    }

    static void TakeToken(string json)
    {
        object o = Json.Parse(json);
        accessToken = Json.Str(o, "access_token");
        double secs = (Json.Get(o, "expires_in") is double) ? (double)Json.Get(o, "expires_in") : 3600;
        expires = DateTime.Now.AddSeconds(secs - 60);
        string refresh = Json.Str(o, "refresh_token");
        if (refresh.Length > 0) SaveRefresh(refresh);
    }

    static string Query(string line, string key)
    {
        int q = line.IndexOf('?');
        if (q < 0) return null;
        string rest = line.Substring(q + 1);
        int sp = rest.IndexOf(' ');
        if (sp >= 0) rest = rest.Substring(0, sp);
        foreach (string part in rest.Split('&'))
        {
            int eq = part.IndexOf('=');
            if (eq > 0 && part.Substring(0, eq) == key) return Uri.UnescapeDataString(part.Substring(eq + 1).Replace('+', ' '));
        }
        return null;
    }

    // Oeffnet den Browser, wartet auf die Rueckkehr und holt die Tokens (blockiert, im Hintergrund aufrufen).
    // Eigener kleiner Server auf 127.0.0.1:8898 - braucht keine Administrator-Rechte.
    public static void Login(string clientId)
    {
        byte[] rnd = new byte[48];
        using (RandomNumberGenerator rng = RandomNumberGenerator.Create()) rng.GetBytes(rnd);
        string verifier = Base64Url(rnd);
        string challenge;
        using (SHA256 sha = SHA256.Create()) challenge = Base64Url(sha.ComputeHash(Encoding.ASCII.GetBytes(verifier)));

        TcpListener l = new TcpListener(IPAddress.Loopback, 8898);
        try { l.Start(); }
        catch (SocketException) { throw new IOException(Lang.T("pl_hport")); }

        try
        {
            string url = "https://accounts.spotify.com/authorize?response_type=code&client_id=" + Uri.EscapeDataString(clientId) +
                         "&redirect_uri=" + Uri.EscapeDataString(Redirect) + "&scope=" + Uri.EscapeDataString(Scopes) +
                         "&code_challenge_method=S256&code_challenge=" + challenge;
            Process.Start(url);

            string code = null, err = null;
            DateTime end = DateTime.Now.AddSeconds(180);
            while (DateTime.Now < end && code == null && err == null)
            {
                if (!l.Pending()) { Thread.Sleep(100); continue; }
                using (TcpClient c = l.AcceptTcpClient())
                {
                    c.ReceiveTimeout = 3000;
                    NetworkStream ns = c.GetStream();
                    byte[] buf = new byte[8192];
                    int n = 0;
                    try { n = ns.Read(buf, 0, buf.Length); } catch { }
                    if (n <= 0) continue;
                    string req = Encoding.ASCII.GetString(buf, 0, n);
                    string first = req.Split('\n')[0];
                    code = Query(first, "code");
                    err = Query(first, "error");
                    if (code == null && err == null) { Reply(ns, "..."); continue; }
                    Reply(ns, code != null ? "Connected. You can close this tab and go back to the overlay." : "Login failed: " + err);
                }
            }
            if (err != null) throw new IOException(err + "\n" + Lang.T("pl_hredir"));
            if (code == null) throw new IOException("login timed out\n" + Lang.T("pl_hredir"));

            string form = "grant_type=authorization_code&code=" + Uri.EscapeDataString(code) +
                          "&redirect_uri=" + Uri.EscapeDataString(Redirect) + "&client_id=" + Uri.EscapeDataString(clientId) +
                          "&code_verifier=" + verifier;
            TakeToken(Post("https://accounts.spotify.com/api/token", form));
        }
        finally { l.Stop(); }
    }

    static void Reply(NetworkStream ns, string text)
    {
        string html = "<html><body style='font-family:Segoe UI,sans-serif;background:#121212;color:#fff;text-align:center;padding-top:90px'><h2>Spotify Overlay</h2><p>" + text + "</p></body></html>";
        byte[] body = Encoding.UTF8.GetBytes(html);
        byte[] head = Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Type: text/html; charset=utf-8\r\nConnection: close\r\nContent-Length: " + body.Length + "\r\n\r\n");
        ns.Write(head, 0, head.Length);
        ns.Write(body, 0, body.Length);
        ns.Flush();
    }

    static string Token()
    {
        if (accessToken.Length > 0 && DateTime.Now < expires) return accessToken;
        string refresh = LoadRefresh();
        if (refresh.Length == 0) throw new IOException("not connected");
        TakeToken(Post("https://accounts.spotify.com/api/token",
            "grant_type=refresh_token&refresh_token=" + Uri.EscapeDataString(refresh) + "&client_id=" + Uri.EscapeDataString(Config.SpotifyId)));
        return accessToken;
    }

    static string Call(string method, string url, string body, out int status)
    {
        string result = Send(method, url, body, out status);
        if (status == 401)
        {
            // Token abgelaufen: einmal neu holen und wiederholen
            accessToken = "";
            result = Send(method, url, body, out status);
        }
        return result;
    }

    static string Send(string method, string url, string body, out int status)
    {
        ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
        HttpWebRequest rq = (HttpWebRequest)WebRequest.Create(url);
        rq.Method = method;
        rq.Headers["Authorization"] = "Bearer " + Token();
        rq.UserAgent = "SpotifyOverlay";
        rq.Timeout = 10000;
        if (body != null)
        {
            byte[] b = Encoding.UTF8.GetBytes(body);
            rq.ContentType = "application/json";
            rq.ContentLength = b.Length;
            using (Stream s = rq.GetRequestStream()) s.Write(b, 0, b.Length);
        }
        else if (method == "PUT") rq.ContentLength = 0;

        try
        {
            using (HttpWebResponse rs = (HttpWebResponse)rq.GetResponse())
            using (StreamReader sr = new StreamReader(rs.GetResponseStream()))
            {
                status = (int)rs.StatusCode;
                return sr.ReadToEnd();
            }
        }
        catch (WebException ex)
        {
            HttpWebResponse r = ex.Response as HttpWebResponse;
            if (r == null) throw new IOException(ex.Message);
            status = (int)r.StatusCode;
            using (StreamReader sr = new StreamReader(r.GetResponseStream())) return sr.ReadToEnd();
        }
    }

    // Name und ID aller Playlists des Nutzers
    public static List<KeyValuePair<string, string>> Playlists()
    {
        List<KeyValuePair<string, string>> list = new List<KeyValuePair<string, string>>();
        int offset = 0;
        while (offset < 500)
        {
            int st;
            string json = Call("GET", "https://api.spotify.com/v1/me/playlists?limit=50&offset=" + offset, null, out st);
            if (st != 200) throw new IOException(Explain(st, json));
            List<object> items = Json.Get(Json.Parse(json), "items") as List<object>;
            if (items == null || items.Count == 0) break;
            foreach (object it in items)
                if (it != null) list.Add(new KeyValuePair<string, string>(Json.Str(it, "name"), Json.Str(it, "id")));
            if (items.Count < 50) break;
            offset += 50;
        }
        return list;
    }

    // 0 = wird abgespielt, 1 = kein Premium (Playlist in Spotify geoeffnet), 2 = nur in Spotify geoeffnet
    public static int Play(string playlistId)
    {
        string body = "{\"context_uri\":\"spotify:playlist:" + playlistId + "\"}";
        int st;
        string reply = Call("PUT", "https://api.spotify.com/v1/me/player/play", body, out st);
        if (st >= 200 && st < 300) return 0;

        // kein Premium, kein aktives Geraet o.ae.: Playlist in Spotify oeffnen
        try { Process.Start("spotify:playlist:" + playlistId); } catch { }
        return reply.Contains("PREMIUM_REQUIRED") ? 1 : 2;
    }
}
