// Spotify Overlay - funny.games
using System.Collections.Generic;

// Alle Texte in Englisch (Standard) und Deutsch
static class Lang
{
    public static string Cur = "en";
    static Dictionary<string, string[]> t = new Dictionary<string, string[]>();

    static void A(string key, string en, string de)
    {
        t[key] = new string[] { en, de };
    }

    static Lang()
    {
        // Overlay
        A("idle", "Nothing playing", "Nichts wird abgespielt");
        A("notStarted", "Spotify is not running", "Spotify nicht gestartet");
        A("tray_toggle", "Show / hide overlay", "Overlay ein-/ausblenden");
        A("tray_settings", "Settings", "Einstellungen");
        A("tray_update", "Check for updates", "Nach Updates suchen");
        A("tray_exit", "Quit completely", "Komplett beenden");
        A("upd_new", "Update {0} found - installing...", "Update {0} gefunden - wird installiert...");
        A("upd_avail", "Update {0} is available", "Update {0} ist verfügbar");
        A("upd_none", "You are up to date", "Du bist auf dem neuesten Stand");
        A("upd_fail", "Update check failed", "Update-Suche fehlgeschlagen");
        A("dc_paused", "Paused", "Pausiert");
        A("upd_norepo", "No update repository set", "Kein Update-Repository eingestellt");
        A("dc_status", "Discord status: {0}", "Discord-Status: {0}");
        A("set_keys", "Keybinds (work in games too)", "Tastenkürzel (funktionieren auch im Spiel)");
        A("hk_volup", "Volume up (Spotify only)", "Lauter (nur Spotify)");
        A("hk_voldown", "Volume down (Spotify only)", "Leiser (nur Spotify)");
        A("hk_next", "Next song", "Nächster Song");
        A("hk_prev", "Previous song", "Vorheriger Song");
        A("hk_play", "Play / pause", "Play / Pause");
        A("hk_toggle", "Show / hide overlay", "Overlay ein-/ausblenden");
        A("hk_hint", "Click a box and press the keys. Backspace clears it.", "In ein Feld klicken und die Tasten drücken. Mit Rücktaste löschen.");

        // Einstellungen
        A("set_title", "Settings", "Einstellungen");
        A("set_lang", "Language", "Sprache");
        A("set_opacity", "Transparency", "Transparenz");
        A("set_popup", "Only show as popup when the song changes", "Nur als Popup bei Songwechsel");
        A("set_volume", "Show Spotify volume slider", "Spotify-Lautstärkeregler anzeigen");
        A("set_discord", "Show music in Discord activity", "Musik in der Discord-Aktivität anzeigen");
        A("set_discordid", "Discord application ID", "Discord-Anwendungs-ID");
        A("set_update", "Install updates automatically", "Updates automatisch installieren");
        A("set_auto", "Start with Windows", "Mit Windows starten");
        A("set_tip", "Tip: drag the bottom-right corner of the overlay to resize it.", "Tipp: Zum Ändern der Größe die untere rechte Ecke des Overlays ziehen.");
        A("set_close", "Close", "Schließen");

        // Installer
        A("sub_install", "Installation · Version {0}", "Installation · Version {0}");
        A("sub_uninstall", "Uninstall", "Deinstallation");
        A("lang_label", "Language", "Sprache");
        A("intro", "Always shows the song that is playing on Spotify.", "Zeigt dir immer den aktuellen Spotify-Song an.");
        A("intro2", "You don't need to close any other programs.\nUsage (approx.): 0.2 MB disk · 20–40 MB RAM\nProcessor: practically 0 %", "Du musst dafür keine anderen Programme schließen.\nVerbrauch (ungefähr): ca. 0,2 MB Speicher · ca. 20–40 MB RAM\nProzessor: praktisch 0 %");
        A("folder", "Install folder", "Installationsordner");
        A("browse", "Browse", "Durchsuchen");
        A("browse_desc", "Where should Spotify Overlay be installed?", "Wo soll Spotify Overlay installiert werden?");
        A("chk_desktop", "Create desktop shortcut", "Desktop-Verknüpfung erstellen");
        A("chk_auto", "Start with Windows", "Mit Windows automatisch starten");
        A("chk_discord", "Show music in Discord activity (can be turned off later)", "Musik in der Discord-Aktivität anzeigen (später abschaltbar)");
        A("chk_update", "Install updates automatically", "Updates automatisch installieren");
        A("btn_cancel", "Cancel", "Abbrechen");
        A("btn_install", "Install", "Installieren");
        A("btn_uninstall", "Uninstall", "Deinstallieren");
        A("btn_finish", "Finish", "Fertig");
        A("bad_dir", "Please enter a valid folder.", "Bitte einen gültigen Ordner angeben.");
        A("uninst_q", "Do you really want to remove Spotify Overlay?", "Möchtest du Spotify Overlay wirklich entfernen?");
        A("uninst_text", "The program, shortcuts and autostart entry will be deleted.\nInstalled in: {0}", "Das Programm, die Verknüpfungen und der Autostart-Eintrag werden gelöscht.\nInstalliert in: {0}");
        A("st_kill", "Closing running version ...", "Beende laufende Version ...");
        A("st_dir", "Creating folder ...", "Lege Ordner an ...");
        A("st_copy", "Copying files ...", "Kopiere Programmdateien ...");
        A("st_links", "Creating shortcuts ...", "Erstelle Verknüpfungen ...");
        A("st_reg", "Registering with Windows ...", "Trage in Windows ein ...");
        A("st_done", "Done!", "Fertig!");
        A("st_un1", "Closing Spotify Overlay ...", "Beende Spotify Overlay ...");
        A("st_un2", "Deleting files ...", "Lösche Dateien ...");
        A("st_un3", "Removing shortcuts ...", "Entferne Verknüpfungen ...");
        A("st_un4", "Removing Windows entries ...", "Entferne Windows-Einträge ...");
        A("done_inst", "Done!", "Fertig!");
        A("done_un", "Uninstalled", "Deinstalliert");
        A("done_text", "Spotify Overlay was installed to:\n{0}\n\nTip: drag the bottom-right corner of the overlay to resize it.\nTo quit: right-click the tray icon and choose \"Quit completely\".", "Spotify Overlay wurde installiert nach:\n{0}\n\nTipp: Zum Ändern der Größe die untere rechte Ecke des Overlays ziehen.\nBeenden: Rechtsklick auf das Symbol in der Taskleiste → „Komplett beenden“.");
        A("done_un_text", "Spotify Overlay was removed from this PC.", "Spotify Overlay wurde von diesem PC entfernt.");
        A("run_now", "Start Spotify Overlay now", "Spotify Overlay jetzt starten");
        A("err", "An error occurred:\n{0}", "Es ist ein Fehler aufgetreten:\n{0}");
    }

    public static string T(string key)
    {
        string[] v;
        if (!t.TryGetValue(key, out v)) return key;
        return Cur == "de" ? v[1] : v[0];
    }

    public static string T(string key, params object[] args)
    {
        return string.Format(T(key), args);
    }
}
