// Spotify Overlay - funny.games
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
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

    static string Post(string url, string form)
    {
        ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
        using (WebClient wc = new WebClient())
        {
            wc.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
            return wc.UploadString(url, form);
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

    // Oeffnet den Browser, wartet auf die Rueckkehr und holt die Tokens (blockiert, im Hintergrund aufrufen)
    public static void Login(string clientId)
    {
        byte[] rnd = new byte[48];
        using (RandomNumberGenerator rng = RandomNumberGenerator.Create()) rng.GetBytes(rnd);
        string verifier = Base64Url(rnd);
        string challenge;
        using (SHA256 sha = SHA256.Create()) challenge = Base64Url(sha.ComputeHash(Encoding.ASCII.GetBytes(verifier)));

        HttpListener l = new HttpListener();
        l.Prefixes.Add("http://127.0.0.1:8898/");
        l.Start();
        try
        {
            string url = "https://accounts.spotify.com/authorize?response_type=code&client_id=" + Uri.EscapeDataString(clientId) +
                         "&redirect_uri=" + Uri.EscapeDataString(Redirect) + "&scope=" + Uri.EscapeDataString(Scopes) +
                         "&code_challenge_method=S256&code_challenge=" + challenge;
            Process.Start(url);

            IAsyncResult ar = l.BeginGetContext(null, null);
            if (!ar.AsyncWaitHandle.WaitOne(180000)) throw new IOException("login timed out");
            HttpListenerContext ctx = l.EndGetContext(ar);
            string code = ctx.Request.QueryString["code"];
            string err = ctx.Request.QueryString["error"];

            byte[] page = Encoding.UTF8.GetBytes("<html><body style='font-family:sans-serif;background:#121212;color:#fff;text-align:center;padding-top:80px'><h2>Spotify Overlay</h2><p>" +
                                                 (code != null ? "Connected. You can close this tab." : "Login failed.") + "</p></body></html>");
            ctx.Response.ContentType = "text/html; charset=utf-8";
            ctx.Response.OutputStream.Write(page, 0, page.Length);
            ctx.Response.Close();

            if (code == null) throw new IOException(err ?? "login failed");

            string form = "grant_type=authorization_code&code=" + Uri.EscapeDataString(code) +
                          "&redirect_uri=" + Uri.EscapeDataString(Redirect) + "&client_id=" + Uri.EscapeDataString(clientId) +
                          "&code_verifier=" + verifier;
            TakeToken(Post("https://accounts.spotify.com/api/token", form));
        }
        finally { l.Stop(); }
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
            if (r == null) throw;
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
            if (st != 200) throw new IOException(Json.Str(Json.Get(Json.Parse(json), "error"), "message"));
            List<object> items = Json.Get(Json.Parse(json), "items") as List<object>;
            if (items == null || items.Count == 0) break;
            foreach (object it in items)
                if (it != null) list.Add(new KeyValuePair<string, string>(Json.Str(it, "name"), Json.Str(it, "id")));
            if (items.Count < 50) break;
            offset += 50;
        }
        return list;
    }

    // true = wird abgespielt, false = nur in Spotify geoeffnet
    public static bool Play(string playlistId)
    {
        string body = "{\"context_uri\":\"spotify:playlist:" + playlistId + "\"}";
        int st;
        Call("PUT", "https://api.spotify.com/v1/me/player/play", body, out st);
        if (st >= 200 && st < 300) return true;

        // kein aktives Geraet oder kein Premium: Playlist in Spotify oeffnen
        try { Process.Start("spotify:playlist:" + playlistId); } catch { }
        return false;
    }
}
