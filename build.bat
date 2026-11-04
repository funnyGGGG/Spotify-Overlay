@echo off
cd /d "%~dp0"
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
set REFS=/r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Core.dll /r:Microsoft.CSharp.dll
if not exist build mkdir build

"%CSC%" /nologo /target:winexe /optimize+ /codepage:65001 /win32icon:assets\SpotifyOverlay.ico /resource:assets\cover.png,cover.png /resource:assets\repo.txt,repo.txt %REFS% /out:build\SpotifyOverlay.exe src\Overlay.cs src\Settings.cs src\Lang.cs src\Config.cs src\UI.cs src\Audio.cs src\Discord.cs src\Updater.cs src\HotKeys.cs
if errorlevel 1 ( echo Error while building the exe & pause & exit /b 1 )

"%CSC%" /nologo /target:winexe /optimize+ /codepage:65001 /win32icon:assets\SpotifyOverlay.ico /resource:build\SpotifyOverlay.exe,payload.exe /resource:assets\cover.png,cover.png %REFS% /out:build\SpotifyOverlay_Setup.exe installer\Setup.cs src\Lang.cs src\Config.cs src\UI.cs
if errorlevel 1 ( echo Error while building the installer & pause & exit /b 1 )

echo Done: build\SpotifyOverlay_Setup.exe
pause
