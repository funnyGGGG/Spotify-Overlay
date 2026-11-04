// Spotify Overlay - funny.games
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

// Globale Tastenkuerzel (funktionieren auch im Spiel)
static class HotKeys
{
    [DllImport("user32.dll")]
    static extern bool RegisterHotKey(IntPtr h, int id, uint mods, uint vk);
    [DllImport("user32.dll")]
    static extern bool UnregisterHotKey(IntPtr h, int id);

    static bool Parse(string s, out uint mods, out uint vk)
    {
        mods = 0;
        vk = 0;
        if (string.IsNullOrEmpty(s)) return false;
        string[] p = s.Split('+');
        for (int i = 0; i < p.Length - 1; i++)
        {
            if (p[i] == "Alt") mods |= 1;
            else if (p[i] == "Ctrl") mods |= 2;
            else if (p[i] == "Shift") mods |= 4;
        }
        try { vk = (uint)(Keys)Enum.Parse(typeof(Keys), p[p.Length - 1]); }
        catch { return false; }
        return vk != 0;
    }

    public static void Apply(IntPtr handle)
    {
        for (int i = 0; i < Config.Hk.Length; i++)
        {
            UnregisterHotKey(handle, i + 1);
            uint mods, vk;
            if (!Parse(Config.Hk[i], out mods, out vk)) continue;
            if (i >= 2) mods |= 0x4000;   // nur Lauter/Leiser duerfen sich wiederholen
            RegisterHotKey(handle, i + 1, mods, vk);
        }
    }

    public static void Remove(IntPtr handle)
    {
        for (int i = 0; i < Config.Hk.Length; i++) UnregisterHotKey(handle, i + 1);
    }
}
