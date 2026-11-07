@echo off
cd /d "%~dp0"
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
set REFS=/r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Core.dll /r:System.Security.dll /r:Microsoft.CSharp.dll
set OPTS=/nologo /target:winexe /optimize+ /codepage:65001 /win32icon:assets\SpotifyOverlay.ico /win32manifest:assets\app.manifest
if not exist build mkdir build

"%CSC%" %OPTS% /resource:assets\cover.png,cover.png /resource:assets\repo.txt,repo.txt /resource:assets\discord.txt,discord.txt /resource:assets\discord_app.txt,discord_app.txt %REFS% /out:build\SpotifyOverlay.exe src\Overlay.cs src\Settings.cs src\Lang.cs src\Config.cs src\UI.cs src\Audio.cs src\Discord.cs src\Updater.cs src\HotKeys.cs src\Media.cs src\Json.cs src\SpotifyApi.cs src\Playlists.cs src\UpdateWindow.cs src\Help.cs src\Contact.cs src\Lyrics.cs src\Welcome.cs src\AssemblyInfo.cs
if errorlevel 1 ( echo Error while building the exe & pause & exit /b 1 )

"%CSC%" %OPTS% /resource:build\SpotifyOverlay.exe,payload.exe /resource:assets\cover.png,cover.png %REFS% /out:build\SpotifyOverlay_Setup.exe installer\Setup.cs src\Lang.cs src\Config.cs src\UI.cs src\AssemblyInfo.cs
if errorlevel 1 ( echo Error while building the installer & pause & exit /b 1 )

copy /Y build\SpotifyOverlay_Setup.exe "%USERPROFILE%\Downloads\SpotifyOverlay_Setup.exe" >nul
echo.
echo Done! The installer is in your Downloads folder:
echo %USERPROFILE%\Downloads\SpotifyOverlay_Setup.exe
pause
