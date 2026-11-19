// Spotify Overlay - funny.games
using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

// Steuerung und Infos ueber die Windows-Mediensitzung von Spotify (per Reflection)
// So wirken die Tasten nur auf Spotify und nicht auf z.B. ein YouTube-Video im Browser.
static class Media
{
    static Assembly rt;
    static Type ext, streamExt, mgrT, propT, strT;

    static void Init()
    {
        if (ext != null) return;
        rt = Assembly.Load("System.Runtime.WindowsRuntime, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
        streamExt = rt.GetType("System.IO.WindowsRuntimeStreamExtensions");
        mgrT = Type.GetType("Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager, Windows.Media.Control, ContentType=WindowsRuntime");
        propT = Type.GetType("Windows.Media.Control.GlobalSystemMediaTransportControlsSessionMediaProperties, Windows.Media.Control, ContentType=WindowsRuntime");
        strT = Type.GetType("Windows.Storage.Streams.IRandomAccessStreamWithContentType, Windows.Storage.Streams, ContentType=WindowsRuntime");
        ext = rt.GetType("System.WindowsRuntimeSystemExtensions");
    }

    static object Await(object op, Type resultType)
    {
        MethodInfo m = null;
        foreach (MethodInfo mi in ext.GetMethods())
        {
            if (mi.Name == "AsTask" && mi.IsGenericMethodDefinition && mi.GetParameters().Length == 1
                && mi.GetParameters()[0].ParameterType.Name.StartsWith("IAsyncOperation`1"))
                m = mi;
        }
        Task t = (Task)m.MakeGenericMethod(resultType).Invoke(null, new object[] { op });
        t.Wait(4000);
        return ((dynamic)t).Result;
    }

    static object Session()
    {
        Init();
        dynamic mgr = Await(mgrT.GetMethod("RequestAsync").Invoke(null, null), mgrT);
        foreach (dynamic s in mgr.GetSessions())
        {
            string id = ((string)s.SourceAppUserModelId).ToLower();
            if (id.Contains("spotify")) return s;
        }
        return null;
    }

    // 0 = ausgefuehrt, 1 = Spotify hat keine Sitzung, 2 = Fehler (Windows-Funktion nicht verfuegbar)
    public static int Command(string name)
    {
        try
        {
            dynamic s = Session();
            if (s == null) return 1;
            object op;
            if (name == "next") op = s.TrySkipNextAsync();
            else if (name == "prev") op = s.TrySkipPreviousAsync();
            else op = s.TryTogglePlayPauseAsync();
            Await(op, typeof(bool));
            return 0;
        }
        catch { return 2; }
    }

    // Titel, Interpret und Zustand (4 = spielt, 5 = Pause) aus der Windows-Mediensitzung von Spotify.
    // Das ist genauer als der Fenstertitel: erkennt Pause und Werbung zuverlaessig.
    public static bool GetState(out string title, out string artist, out int status)
    {
        title = ""; artist = ""; status = -1;
        dynamic s = Session();
        if (s == null) return false;
        dynamic props = Await(s.TryGetMediaPropertiesAsync(), propT);
        title = (string)props.Title ?? "";
        artist = (string)props.Artist ?? "";
        dynamic info = s.GetPlaybackInfo();
        status = Convert.ToInt32((object)info.PlaybackStatus);
        return true;
    }

    // Zufall und Wiederholen aus der Mediensitzung. shuffle: -1 unbekannt, 0 aus, 1 an.
    // repeat: -1 unbekannt, 0 aus, 1 Playlist, 2 Titel
    public static bool GetModes(out int shuffle, out int repeat)
    {
        shuffle = -1; repeat = -1;
        dynamic s = Session();
        if (s == null) return false;
        dynamic info = s.GetPlaybackInfo();
        object sh = info.IsShuffleActive;
        if (sh != null) shuffle = Convert.ToBoolean(sh) ? 1 : 0;
        object rp = info.AutoRepeatMode;
        if (rp != null)
        {
            int v = Convert.ToInt32(rp);   // 0 aus, 1 Titel, 2 Liste
            repeat = v == 1 ? 2 : (v == 2 ? 1 : 0);
        }
        return true;
    }

    // 0 = ok, 1 = keine Sitzung, 2 = Fehler, 3 = von Spotify nicht unterstuetzt
    public static int SetShuffle(bool on)
    {
        try
        {
            dynamic s = Session();
            if (s == null) return 1;
            object op = s.TryChangeShuffleActiveAsync(on);
            return (bool)Await(op, typeof(bool)) ? 0 : 3;
        }
        catch { return 2; }
    }

    public static int SetRepeat(int mode)
    {
        try
        {
            dynamic s = Session();
            if (s == null) return 1;
            Type et = Type.GetType("Windows.Media.MediaPlaybackAutoRepeatMode, Windows.Media, ContentType=WindowsRuntime");
            if (et == null) return 2;
            object val = Enum.ToObject(et, mode == 2 ? 1 : (mode == 1 ? 2 : 0));
            object op = s.TryChangeAutoRepeatModeAsync((dynamic)val);
            return (bool)Await(op, typeof(bool)) ? 0 : 3;
        }
        catch { return 2; }
    }

    public static bool Seek(double sec)
    {
        try
        {
            dynamic s = Session();
            if (s == null) return false;
            object op = s.TryChangePlaybackPositionAsync((long)(sec * 10000000.0));
            return (bool)Await(op, typeof(bool));
        }
        catch { return false; }
    }

    public static bool GetTimeline(bool isPlaying, out double pos, out double dur)
    {
        pos = 0; dur = 0;
        dynamic s = Session();
        if (s == null) return false;
        dynamic tl = s.GetTimelineProperties();
        TimeSpan st = tl.StartTime, en = tl.EndTime, ps = tl.Position;
        dur = (en - st).TotalSeconds;
        pos = (ps - st).TotalSeconds;
        if (isPlaying)
        {
            DateTimeOffset lu = tl.LastUpdatedTime;
            double extra = (DateTimeOffset.Now - lu).TotalSeconds;
            if (extra > 0 && extra < 7200) pos += extra;
        }
        if (pos < 0) pos = 0;
        if (pos > dur) pos = dur;
        return dur > 1;
    }

    // Titel, Interpret und Bild (auch bei Werbung)
    public static bool GetMeta(out string title, out string artist, out Image image)
    {
        title = ""; artist = ""; image = null;
        dynamic s = Session();
        if (s == null) return false;
        dynamic props = Await(s.TryGetMediaPropertiesAsync(), propT);
        title = (string)props.Title ?? "";
        artist = (string)props.Artist ?? "";
        try
        {
            dynamic thumb = props.Thumbnail;
            if (thumb != null)
            {
                object stream = Await(thumb.OpenReadAsync(), strT);
                MethodInfo asRead = null;
                foreach (MethodInfo mi in streamExt.GetMethods())
                    if (mi.Name == "AsStreamForRead" && mi.GetParameters().Length == 1) asRead = mi;
                Stream net = (Stream)asRead.Invoke(null, new object[] { stream });
                MemoryStream ms = new MemoryStream();
                net.CopyTo(ms);
                ms.Position = 0;
                image = Image.FromStream(ms);
            }
        }
        catch { }
        return true;
    }
}
