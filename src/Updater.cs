// Spotify Overlay - funny.games
using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Collections.Generic;

class UpdateInfo
{
    public Version Ver;
    public string Url;
    public string Notes;
}

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

    public static bool Latest(out UpdateInfo info)
    {
        info = null;
        if (!Configured) return false;

        ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;   // TLS 1.2
        HttpWebRequest rq = (HttpWebRequest)WebRequest.Create("https://api.github.com/repos/" + Repo + "/releases/latest");
        rq.UserAgent = "SpotifyOverlay";
        rq.Timeout = 8000;
        string json;
        using (WebResponse rs = rq.GetResponse())
        using (StreamReader sr = new StreamReader(rs.GetResponseStream()))
            json = sr.ReadToEnd();

        object rel = Json.Parse(json);
        string tag = Json.Str(rel, "tag_name").TrimStart('v', 'V');
        string url = null;
        List<object> assets = Json.Get(rel, "assets") as List<object>;
        if (assets != null)
            foreach (object a in assets)
                if (Json.Str(a, "name").EndsWith("Setup.exe", StringComparison.OrdinalIgnoreCase))
                    url = Json.Str(a, "browser_download_url");
        if (url == null || tag.Length == 0) return false;

        info = new UpdateInfo();
        info.Ver = new Version(tag);
        info.Url = url;
        info.Notes = Json.Str(rel, "body");
        return true;
    }

    // Die letzten Versionen (fuer "Version wechseln")
    public static List<UpdateInfo> Recent()
    {
        List<UpdateInfo> list = new List<UpdateInfo>();
        if (!Configured) return list;

        ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
        HttpWebRequest rq = (HttpWebRequest)WebRequest.Create("https://api.github.com/repos/" + Repo + "/releases?per_page=10");
        rq.UserAgent = "SpotifyOverlay";
        rq.Timeout = 8000;
        string json;
        using (WebResponse rs = rq.GetResponse())
        using (StreamReader sr = new StreamReader(rs.GetResponseStream()))
            json = sr.ReadToEnd();

        List<object> rels = Json.Parse(json) as List<object>;
        if (rels == null) return list;
        foreach (object rel in rels)
        {
            try
            {
                string tag = Json.Str(rel, "tag_name").TrimStart('v', 'V');
                string url = null;
                List<object> assets = Json.Get(rel, "assets") as List<object>;
                if (assets != null)
                    foreach (object a in assets)
                        if (Json.Str(a, "name").EndsWith("Setup.exe", StringComparison.OrdinalIgnoreCase))
                            url = Json.Str(a, "browser_download_url");
                if (url == null || tag.Length == 0) continue;
                UpdateInfo item = new UpdateInfo();
                item.Ver = new Version(tag);
                item.Url = url;
                item.Notes = Json.Str(rel, "body");
                list.Add(item);
            }
            catch { }
        }
        return list;
    }

    public static string TempFile()
    {
        return Path.Combine(Path.GetTempPath(), "SpotifyOverlay_Update.exe");
    }
}
