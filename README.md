# Spotify Overlay

[English](README.md) | [Deutsch](README.de.md)

Made by **funny.games**

[![Discord](https://img.shields.io/badge/Discord-Join%20the%20server-5865F2?logo=discord&logoColor=white)](https://discord.gg/QfxEnH2gN9)

A small always-on-top overlay for Windows that shows what is playing on Spotify. It works over games in windowed or borderless mode.

## Features
- Song, artist, cover and progress bar
- Previous / play-pause / next - only for Spotify, even if a YouTube video is open
- Click or drag the progress bar to skip within a song
- Volume slider that only changes Spotify's volume
- Shows ads (with the ad picture when Spotify provides one)
- Your Spotify playlists with one click (optional, needs a free Spotify developer app)
- Global keybinds - every key works (volume, skip, play/pause, show/hide)
- Drag to move, drag the bottom-right corner to resize
- Taskbar button with a live preview (also in Alt+Tab), tray icon, settings window with tabs
- Discord activity (optional)
- Automatic updates with an update window
- English and German

## Install
1. Open the **Releases** page of this repository.
2. Download `SpotifyOverlay_Setup.exe`.
3. Run it. Choose the language and the install folder. You do not have to close other programs.
4. Start Spotify. The overlay appears in the top right corner.

Rough usage: about 0.2 MB on disk, 20-40 MB RAM, almost no CPU.

Windows SmartScreen may warn about unsigned programs. Click "More info" and then "Run anyway".

## How to use
| What | How |
| --- | --- |
| Move | Drag the overlay |
| Resize | Drag the small grip in the bottom-right corner |
| Skip in a song | Click or drag on the progress bar |
| Spotify volume | Drag the slider (only Spotify changes) |
| Settings | Gear icon |
| Playlists | List icon |
| Show / hide | Click the taskbar button or the tray icon |
| Quit | Right-click the taskbar button and choose Close, or tray menu - Quit completely |

The settings window has a **Help / Tutorial** button with all of this and the setup steps below.

## Discord activity
Switch it on in the settings (*Connections*). Discord then shows which song you are listening to and how long it runs. You can turn it off any time. The Discord desktop app must be running and *Display current activity* must be on in Discord. The status line in the settings shows errors.

## Playlists (optional)
1. Go to https://developer.spotify.com/dashboard and create an app.
2. Add the redirect URI `http://127.0.0.1:8898/callback`.
3. Copy the **Client ID** into the settings under *Spotify Client ID*.
4. Click the list icon in the overlay and choose **Connect**. Playing a playlist needs Spotify to be open and may need Premium.

## Updates
The app checks the latest GitHub release and shows an update window with the changes. In the settings you can choose whether updates install automatically.

## Notes
- It does not show over exclusive fullscreen games. Use windowed or borderless.
- The song is read from the Spotify window title, so no login is needed for the basic features.

## Build
Run `build.bat` on Windows. It uses the C# compiler that comes with Windows and puts the installer into your Downloads folder.

To publish a new version: create a release with a tag like `v1.18`. GitHub builds the installer and attaches it.

## License
MIT

## Help and contact
Questions, bugs or ideas? Join the Discord: https://discord.gg/QfxEnH2gN9
The same link is in the app under *Settings -> Help & Contact*.

## Antivirus warning
The installer is not code-signed, so some virus scanners and Windows SmartScreen may flag it by mistake (false positive), as they do with many small free tools. The code is open, you can read it and build it yourself with `build.bat`. Each release also contains `SHA256.txt` to check that the file was not changed. If a scanner flags it, you can report it as a false positive to the vendor or to Microsoft (https://www.microsoft.com/wdsi/filesubmission).
