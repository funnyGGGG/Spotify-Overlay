// Spotify Overlay - funny.games
using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;

// Sucht neue Versionen in den GitHub-Releases
static class Updater
{
    // Repo steht in der Datei repo.txt (wird beim Bauen eingebettet), z.B. "name/spotify-overlay"
    public static string Repo
    {
        get
        {
            try
            {
                using (Stream rs = Assembly.GetExecutingAssembly().GetManifestResourceStream("repo.txt"))
                {
                    if (rs != null)
                        using (StreamReader r = new StreamReader(rs)) return r.ReadToEnd().Trim();
                }
            }
            catch { }
            return "";
        }
    }

    public static bool Configured
    {
        get
        {
            string r = Repo;
            return r.Contains("/") && !r.StartsWith("DEIN");
        }
    }

    public static Version Current
    {
        get { return new Version(App.Version); }
    }

    public static bool Latest(out Version ver, out string url)
    {
        ver = null;
        url = null;
        if (!Configured) return false;

        ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;   // TLS 1.2
        HttpWebRequest rq = (HttpWebRequest)WebRequest.Create("https://api.github.com/repos/" + Repo + "/releases/latest");
        rq.UserAgent = "SpotifyOverlay";
        rq.Timeout = 8000;
        string json;
        using (WebResponse rs = rq.GetResponse())
        using (StreamReader sr = new StreamReader(rs.GetResponseStream()))
            json = sr.ReadToEnd();

        Match tag = Regex.Match(json, "\"tag_name\"\\s*:\\s*\"v?([0-9.]+)\"");
        Match file = Regex.Match(json, "\"browser_download_url\"\\s*:\\s*\"([^\"]+Setup\\.exe)\"");
        if (!tag.Success || !file.Success) return false;
        ver = new Version(tag.Groups[1].Value);
        url = file.Groups[1].Value;
        return true;
    }

    public static string Download(string url)
    {
        ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
        string path = Path.Combine(Path.GetTempPath(), "SpotifyOverlay_Update.exe");
        using (WebClient wc = new WebClient())
        {
            wc.Headers[HttpRequestHeader.UserAgent] = "SpotifyOverlay";
            wc.DownloadFile(url, path);
        }
        return path;
    }
}
