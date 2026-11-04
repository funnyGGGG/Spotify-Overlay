// Spotify Overlay - funny.games
using System;
using Microsoft.Win32;

static class App
{
    public const string Version = "1.6";
}

// Einstellungen, liegen in der Registry unter HKCU\Software\SpotifyOverlay
static class Config
{
    public const string Key = @"Software\SpotifyOverlay";

    public static string Language = "en";
    public static int Scale = 100;
    public static int Opacity = 84;
    public static bool Popup = false;
    public static bool Volume = true;
    public static bool Discord = true;
    public static bool AutoUpdate = true;
    public static string DiscordId = "";
    // Tastenkuerzel: Lauter, Leiser, Weiter, Zurueck, Play/Pause, Overlay ein/aus
    public static string[] Hk = new string[] { "", "", "", "", "", "" };
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
                    Opacity = GetInt(k, "Opacity", 84);
                    Popup = GetInt(k, "PopupOnly", 0) == 1;
                    Volume = GetInt(k, "ShowVolume", 1) == 1;
                    Discord = GetInt(k, "Discord", 1) == 1;
                    AutoUpdate = GetInt(k, "AutoUpdate", 1) == 1;
                    DiscordId = (k.GetValue("DiscordId") as string) ?? "";
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
                k.SetValue("DiscordId", DiscordId ?? "");
                for (int i = 0; i < Hk.Length; i++) k.SetValue("Hk" + i, Hk[i] ?? "");
                if (X != int.MinValue) k.SetValue("X", X);
                if (Y != int.MinValue) k.SetValue("Y", Y);
            }
        }
        catch { }
    }
}
