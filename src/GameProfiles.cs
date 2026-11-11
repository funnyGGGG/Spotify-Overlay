// Spotify Overlay - funny.games
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

// Profil fuer ein Spiel (oder ein anderes Programm)
class GameProfile
{
    public string Exe = "";        // Programmname, z.B. RobloxPlayerBeta - oder mit Fenstertitel: javaw:Minecraft
    public int Scale = 100;
    public int Opacity = 70;
    public bool Popup = false;
    public bool Click = false;
    public bool Volume = true;
    public int X = int.MinValue, Y = int.MinValue;

    public bool HasPos { get { return X != int.MinValue && Y != int.MinValue; } }
}

static class GameProfiles
{
    public static List<GameProfile> Items = new List<GameProfile>();
    public static volatile bool Any = false;

    static int Int(string s, int def)
    {
        int v;
        return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out v) ? v : def;
    }

    static int Clamp(int v, int lo, int hi)
    {
        return v < lo ? lo : (v > hi ? hi : v);
    }

    // liest die Profile aus Config.Profiles (eine Zeile pro Profil, Felder mit Tab getrennt)
    public static void Load()
    {
        List<GameProfile> l = new List<GameProfile>();
        foreach (string raw in (Config.Profiles ?? "").Split('\n'))
        {
            string[] f = raw.TrimEnd('\r').Split('\t');
            if (f.Length < 6 || f[0].Length == 0) continue;
            GameProfile g = new GameProfile();
            g.Exe = f[0];
            g.Scale = Clamp(Int(f[1], 100), 60, 250);
            g.Opacity = Clamp(Int(f[2], 70), 20, 100);
            g.Popup = f[3] == "1";
            g.Click = f[4] == "1";
            g.Volume = f[5] == "1";
            if (f.Length >= 8)
            {
                g.X = Int(f[6], int.MinValue);
                g.Y = Int(f[7], int.MinValue);
            }
            l.Add(g);
        }
        Items = l;
        Any = l.Count > 0;
    }

    // schreibt die Profile nach Config.Profiles (gespeichert wird mit Config.Save)
    public static void Store()
    {
        CultureInfo ci = CultureInfo.InvariantCulture;
        StringBuilder sb = new StringBuilder();
        foreach (GameProfile g in Items)
        {
            sb.Append(g.Exe.Replace('\t', ' ').Replace('\n', ' ').Replace('\r', ' ')).Append('\t');
            sb.Append(g.Scale.ToString(ci)).Append('\t');
            sb.Append(g.Opacity.ToString(ci)).Append('\t');
            sb.Append(g.Popup ? "1" : "0").Append('\t');
            sb.Append(g.Click ? "1" : "0").Append('\t');
            sb.Append(g.Volume ? "1" : "0").Append('\t');
            sb.Append(g.X.ToString(ci)).Append('\t');
            sb.Append(g.Y.ToString(ci)).Append('\n');
        }
        Config.Profiles = sb.ToString();
        Any = Items.Count > 0;
    }

    // Sucht das Profil zum vordersten Fenster. "javaw:Minecraft" passt nur, wenn der Fenstertitel "Minecraft" enthaelt.
    public static GameProfile Find(string exe, string title)
    {
        GameProfile plain = null;
        foreach (GameProfile g in Items)
        {
            string name = g.Exe, part = "";
            int c = name.IndexOf(':');
            if (c > 0) { part = name.Substring(c + 1); name = name.Substring(0, c); }
            if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) name = name.Substring(0, name.Length - 4);
            if (!string.Equals(name, exe, StringComparison.OrdinalIgnoreCase)) continue;
            if (part.Length > 0)
            {
                if (title != null && title.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0) return g;
            }
            else if (plain == null) plain = g;
        }
        return plain;
    }
}
