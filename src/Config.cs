// Spotify Overlay - funny.games
using System;
using Microsoft.Win32;

static class App
{
    public const string Version = "1.19";
}

// Einstellungen, liegen in der Registry unter HKCU\Software\SpotifyOverlay
static class Config
{
    public const string Key = @"Software\SpotifyOverlay";

    public static string Language = "en";
    public static int Scale = 100;
    public static int Opacity = 70;
    public static bool Popup = false;
    public static bool Volume = true;
    public static bool Discord = true;
    public static bool AutoUpdate = true;
    public static bool DiscordCover = true;
    public static bool Welcome = false;
    public static bool Follow = false;          // Overlay folgt Spotify (zeigen/verstecken)
    public static bool StreamFile = false;      // Songtext in eine Datei schreiben
    public static bool StreamWeb = false;       // kleine Webseite fuer OBS
    public static string StreamFormat = "{artist} - {title}";
    public static string MyPlaylists = "";      // eigene Playlist-Links (ohne Anmeldung)
    public static bool Dodge = false;           // Ausweich-Modus
    public static bool Waves = false;           // Mini-Wellen
    public static int VolStep = 5;              // Lautstaerke-Schritt der Tastenkuerzel in Prozent
    public static string Profiles = "";         // Spiele-Profile (siehe GameProfiles.cs)

    // Solange ein Spiele-Profil aktiv ist, steht hier der normale Stand.
    // Config.Save() speichert dann den normalen Stand und nicht die Werte des Profils.
    public static bool ProfileOn = false;
    public static int BaseScale = 100, BaseOpacity = 70, BaseX = int.MinValue, BaseY = int.MinValue;
    public static bool BasePopup = false, BaseVolume = true;
    public static string DiscordId = "";
    public static string SpotifyId = "";
    // Tastenkuerzel: Lauter, Leiser, Weiter, Zurueck, Play/Pause, Overlay ein/aus
    public static string[] Hk = new string[] { "", "", "", "", "", "", "", "" };
    // Fenster fuer den Songtext
    public static int LyX = int.MinValue, LyY = int.MinValue, LyW = 380, LyH = 440;
    public static int X = int.MinValue;
    public static int Y = int.MinValue;

    static int GetInt(RegistryKey k, string name, int def)
    {
        object v = k.GetValue(name);
        return (v is int) ? (int)v : def;
    }

    public static void Load()
    {
        try
        {
            using (RegistryKey k = Registry.CurrentUser.OpenSubKey(Key))
            {
                if (k != null)
                {
                    string l = k.GetValue("Language") as string;
                    if (l == "de" || l == "en") Language = l;
                    Scale = GetInt(k, "Scale", 100);
                    Opacity = GetInt(k, "Opacity", 70);
                    Popup = GetInt(k, "PopupOnly", 0) == 1;
                    Volume = GetInt(k, "ShowVolume", 1) == 1;
                    Discord = GetInt(k, "Discord", 1) == 1;
                    AutoUpdate = GetInt(k, "AutoUpdate", 1) == 1;
                    DiscordCover = GetInt(k, "DiscordCover", 1) == 1;
                    Welcome = GetInt(k, "Welcome", 0) == 1;
                    Follow = GetInt(k, "FollowSpotify", 0) == 1;
                    StreamFile = GetInt(k, "StreamFile", 0) == 1;
                    StreamWeb = GetInt(k, "StreamWeb", 0) == 1;
                    StreamFormat = (k.GetValue("StreamFormat") as string) ?? "{artist} - {title}";
                    MyPlaylists = (k.GetValue("MyPlaylists") as string) ?? "";
                    Dodge = GetInt(k, "Dodge", 0) == 1;
                    Waves = GetInt(k, "Waves", 0) == 1;
                    VolStep = GetInt(k, "VolStep", 5);
                    if (VolStep < 1 || VolStep > 25) VolStep = 5;
                    Profiles = (k.GetValue("Profiles") as string) ?? "";
                    LyX = GetInt(k, "LyX", int.MinValue);
                    LyY = GetInt(k, "LyY", int.MinValue);
                    LyW = GetInt(k, "LyW", 380);
                    LyH = GetInt(k, "LyH", 440);
                    DiscordId = (k.GetValue("DiscordId") as string) ?? "";
                    SpotifyId = (k.GetValue("SpotifyId") as string) ?? "";
                    for (int i = 0; i < Hk.Length; i++) Hk[i] = (k.GetValue("Hk" + i) as string) ?? "";
                    X = GetInt(k, "X", int.MinValue);
                    Y = GetInt(k, "Y", int.MinValue);
                }
            }
        }
        catch { }
        Lang.Cur = Language;
    }

    public static void Save()
    {
        int sOp = Opacity, sSc = Scale, sX = X, sY = Y;
        bool sPo = Popup, sVo = Volume;
        if (ProfileOn)
        {
            Opacity = BaseOpacity; Scale = BaseScale; X = BaseX; Y = BaseY; Popup = BasePopup; Volume = BaseVolume;
        }
        try
        {
            using (RegistryKey k = Registry.CurrentUser.CreateSubKey(Key))
            {
                k.SetValue("Language", Language);
                k.SetValue("Scale", Scale);
                k.SetValue("Opacity", Opacity);
                k.SetValue("PopupOnly", Popup ? 1 : 0);
                k.SetValue("ShowVolume", Volume ? 1 : 0);
                k.SetValue("Discord", Discord ? 1 : 0);
                k.SetValue("AutoUpdate", AutoUpdate ? 1 : 0);
                k.SetValue("DiscordCover", DiscordCover ? 1 : 0);
                k.SetValue("Welcome", Welcome ? 1 : 0);
                k.SetValue("FollowSpotify", Follow ? 1 : 0);
                k.SetValue("StreamFile", StreamFile ? 1 : 0);
                k.SetValue("StreamWeb", StreamWeb ? 1 : 0);
                k.SetValue("StreamFormat", StreamFormat ?? "");
                k.SetValue("MyPlaylists", MyPlaylists ?? "");
                k.SetValue("Dodge", Dodge ? 1 : 0);
                k.SetValue("Waves", Waves ? 1 : 0);
                k.SetValue("VolStep", VolStep);
                k.SetValue("Profiles", Profiles ?? "");
                if (LyX != int.MinValue) k.SetValue("LyX", LyX);
                if (LyY != int.MinValue) k.SetValue("LyY", LyY);
                k.SetValue("LyW", LyW);
                k.SetValue("LyH", LyH);
                k.SetValue("DiscordId", DiscordId ?? "");
                k.SetValue("SpotifyId", SpotifyId ?? "");
                for (int i = 0; i < Hk.Length; i++) k.SetValue("Hk" + i, Hk[i] ?? "");
                if (X != int.MinValue) k.SetValue("X", X);
                if (Y != int.MinValue) k.SetValue("Y", Y);
            }
        }
        catch { }
        finally
        {
            Opacity = sOp; Scale = sSc; X = sX; Y = sY; Popup = sPo; Volume = sVo;
        }
    }
}
