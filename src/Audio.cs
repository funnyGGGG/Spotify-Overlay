// Spotify Overlay - funny.games
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

// Lautstaerke nur fuer Spotify (Windows Core Audio, einzelne App-Sitzung)
static class SpotifyAudio
{
    [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    class MMDeviceEnumeratorClass { }

    [ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IMMDeviceEnumerator
    {
        int EnumAudioEndpoints(int dataFlow, int stateMask, out IntPtr devices);
        int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice device);
    }

    [ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IMMDevice
    {
        int Activate(ref Guid iid, int clsCtx, IntPtr activationParams, [MarshalAs(UnmanagedType.IUnknown)] out object iface);
    }

    [ComImport, Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IAudioSessionManager2
    {
        int GetAudioSessionControl(IntPtr groupId, int flags, out IntPtr ctl);
        int GetSimpleAudioVolume(IntPtr groupId, int flags, out IntPtr vol);
        int GetSessionEnumerator(out IAudioSessionEnumerator e);
    }

    [ComImport, Guid("E2F5BB11-0570-40CA-ACDD-3AA01277DEE8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IAudioSessionEnumerator
    {
        int GetCount(out int count);
        int GetSession(int index, out IAudioSessionControl2 session);
    }

    [ComImport, Guid("BFB7FF88-7239-4FC9-8FA2-07C950BE9C6D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IAudioSessionControl2
    {
        int GetState(out int state);
        int GetDisplayName(out IntPtr name);
        int SetDisplayName(IntPtr name, ref Guid ctx);
        int GetIconPath(out IntPtr path);
        int SetIconPath(IntPtr path, ref Guid ctx);
        int GetGroupingParam(out Guid g);
        int SetGroupingParam(ref Guid g, ref Guid ctx);
        int RegisterAudioSessionNotification(IntPtr n);
        int UnregisterAudioSessionNotification(IntPtr n);
        int GetSessionIdentifier(out IntPtr id);
        int GetSessionInstanceIdentifier(out IntPtr id);
        int GetProcessId(out uint pid);
        int IsSystemSoundsSession();
        int SetDuckingPreference(bool optOut);
    }

    [ComImport, Guid("87CE5498-68D6-44E5-9215-6DA47EF883D8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface ISimpleAudioVolume
    {
        int SetMasterVolume(float level, ref Guid ctx);
        int GetMasterVolume(out float level);
        int SetMute(bool mute, ref Guid ctx);
        int GetMute(out bool mute);
    }

    [ComImport, Guid("C02216F6-8C67-4B5B-9D00-D008E73E0064"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IAudioMeterInformation
    {
        int GetPeakValue(out float peak);
        int GetMeteringChannelCount(out int count);
        int GetChannelsPeakValues(int count, [Out] float[] values);
        int QueryHardwareSupport(out int mask);
    }

    static readonly object gate = new object();
    static List<ISimpleAudioVolume> cache = new List<ISimpleAudioVolume>();
    static DateTime cacheTime = DateTime.MinValue;

    static List<ISimpleAudioVolume> Sessions()
    {
        if (cache.Count > 0 && (DateTime.Now - cacheTime).TotalSeconds < 3) return cache;

        List<ISimpleAudioVolume> list = new List<ISimpleAudioVolume>();
        HashSet<uint> pids = new HashSet<uint>();
        foreach (Process p in Process.GetProcessesByName("Spotify")) { pids.Add((uint)p.Id); p.Dispose(); }
        if (pids.Count > 0)
        {
            IMMDeviceEnumerator en = (IMMDeviceEnumerator)new MMDeviceEnumeratorClass();
            IMMDevice dev;
            en.GetDefaultAudioEndpoint(0, 1, out dev);   // Ausgabe, Multimedia
            Guid iid = new Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F");
            object o;
            dev.Activate(ref iid, 23, IntPtr.Zero, out o);
            IAudioSessionManager2 mgr = (IAudioSessionManager2)o;
            IAudioSessionEnumerator se;
            mgr.GetSessionEnumerator(out se);
            int n;
            se.GetCount(out n);
            for (int i = 0; i < n; i++)
            {
                IAudioSessionControl2 c;
                se.GetSession(i, out c);
                uint pid;
                c.GetProcessId(out pid);
                if (pids.Contains(pid)) list.Add((ISimpleAudioVolume)c);
            }
        }
        cache = list;
        cacheTime = DateTime.Now;
        return list;
    }

    // 0..1, oder -1 wenn Spotify gerade keine Audio-Sitzung hat
    public static float Get()
    {
        try
        {
            lock (gate)
            {
                List<ISimpleAudioVolume> l = Sessions();
                if (l.Count == 0) return -1f;
                float v;
                l[0].GetMasterVolume(out v);
                return v;
            }
        }
        catch { cache = new List<ISimpleAudioVolume>(); return -1f; }
    }

    // Aktueller Pegel 0..1 (wie laut Spotify gerade spielt), 0 wenn nichts zu messen ist
    public static float Peak()
    {
        try
        {
            lock (gate)
            {
                List<ISimpleAudioVolume> l = Sessions();
                if (l.Count == 0) return 0f;
                float p;
                ((IAudioMeterInformation)l[0]).GetPeakValue(out p);
                if (p < 0f) p = 0f;
                if (p > 1f) p = 1f;
                return p;
            }
        }
        catch { return 0f; }
    }

    public static void Set(float v)
    {
        try
        {
            if (v < 0f) v = 0f;
            if (v > 1f) v = 1f;
            lock (gate)
            {
                foreach (ISimpleAudioVolume s in Sessions())
                {
                    Guid g = Guid.Empty;
                    s.SetMasterVolume(v, ref g);
                }
            }
        }
        catch { }
    }
}
