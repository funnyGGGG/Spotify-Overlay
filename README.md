# Spotify Overlay

Made by **funny.games**

A small always-on-top overlay for Windows that shows what is playing on Spotify. Works over games in windowed or borderless mode.

## Features
- Song, artist, cover and progress bar
- Previous / play-pause / next
- Volume slider that only changes Spotify's volume
- Global keybinds (volume, skip, play/pause, show/hide)
- Drag to move, drag the bottom-right corner to resize
- Taskbar button, tray icon and a settings window
- Discord activity (optional)
- Automatic updates
- English and German

## Install
Download `SpotifyOverlay_Setup.exe` from the Releases page and run it. You can pick the install folder and the language. Other programs do not have to be closed.

Rough usage: about 0.2 MB on disk, 20-40 MB RAM, almost no CPU.

Windows SmartScreen may warn about unsigned programs. Click "More info" and then "Run anyway".

## Notes
- Does not show over exclusive fullscreen games.
- The song is read from the Spotify window title and the controls use the Windows media keys, so no login is needed.

## Build
Run `build.bat` on Windows. It uses the C# compiler that comes with Windows.

## License
MIT
