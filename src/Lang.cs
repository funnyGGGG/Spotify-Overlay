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
        A("hk_hint", "Click a box and press any key (with or without Ctrl/Alt/Shift). Backspace clears it.", "In ein Feld klicken und eine beliebige Taste drücken (mit oder ohne Strg/Alt/Umschalt). Rücktaste löscht.");
        // neu in 1.7
        A("ad_title", "Advertisement", "Werbung");
        A("tray_playlists", "Playlists", "Playlists");
        A("set_spid", "Spotify Client ID (for playlists)", "Spotify-Client-ID (für Playlists)");
        A("set_spid_hint", "Redirect URI for your Spotify app: http://127.0.0.1:8898/callback", "Redirect URI für deine Spotify-App: http://127.0.0.1:8898/callback");
        A("set_help", "Help / Tutorial", "Hilfe / Anleitung");
        A("help_title", "Help / Tutorial", "Hilfe / Anleitung");
        A("help_text", "## Move and resize\nDrag the overlay with the mouse. Drag the small grip in the bottom-right corner to resize it. In the settings you can also use the Size slider and reset the position.\n\n## Controls\nThe buttons (previous, play/pause, next) control only Spotify, even if a YouTube video is open. Click or drag the progress bar to skip. The slider changes only Spotify's volume.\n\n## Lyrics\nThe lyrics button opens a window with the lyrics of the current song and follows the song line by line. Click it again to close. The lyrics are looked up online at lrclib.net when the window is open.\n\n## Playlists\nThe list button shows your Spotify playlists. Setup: 1. Open developer.spotify.com/dashboard and create an app. 2. Add the Redirect URI http://127.0.0.1:8898/callback 3. Copy the Client ID into Settings -> Connections. 4. Click the list button and choose Connect. Without Spotify Premium the playlist is opened in Spotify and you press play there.\n\n## Discord activity\nSwitch it on in Settings -> Connections. Discord then shows the song, the cover and how long it runs. It shows Paused when you pause. The Discord desktop app must be running and Display current activity must be on.\n\n## Keybinds\nSettings -> Controls. Click a box and press the keys. Any key works. Keys without Ctrl/Alt/Shift are taken away from all other programs while the overlay runs, so choose carefully.\n\n## Click-through mode\nMakes the overlay invisible to the mouse, so clicks in the game do not hit it. Set a keybind or use the tray menu. An orange frame shows that it is on.\n\n## Taskbar and tray\nThe overlay has its own taskbar button. Click it to show / hide. Right-click it and choose Close to quit. The tray menu has Quit completely.\n\n## Ads\nDuring ads the overlay shows Advertisement and the ad picture if Spotify provides it.\n\n## Updates\nThe app checks GitHub for new versions and offers to install them.", "## Verschieben und Größe\nDas Overlay mit der Maus ziehen. Die kleine Ecke unten rechts ändert die Größe. In den Einstellungen gibt es auch einen Größen-Regler und die Position lässt sich zurücksetzen.\n\n## Steuerung\nDie Tasten (Zurück, Play/Pause, Weiter) steuern nur Spotify, auch wenn ein YouTube-Video offen ist. Auf den Fortschrittsbalken klicken oder ziehen zum Vorspulen. Der Regler ändert nur die Lautstärke von Spotify.\n\n## Songtext\nDer Songtext-Button öffnet ein Fenster mit dem Text des aktuellen Songs und folgt dem Lied Zeile für Zeile. Nochmal klicken schließt es. Der Text wird bei geöffnetem Fenster online bei lrclib.net gesucht.\n\n## Playlists\nDer Listen-Button zeigt deine Spotify-Playlists. Einrichten: 1. developer.spotify.com/dashboard öffnen und eine App erstellen. 2. Die Redirect URI http://127.0.0.1:8898/callback eintragen. 3. Die Client ID unter Einstellungen -> Verbindungen einfügen. 4. Auf den Listen-Button klicken und Verbinden wählen. Ohne Spotify Premium wird die Playlist in Spotify geöffnet und du drückst dort auf Play.\n\n## Discord-Aktivität\nUnter Einstellungen -> Verbindungen einschalten. Discord zeigt dann Song, Cover und die Restzeit. Bei Pause steht dort Pausiert. Die Discord-Desktop-App muss laufen und Aktuelle Aktivität anzeigen muss an sein.\n\n## Tastenkürzel\nEinstellungen -> Steuerung. In ein Feld klicken und die Tasten drücken. Jede Taste geht. Tasten ohne Strg/Alt/Umschalt werden solange das Overlay läuft allen anderen Programmen weggenommen, wähle also mit Bedacht.\n\n## Durchklickbar-Modus\nMacht das Overlay für die Maus unsichtbar, damit Klicks im Spiel es nicht treffen. Ein Tastenkürzel setzen oder das Tray-Menü nutzen. Ein oranger Rahmen zeigt, dass er an ist.\n\n## Taskleiste und Tray\nDas Overlay hat einen eigenen Button in der Taskleiste. Klick blendet es ein/aus. Rechtsklick und Schließen beendet es. Im Tray-Menü gibt es Komplett beenden.\n\n## Werbung\nBei Werbung zeigt das Overlay Werbung und das Werbebild, falls Spotify eines liefert.\n\n## Updates\nDie App sucht bei GitHub nach neuen Versionen und bietet die Installation an.");
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
        // neu in 1.17 bis 1.21
        A("pl_h401", "Your Spotify login expired. Click Disconnect and connect again.", "Deine Spotify-Anmeldung ist abgelaufen. Auf Trennen klicken und neu verbinden.");
        A("pl_h403", "Spotify says this account is not allowed. In your Spotify app on developer.spotify.com/dashboard open Settings -> User Management and add your Spotify e-mail address.", "Spotify sagt, dieses Konto ist nicht erlaubt. In deiner Spotify-App auf developer.spotify.com/dashboard unter Settings -> User Management deine Spotify-E-Mail-Adresse eintragen.");
        A("pl_hredir", "Check that the Redirect URI in your Spotify app is exactly http://127.0.0.1:8898/callback and that the Client ID is correct.", "Prüfe, ob die Redirect URI in deiner Spotify-App genau http://127.0.0.1:8898/callback lautet und die Client-ID stimmt.");
        A("pl_hport", "Port 8898 is already in use. Close other programs that use it and try again.", "Port 8898 wird schon benutzt. Schließe andere Programme, die ihn nutzen, und versuche es erneut.");
        A("tab_general", "General", "Allgemein");
        A("tab_controls", "Controls", "Steuerung");
        A("tab_connections", "Connections", "Verbindungen");
        A("tab_updates", "Updates", "Updates");
        A("tab_help", "Help & Contact", "Hilfe & Kontakt");
        A("grp_discord", "Discord activity", "Discord-Aktivität");
        A("grp_spotify", "Spotify playlists", "Spotify-Playlists");
        A("set_version", "Installed version: {0}", "Installierte Version: {0}");
        A("set_check", "Check for updates", "Nach Updates suchen");
        A("set_checking", "Checking ...", "Suche läuft ...");
        A("set_repo", "Updates come from: github.com/{0}", "Updates kommen von: github.com/{0}");
        A("set_norepo", "No update source set in this build.", "In dieser Version ist keine Update-Quelle eingestellt.");
        A("set_contact", "Questions, bugs or ideas? Join our Discord!", "Fragen, Fehler oder Ideen? Komm auf unseren Discord!");
        A("set_discord_btn", "Join the Discord", "Zum Discord");
        A("set_github_btn", "GitHub page", "GitHub-Seite");
        A("set_nodiscord", "The Discord link has not been set yet.", "Der Discord-Link ist noch nicht eingestellt.");
        A("set_copy", "Copy", "Kopieren");
        A("set_copied", "Copied", "Kopiert");
        A("step_setup", "Setup", "Einrichtung");
        A("step_install", "Installing", "Installation");
        A("step_done", "Finished", "Fertig");
        A("head_install", "Install Spotify Overlay", "Spotify Overlay installieren");
        A("head_update", "Updating Spotify Overlay", "Spotify Overlay wird aktualisiert");
        A("head_uninstall", "Remove Spotify Overlay", "Spotify Overlay entfernen");
        A("head_working", "Please wait", "Bitte warten");
        A("lbl_version", "Version {0}", "Version {0}");
        // neu in 1.18
        A("set_opacity", "Opacity (lower = more see-through)", "Deckkraft (niedriger = durchsichtiger)");
        A("set_size", "Size", "Größe");
        A("set_resetpos", "Reset position", "Position zurücksetzen");
        A("grp_look", "Appearance", "Aussehen");
        A("grp_behavior", "Behavior", "Verhalten");
        A("set_dccover", "Show the album cover in Discord (looked up online)", "Album-Cover in Discord anzeigen (wird online gesucht)");
        A("hk_click", "Click-through on / off", "Durchklickbar an / aus");
        A("hk_lyrics", "Show / hide lyrics", "Songtext ein-/ausblenden");
        A("tray_click", "Click-through mode", "Durchklickbar-Modus");
        A("tray_lyrics", "Lyrics", "Songtext");
        A("ct_on", "Click-through is ON. Turn it off with your keybind or in the tray menu.", "Durchklickbar ist AN. Ausschalten mit deinem Tastenkürzel oder im Tray-Menü.");
        A("ct_off", "Click-through is off.", "Durchklickbar ist aus.");
        A("ly_title", "Lyrics", "Songtext");
        A("ly_loading", "Loading lyrics ...", "Songtext wird geladen ...");
        A("ly_none", "No lyrics found for this song.", "Für diesen Song wurde kein Songtext gefunden.");
        A("ly_instr", "Instrumental - no lyrics.", "Instrumental - kein Text.");
        A("ly_ad", "No lyrics during ads.", "Während der Werbung gibt es keinen Songtext.");
        A("ly_credit", "Lyrics: lrclib.net", "Songtexte: lrclib.net");
        A("ly_wait", "Waiting for a song ...", "Warte auf einen Song ...");
        A("pl_free", "You do not have Spotify Premium, so playback cannot be started from outside. The playlist was opened in Spotify - press play there.", "Du hast kein Spotify Premium, deshalb kann das Abspielen nicht von außen gestartet werden. Die Playlist wurde in Spotify geöffnet - drücke dort auf Play.");
        A("help_card_title", "Need help?", "Brauchst du Hilfe?");
        A("help_card_text", "Ask on our Discord. Bugs and ideas are welcome.", "Frag auf unserem Discord. Fehler und Ideen sind willkommen.");
        A("set_issue", "Report a problem", "Problem melden");
        A("set_tour", "Show the tour again", "Tour noch einmal zeigen");
        A("set_guide", "Quick guide", "Kurzanleitung");
        A("tour_next", "Next", "Weiter");
        A("tour_back", "Back", "Zurück");
        A("tour_done", "Let's go", "Los geht's");
        A("tour_skip", "Skip", "Überspringen");
        A("tour1_t", "Welcome to Spotify Overlay", "Willkommen bei Spotify Overlay");
        A("tour1_x", "Your Spotify song, always visible - also in games. This short tour shows you the most important things.", "Dein Spotify-Song, immer sichtbar - auch im Spiel. Diese kurze Tour zeigt dir das Wichtigste.");
        A("tour2_t", "Move and resize", "Verschieben und Größe");
        A("tour2_x", "Drag the overlay anywhere you like. Drag the small grip in the bottom-right corner to make it bigger or smaller.", "Zieh das Overlay an eine beliebige Stelle. Die kleine Ecke unten rechts macht es größer oder kleiner.");
        A("tour3_t", "Control Spotify", "Spotify steuern");
        A("tour3_x", "The buttons only control Spotify, even if a YouTube video is open. Click the progress bar to skip, and use the slider for Spotify's volume.", "Die Tasten steuern nur Spotify, auch wenn ein YouTube-Video offen ist. Klick auf den Fortschrittsbalken zum Vorspulen, der Regler ändert nur Spotifys Lautstärke.");
        A("tour4_t", "Lyrics, playlists, settings", "Songtext, Playlists, Einstellungen");
        A("tour4_x", "The icons on the right open the lyrics, your playlists and the settings. There you also find keybinds, click-through mode and Discord activity.", "Die Symbole rechts öffnen den Songtext, deine Playlists und die Einstellungen. Dort findest du auch Tastenkürzel, den Durchklickbar-Modus und die Discord-Aktivität.");
        A("tour5_t", "Need help?", "Hilfe?");
        A("set_save", "Save", "Speichern");
        A("set_saved", "Saved", "Gespeichert");
        A("set_unsaved", "You have unsaved changes. Save them now?", "Du hast ungespeicherte Änderungen. Jetzt speichern?");
        A("tour5_x", "Questions or ideas? Join our Discord. The help and the link are in Settings -> Help & Contact. Updates install automatically.", "Fragen oder Ideen? Komm auf unseren Discord. Die Hilfe und den Link findest du unter Einstellungen -> Hilfe & Kontakt. Updates werden automatisch installiert.");




        // Einstellungen
        A("set_title", "Settings", "Einstellungen");
        A("set_lang", "Language", "Sprache");
        A("set_popup", "Only show as popup when the song changes", "Nur als Popup bei Songwechsel");
        A("set_volume", "Show Spotify volume slider", "Spotify-Lautstärkeregler anzeigen");
        A("set_discord", "Show music in Discord activity", "Musik in der Discord-Aktivität anzeigen");
        A("set_discordid", "Own Discord application ID (optional - leave empty to use the built-in one)", "Eigene Discord-Anwendungs-ID (optional - leer lassen für die eingebaute)");
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
