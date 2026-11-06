// Spotify Overlay - funny.games
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

    public static string GitHub
    {
        get
        {
            return Updater.Configured ? "https://github.com/" + Updater.Repo : "";
        }
    }
}
