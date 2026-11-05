# Spotify Overlay

[English](README.md) | [Deutsch](README.de.md)

Erstellt von **funny.games**

Ein kleines Overlay für Windows, das immer im Vordergrund zeigt, was gerade auf Spotify läuft. Es funktioniert über Spielen im Fenstermodus oder im randlosen Fenster.

## Funktionen
- Song, Interpret, Cover und Fortschrittsbalken
- Zurück / Play-Pause / Weiter - nur für Spotify, auch wenn ein YouTube-Video offen ist
- Auf den Fortschrittsbalken klicken oder ziehen, um im Song vorzuspulen
- Lautstärkeregler, der nur die Lautstärke von Spotify ändert
- Zeigt Werbung an (mit dem Werbebild, wenn Spotify eins liefert)
- Deine Spotify-Playlists mit einem Klick (optional, braucht eine kostenlose Spotify-Entwickler-App)
- Globale Tastenkürzel - jede Taste geht (Lautstärke, Weiter, Play/Pause, Ein-/Ausblenden)
- Mit der Maus verschieben, an der unteren rechten Ecke die Größe ziehen
- Taskleisten-Button mit Live-Vorschau (auch bei Alt+Tab), Tray-Symbol, Einstellungen
- Discord-Aktivität (optional)
- Automatische Updates mit Update-Fenster
- Englisch und Deutsch

## Installation
1. Öffne die **Releases**-Seite dieses Repositories.
2. Lade `SpotifyOverlay_Setup.exe` herunter.
3. Starte die Datei. Wähle die Sprache und den Installationsordner. Andere Programme müssen nicht geschlossen werden.
4. Starte Spotify. Das Overlay erscheint oben rechts.

Ungefährer Verbrauch: ca. 0,2 MB Speicherplatz, 20-40 MB RAM, fast keine CPU.

Windows SmartScreen kann bei nicht signierten Programmen warnen. Auf "Weitere Informationen" und dann "Trotzdem ausführen" klicken.

## Bedienung
| Was | Wie |
| --- | --- |
| Verschieben | Das Overlay ziehen |
| Größe ändern | Die kleine Ecke unten rechts ziehen |
| Im Song vorspulen | Auf den Fortschrittsbalken klicken oder ziehen |
| Spotify-Lautstärke | Regler ziehen (nur Spotify ändert sich) |
| Einstellungen | Zahnrad |
| Playlists | Listen-Symbol |
| Ein-/Ausblenden | Klick auf den Taskleisten-Button oder das Tray-Symbol |
| Beenden | Rechtsklick auf den Taskleisten-Button und Schließen, oder Tray-Menü - Komplett beenden |

In den Einstellungen gibt es einen Button **Hilfe / Anleitung** mit all dem und den Einrichtungsschritten unten.

## Discord-Aktivität (optional)
1. Öffne https://discord.com/developers/applications und klicke auf **New Application**.
2. Kopiere die **Application ID**.
3. Füge sie in den Einstellungen (Zahnrad) bei *Discord-Anwendungs-ID* ein.
4. Die Discord-Desktop-App muss laufen und *Aktuelle Aktivität anzeigen* muss an sein. Die Statuszeile in den Einstellungen zeigt Fehler.

## Playlists (optional)
1. Öffne https://developer.spotify.com/dashboard und erstelle eine App.
2. Trage die Redirect URI `http://127.0.0.1:8898/callback` ein.
3. Kopiere die **Client ID** in die Einstellungen bei *Spotify-Client-ID*.
4. Klicke im Overlay auf das Listen-Symbol und wähle **Verbinden**. Zum Abspielen muss Spotify offen sein, eventuell wird Premium gebraucht.

## Updates
Die App prüft das neueste GitHub-Release und zeigt ein Update-Fenster mit den Änderungen. In den Einstellungen wählst du, ob Updates automatisch installiert werden.

## Hinweise
- Es erscheint nicht über Spielen im exklusiven Vollbild. Nutze Fenster oder randloses Fenster.
- Der Songname wird aus dem Spotify-Fenstertitel gelesen, für die Grundfunktionen ist also keine Anmeldung nötig.

## Selbst bauen
Führe `build.bat` unter Windows aus. Es nutzt den C#-Compiler, der in Windows steckt, und legt den Installer in deinen Downloads-Ordner.

Neue Version veröffentlichen: ein Release mit einem Tag wie `v1.17` erstellen. GitHub baut den Installer und hängt ihn an.

## Lizenz
MIT
