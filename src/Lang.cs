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
        A("hk_hint", "Click a box and press any key (with or without Ctrl/Alt/Shift/Win). Backspace clears it.", "In ein Feld klicken und eine beliebige Taste drücken (mit oder ohne Strg/Alt/Umschalt/Win). Rücktaste löscht.");
        // neu in 1.7
        A("ad_title", "Advertisement", "Werbung");
        A("tray_playlists", "Playlists", "Playlists");
        A("set_spid", "Spotify Client ID (for playlists)", "Spotify-Client-ID (für Playlists)");
        A("set_spid_hint", "Redirect URI for your Spotify app: http://127.0.0.1:8898/callback", "Redirect URI für deine Spotify-App: http://127.0.0.1:8898/callback");
        A("set_help", "Help / Tutorial", "Hilfe / Anleitung");
        A("help_title", "Help / Tutorial", "Hilfe / Anleitung");
        A("help_text", "MOVE: drag the overlay with the mouse.\nRESIZE: drag the small grip in the bottom-right corner.\nBUTTONS: previous / play-pause / next control only Spotify, even if a YouTube video is open.\nSEEK: click or drag on the progress bar.\nVOLUME: the slider changes only Spotify's volume.\nPLAYLISTS: the list button shows your Spotify playlists (needs a Spotify developer app, see below).\nSETTINGS: the gear icon - language, transparency, popup mode, keybinds, Discord, updates.\nTASKBAR: the overlay has its own taskbar button. Click it to show / hide. Right-click it and choose Close to quit. The tray icon menu also has \"Quit completely\".\n\nKEYBINDS: Settings -> Keybinds. Click a box and press the keys. Any key works. Keys without Ctrl/Alt/Shift are taken away from all other programs while the overlay runs, so choose carefully.\n\nDISCORD ACTIVITY\n1. Open discord.com/developers/applications and click New Application.\n2. Copy the Application ID.\n3. Paste it into the settings (Discord application ID).\n4. The Discord desktop app must be running and Display current activity must be on. The status line in the settings shows errors.\n\nPLAYLISTS SETUP\n1. Open developer.spotify.com/dashboard and create an app.\n2. Add this Redirect URI: http://127.0.0.1:8898/callback\n3. Copy the Client ID into the settings (Spotify Client ID).\n4. Click the list button and choose Connect. Playing a playlist needs Spotify open (and may need Premium).\n\nUPDATES: the app checks GitHub for new versions and offers to install them.\nADS: during ads the overlay shows Advertisement and the ad picture if Spotify provides it.", "VERSCHIEBEN: Das Overlay mit der Maus ziehen.\nGRÖSSE: Die kleine Ecke unten rechts ziehen.\nTASTEN: Zurück / Play-Pause / Weiter steuern nur Spotify, auch wenn ein YouTube-Video offen ist.\nVORSPULEN: Auf den Fortschrittsbalken klicken oder ziehen.\nLAUTSTÄRKE: Der Regler ändert nur die Lautstärke von Spotify.\nPLAYLISTS: Der Listen-Button zeigt deine Spotify-Playlists (braucht eine Spotify-Entwickler-App, siehe unten).\nEINSTELLUNGEN: Das Zahnrad - Sprache, Transparenz, Popup-Modus, Tastenkürzel, Discord, Updates.\nTASKLEISTE: Das Overlay hat einen eigenen Button in der Taskleiste. Klick blendet es ein/aus. Rechtsklick und Schließen beendet es. Im Tray-Menü gibt es auch \"Komplett beenden\".\n\nTASTENKÜRZEL: Einstellungen -> Tastenkürzel. In ein Feld klicken und die Tasten drücken. Jede Taste geht. Tasten ohne Strg/Alt/Umschalt werden solange das Overlay läuft allen anderen Programmen weggenommen, wähle also mit Bedacht.\n\nDISCORD-AKTIVITÄT\n1. discord.com/developers/applications öffnen und auf New Application klicken.\n2. Die Application ID kopieren.\n3. In den Einstellungen bei Discord-Anwendungs-ID einfügen.\n4. Die Discord-Desktop-App muss laufen und Aktuelle Aktivität anzeigen muss an sein. Die Statuszeile in den Einstellungen zeigt Fehler.\n\nPLAYLISTS EINRICHTEN\n1. developer.spotify.com/dashboard öffnen und eine App erstellen.\n2. Diese Redirect URI eintragen: http://127.0.0.1:8898/callback\n3. Die Client ID in den Einstellungen bei Spotify-Client-ID einfügen.\n4. Auf den Listen-Button klicken und Verbinden wählen. Zum Abspielen muss Spotify offen sein (und es kann Premium nötig sein).\n\nUPDATES: Die App sucht bei GitHub nach neuen Versionen und bietet die Installation an.\nWERBUNG: Bei Werbung zeigt das Overlay Werbung und das Werbebild, falls Spotify eines liefert.");
        A("pl_title", "My playlists", "Meine Playlists");
        A("pl_noid", "Enter your Spotify Client ID in the settings first (gear icon). The help window explains how to get one.", "Trage zuerst deine Spotify-Client-ID in den Einstellungen ein (Zahnrad). Das Hilfe-Fenster erklärt, wie du sie bekommst.");
        A("pl_connect", "Connect Spotify", "Mit Spotify verbinden");
        A("pl_connecting", "Waiting for the login in your browser ...", "Warte auf die Anmeldung im Browser ...");
        A("pl_loading", "Loading ...", "Lade ...");
        A("pl_play", "Play", "Abspielen");
        A("pl_refresh", "Refresh", "Aktualisieren");
        A("pl_logout", "Disconnect", "Trennen");
        A("pl_err", "Error: {0}", "Fehler: {0}");
        A("pl_empty", "No playlists found", "Keine Playlists gefunden");
        A("pl_opened", "Opened in Spotify - press play there.", "In Spotify geöffnet - dort auf Play drücken.");
        A("pl_started", "Playing.", "Wird abgespielt.");
        A("upd_title", "Update available", "Update verfügbar");
        A("upd_versions", "Version {0}  →  {1}", "Version {0}  →  {1}");
        A("upd_now", "Update now", "Jetzt aktualisieren");
        A("upd_later", "Later", "Später");
        A("upd_down", "Downloading ... {0} %", "Lade herunter ... {0} %");
        A("upd_inst", "Installing ...", "Installiere ...");
        A("upd_notes", "What's new", "Neuigkeiten");
        A("sub_update", "Updating to version {0}", "Aktualisiere auf Version {0}");


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
