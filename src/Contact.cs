// Spotify Overlay - funny.games
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

// Links fuer "Hilfe & Kontakt". Der Discord-Link steht in der Datei discord.txt (wird beim Bauen eingebettet).
static class Contact
{
    static string Read(string name)
    {
        try
        {
            using (Stream rs = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
            {
                if (rs != null)
                    using (StreamReader r = new StreamReader(rs)) return r.ReadToEnd().Trim();
            }
        }
        catch { }
        return "";
    }

    public static string Discord
    {
        get { return Read("discord.txt"); }
    }

    public static bool HasDiscord
    {
        get
        {
            string d = Discord;
            return d.StartsWith("http") && !d.Contains("DEIN-LINK");
        }
    }

    // Freiwillige Spenden: Links stehen in donate.txt, eine Zeile pro Link: Name|https://...
    // Zeilen mit DEIN-LINK werden ignoriert. Solange es keinen echten Link gibt, wird nichts angezeigt.
    public static List<KeyValuePair<string, string>> DonateLinks()
    {
        List<KeyValuePair<string, string>> l = new List<KeyValuePair<string, string>>();
        foreach (string raw in Read("donate.txt").Split('\n'))
        {
            string line = raw.Trim();
            if (line.Length == 0 || line.Contains("DEIN-LINK")) continue;
            string name = "", url = line;
            int bar = line.IndexOf('|');
            if (bar > 0)
            {
                name = line.Substring(0, bar).Trim();
                url = line.Substring(bar + 1).Trim();
            }
            if (!url.StartsWith("http")) continue;
            if (name.Length == 0)
            {
                try { name = new Uri(url).Host; }
                catch { name = url; }
            }
            l.Add(new KeyValuePair<string, string>(name, url));
        }
        return l;
    }

    public static bool HasDonate
    {
        get { return DonateLinks().Count > 0; }
    }

    public static string GitHub
    {
        get
        {
            return Updater.Configured ? "https://github.com/" + Updater.Repo : "";
        }
    }
}
