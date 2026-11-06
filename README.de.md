# Spotify Overlay

[English](README.md) | [Deutsch](README.de.md)

Erstellt von **funny.games**

[![Discord](https://img.shields.io/badge/Discord-Join%20the%20server-5865F2?logo=discord&logoColor=white)](https://discord.gg/QfxEnH2gN9)

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

## Discord-Aktivität
In den Einstellungen (*Verbindungen*) einschalten. Discord zeigt dann, welchen Song du hörst und wie lange er noch läuft. Du kannst es jederzeit ausschalten. Die Discord-Desktop-App muss laufen und in Discord muss *Aktuelle Aktivität anzeigen* an sein. Die Statuszeile in den Einstellungen zeigt Fehler.

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

Neue Version veröffentlichen: ein Release mit einem Tag wie `v1.18` erstellen. GitHub baut den Installer und hängt ihn an.

## Lizenz
MIT

## Hilfe und Kontakt
Fragen, Fehler oder Ideen? Komm auf den Discord: https://discord.gg/QfxEnH2gN9
Derselbe Link steht in der App unter *Einstellungen -> Hilfe & Kontakt*.

## Virenwarnung
Der Installer ist nicht digital signiert. Deshalb melden manche Virenscanner und Windows SmartScreen ihn fälschlich als Bedrohung (Fehlalarm), wie bei vielen kleinen kostenlosen Programmen. Der Code ist offen, du kannst ihn lesen und mit `build.bat` selbst bauen. Zu jedem Release gehört außerdem `SHA256.txt`, damit du prüfen kannst, dass die Datei nicht verändert wurde. Wird sie gemeldet, kannst du sie beim Hersteller oder bei Microsoft als Fehlalarm melden (https://www.microsoft.com/wdsi/filesubmission).
