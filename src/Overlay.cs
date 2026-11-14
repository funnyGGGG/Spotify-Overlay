// Spotify Overlay - funny.games
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

// Unsichtbares Mini-Fenster: gibt dem Overlay einen Button in der Taskleiste
// und liefert das Bild fuer die Vorschau (Taskleiste / Alt+Tab)
class TaskbarHost : Form
{
    [DllImport("dwmapi.dll")]
    static extern int DwmSetWindowAttribute(IntPtr h, int attr, ref int val, int size);
    [DllImport("dwmapi.dll")]
    static extern int DwmSetIconicThumbnail(IntPtr h, IntPtr bmp, int flags);
    [DllImport("dwmapi.dll")]
    static extern int DwmSetIconicLivePreviewBitmap(IntPtr h, IntPtr bmp, IntPtr pt, int flags);
    [DllImport("dwmapi.dll")]
    static extern int DwmInvalidateIconicBitmaps(IntPtr h);
    [DllImport("gdi32.dll")]
    static extern bool DeleteObject(IntPtr o);

    Overlay ov;
    DateTime born = DateTime.Now;

    public TaskbarHost(Overlay o, Icon ic)
    {
        ov = o;
        Text = "Spotify Overlay";
        if (ic != null) Icon = ic;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = true;
        StartPosition = FormStartPosition.Manual;
        Location = new Point(0, 0);
        Size = new Size(1, 1);
        Opacity = 0;
        // Nur ein Klick auf die Taskleiste blendet das Overlay um. Alt+Tab aktiviert dieses Fenster
        // ebenfalls, das darf das Overlay NICHT verstecken.
        Activated += delegate { if ((DateTime.Now - born).TotalSeconds > 2 && FromTaskbar()) ov.TaskbarClick(); };
        SizeChanged += delegate
        {
            if (WindowState == FormWindowState.Minimized)
            {
                WindowState = FormWindowState.Normal;
                if (FromTaskbar()) ov.TaskbarClick();
            }
        };
    }

    static bool FromTaskbar()
    {
        Point p = Cursor.Position;
        Screen sc = Screen.FromPoint(p);
        return sc.Bounds.Contains(p) && !sc.WorkingArea.Contains(p);
    }

    protected override CreateParams CreateParams
    {
        get { CreateParams cp = base.CreateParams; cp.ExStyle |= 0x20; return cp; }
    }

    protected override bool ShowWithoutActivation { get { return true; } }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        try
        {
            int one = 1;
            DwmSetWindowAttribute(Handle, 7, ref one, 4);    // Vorschau selbst liefern
            DwmSetWindowAttribute(Handle, 10, ref one, 4);
        }
        catch { }
    }

    public void RefreshThumb()
    {
        try { if (IsHandleCreated) DwmInvalidateIconicBitmaps(Handle); } catch { }
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == 0x0323 || m.Msg == 0x0326)
        {
            try
            {
                Bitmap bmp;
                if (m.Msg == 0x0323)
                {
                    long lp = (long)m.LParam;
                    int w = (int)((lp >> 16) & 0xFFFF), h = (int)(lp & 0xFFFF);
                    bmp = ov.Snapshot(w, h);
                }
                else bmp = ov.Snapshot(0, 0);

                IntPtr hb = bmp.GetHbitmap();
                if (m.Msg == 0x0323) DwmSetIconicThumbnail(Handle, hb, 0);
                else DwmSetIconicLivePreviewBitmap(Handle, hb, IntPtr.Zero, 0);
                DeleteObject(hb);
                bmp.Dispose();
            }
            catch { }
            m.Result = IntPtr.Zero;
            return;
        }
        base.WndProc(ref m);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // "Fenster schliessen" im Taskleisten-Menue beendet alles
        if (!ov.Exiting) { e.Cancel = true; ov.BeginInvoke(new Action(ov.ExitAll)); }
        base.OnFormClosing(e);
    }
}

class Overlay : Form
{
    [DllImport("user32.dll")]
    static extern bool PostMessage(IntPtr h, uint msg, IntPtr w, IntPtr l);
    [DllImport("user32.dll")]
    static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
    static extern int GetWindowLong(IntPtr h, int index);
    [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
    static extern int SetWindowLong(IntPtr h, int index, int value);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern int GetClassName(IntPtr h, StringBuilder sb, int max);
    delegate bool EnumProc(IntPtr h, IntPtr l);
    [DllImport("user32.dll")]
    static extern bool EnumWindows(EnumProc p, IntPtr l);
    [DllImport("user32.dll")]
    static extern int GetWindowThreadProcessId(IntPtr h, out int pid);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern int GetWindowText(IntPtr h, StringBuilder sb, int max);
    [DllImport("user32.dll")]
    static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int cx, int cy, uint flags);
    [DllImport("gdi32.dll")]
    static extern IntPtr CreateRoundRectRgn(int l, int t, int r, int b, int w, int h);

    const int CMD_NEXT = 11, CMD_PREV = 12, CMD_PLAY = 14;   // APPCOMMAND-Werte
    const float BaseW = 376f;
    static readonly int[] BtnX = { 226, 250, 274 };
    static readonly string[] AdWords = { "advertisement", "werbung", "anuncio", "publicidad", "publicité", "publicite", "pubblicità", "pubblicita", "reclame" };
    static readonly Color BG = Color.FromArgb(24, 24, 24);
    static readonly Color KEY = Color.FromArgb(24, 24, 25);   // fast wie BG, wird bei "Hintergrund ausblenden" durchsichtig
    static readonly Color GREEN = Color.FromArgb(29, 185, 84);
    static readonly Color ACC = Color.FromArgb(232, 120, 40);
    bool pp = Config.PerPixel;          // erweiterte Transparenz (Alpha pro Pixel), gilt ab dem Start
    double ppOpacity = 1.0;
    bool ppQueued = false;
    SongToast toast = null;
    bool toastPending = false;
    DateTime toastAt = DateTime.MinValue;

    // Fenster und Helfer
    NotifyIcon tray;
    TaskbarHost host;
    SettingsForm settings;
    PlaylistForm playlists;
    UpdateForm updateWin;
    LyricsForm lyrics;
    bool clickThrough = false;
    AutoResetEvent wake = new AutoResetEvent(false);   // weckt den Hintergrund-Thread sofort
    DateTime fastUntil = DateTime.MinValue;            // bis dahin haeufiger abfragen (nach einem Klick)
    DateTime holdPlayUntil = DateTime.MinValue;
    DateTime nextVol = DateTime.MinValue;
    Icon appIcon;
    Image cover;
    Bitmap coverScaled;
    Font fTitle, fSub, fTiny;
    DiscordRpc rpc = new DiscordRpc();
    System.Windows.Forms.Timer hideTimer;
    DateTime lastToggle = DateTime.MinValue;
    public bool Exiting = false;
    bool followHidden = false;                         // vom Modus "folgt Spotify" versteckt
    static readonly string OwnExe = Path.GetFileNameWithoutExtension(Application.ExecutablePath);
    string wFgExe = "", wFgTitle = "";                 // vorderstes Fenster (vom Hintergrund-Thread)
    GameProfile activeProfile = null;                  // gerade aktives Spiele-Profil
    bool baseClick = false;
    bool dodged = false;                               // Ausweich-Modus: gerade durchsichtig
    volatile float wavePeak = 0f;                      // Pegel fuer die Mini-Wellen
    bool waveActive = false;
    DateTime startedAt = DateTime.Now;

    // Werte, die der Hintergrund-Thread liefert
    readonly object stateLock = new object();
    string wFound = null;
    bool wRunning = false, wAd = false;
    IntPtr wHwnd = IntPtr.Zero;
    bool wSmtc = false;
    string wTitle = "", wArtist = "";
    int wStatus = -1;
    DateTime volHold = DateTime.MinValue;   // so lange den Wert vom Hintergrund ignorieren (nach Ziehen am Regler)
    float wVol = -1f;

    // Zustand
    string trackTitle = "", trackArtist = "", lastKey = "", adInfo = "";
    bool running = false, hasSong = false, playing = false, isAd = false;
    double posSec = 0, durSec = 0;
    DateTime lastTick = DateTime.Now;
    bool syncing = false;
    int tick = 0;
    float vol = -1f;

    // Fenster / Maus
    float scale = 1f;
    int hoverBtn = -1, pressedBtn = -1;
    bool dragging = false, resizing = false, volDrag = false, seeking = false;
    Point dragOffset;
    int resizeStartX;
    float resizeStartScale;
    float pendingVol = 0f;
    AutoResetEvent volEvent = new AutoResetEvent(false);

    float BaseH { get { return Config.Volume ? 86f : 68f; } }

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams cp = base.CreateParams;
            cp.ExStyle |= Config.Capturable ? 0x08000000 : (0x08000000 | 0x80);   // NOACTIVATE (+ TOOLWINDOW): nimmt dem Spiel nie den Fokus
            if (pp) cp.ExStyle |= 0x80000;   // LAYERED: Alpha pro Pixel
            return cp;
        }
    }

    protected override bool ShowWithoutActivation { get { return true; } }

    // Befehl direkt an das Spotify-Fenster schicken: sofort und nur fuer Spotify (nicht fuer YouTube & Co).
    // Gibt es das Fenster nicht, wird die Windows-Mediensitzung von Spotify benutzt.
    void Cmd(string name, int appCommand)
    {
        // Anzeige sofort umschalten, der echte Zustand kommt gleich hinterher
        if (name == "play" && !isAd)
        {
            playing = !playing;
            holdPlayUntil = DateTime.Now.AddSeconds(1.5);
            Invalidate();
        }
        fastUntil = DateTime.Now.AddSeconds(4);
        wake.Set();

        IntPtr h;
        lock (stateLock) { h = wHwnd; }
        if (h != IntPtr.Zero)
        {
            PostMessage(h, 0x0319, h, (IntPtr)(appCommand << 16));   // WM_APPCOMMAND
            return;
        }
        if (running) ThreadPool.QueueUserWorkItem(delegate { Media.Command(name); });
    }

    // ---------- Bilder ----------
    static GraphicsPath RoundRect(int x, int y, int w, int h, int r)
    {
        GraphicsPath p = new GraphicsPath();
        p.AddArc(x, y, r * 2, r * 2, 180, 90);
        p.AddArc(x + w - r * 2, y, r * 2, r * 2, 270, 90);
        p.AddArc(x + w - r * 2, y + h - r * 2, r * 2, r * 2, 0, 90);
        p.AddArc(x, y + h - r * 2, r * 2, r * 2, 90, 90);
        p.CloseFigure();
        return p;
    }

    static Image Round(Image src, int s)
    {
        Bitmap bmp = new Bitmap(s, s);
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.Clear(BG);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            using (GraphicsPath p = RoundRect(0, 0, s, s, s * 10 / 56))
            {
                g.SetClip(p);
                g.DrawImage(src, 0, 0, s, s);
            }
        }
        return bmp;
    }

    static Image Fallback()
    {
        try
        {
            using (Stream rs = Assembly.GetExecutingAssembly().GetManifestResourceStream("cover.png"))
                if (rs != null) return new Bitmap(rs);
        }
        catch { }
        Bitmap bmp = new Bitmap(64, 64);
        using (Graphics g = Graphics.FromImage(bmp)) g.Clear(GREEN);
        return bmp;
    }

    // Bild in der aktuellen Groesse vorbereiten, damit das Zeichnen schnell bleibt
    void RebuildCover()
    {
        int sz = Math.Max(8, (int)(38 * scale));
        Bitmap b = new Bitmap(sz, sz);
        using (Graphics g = Graphics.FromImage(b))
        {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(cover, 0, 0, sz, sz);
        }
        Bitmap old = coverScaled;
        coverScaled = b;
        if (old != null) old.Dispose();
    }

    void MakeFonts()
    {
        Font a = fTitle, b = fSub, c = fTiny;
        fTitle = new Font("Segoe UI", 14f * scale, FontStyle.Bold, GraphicsUnit.Pixel);
        fSub = new Font("Segoe UI", 11.5f * scale, FontStyle.Regular, GraphicsUnit.Pixel);
        fTiny = new Font("Segoe UI", 10f * scale, FontStyle.Regular, GraphicsUnit.Pixel);
        if (a != null) a.Dispose();
        if (b != null) b.Dispose();
        if (c != null) c.Dispose();
    }

    // ---------- Hintergrund: Zeitleiste und Cover ----------
    void SyncTimeline()
    {
        if (syncing) return;
        syncing = true;
        try
        {
            double p, d;
            if (Media.GetTimeline(playing, out p, out d) && !seeking) { posSec = p; durSec = d; }
        }
        catch { }
        syncing = false;
        try { BeginInvoke(new Action(delegate { Invalidate(); })); } catch { }
    }

    void LoadCover()
    {
        Thread.Sleep(1200);
        SyncTimeline();
        Image img = null;
        string mt = "", ma = "";
        try { Media.GetMeta(out mt, out ma, out img); } catch { }
        if (img == null) img = Fallback();
        Image rounded = Round(img, 112);
        bool ad = isAd;
        try
        {
            BeginInvoke(new Action(delegate
            {
                cover = rounded;
                RebuildCover();
                Streamer.SetCover(cover);
                if (ad)
                {
                    List<string> parts = new List<string>();
                    if (mt.Length > 0 && Array.IndexOf(AdWords, mt.Trim().ToLower()) < 0) parts.Add(mt);
                    if (ma.Length > 0 && ma != "Spotify") parts.Add(ma);
                    adInfo = string.Join(" · ", parts.ToArray());
                }
                Invalidate();
                if (host != null) host.RefreshThumb();
            }));
        }
        catch { }
    }

    // Laeuft im Hintergrund, damit die Oberflaeche nie ruckelt
    void WorkerLoop()
    {
        int n = 0, fails = 0;
        while (!Exiting)
        {
            try
            {
                HashSet<int> pids = new HashSet<int>();
                foreach (Process p in Process.GetProcessesByName("Spotify")) { pids.Add(p.Id); p.Dispose(); }
                bool r = pids.Count > 0;
                string found = null;
                bool ad = false;
                IntPtr main = IntPtr.Zero;
                if (r)
                {
                    EnumWindows(delegate(IntPtr h, IntPtr l)
                    {
                        int pid; GetWindowThreadProcessId(h, out pid);
                        if (!pids.Contains(pid)) return true;
                        StringBuilder sb = new StringBuilder(256);
                        GetWindowText(h, sb, 256);
                        string t = sb.ToString();
                        StringBuilder cn = new StringBuilder(64);
                        GetClassName(h, cn, 64);
                        string cls = cn.ToString();
                        if (t.Length > 0 && cls.StartsWith("Chrome_WidgetWin") && (main == IntPtr.Zero || cls == "Chrome_WidgetWin_0")) main = h;
                        if (Array.IndexOf(AdWords, t.Trim().ToLower()) >= 0) { ad = true; return true; }
                        if (t.Contains(" - ")) { found = t; return true; }
                        return true;
                    }, IntPtr.Zero);
                }
                // Titel, Interpret und Spielt/Pause aus der Windows-Mediensitzung von Spotify
                string st = "", sa = "";
                int sstat = -1;
                bool sok = false;
                if (r && (fails < 4 || n % 10 == 0))
                {
                    try { sok = Media.GetState(out st, out sa, out sstat); fails = 0; }
                    catch { fails++; }
                }
                float v = -1f;
                bool readVol = Config.Volume && !volDrag && DateTime.Now >= nextVol;
                if (readVol) { v = SpotifyAudio.Get(); nextVol = DateTime.Now.AddMilliseconds(600); }
                string fgExe = "", fgTitle = "";
                if (GameProfiles.Any)
                {
                    try
                    {
                        IntPtr fgWin = GetForegroundWindow();
                        if (fgWin != IntPtr.Zero)
                        {
                            int fgPid;
                            GetWindowThreadProcessId(fgWin, out fgPid);
                            StringBuilder fgSb = new StringBuilder(256);
                            GetWindowText(fgWin, fgSb, 256);
                            fgTitle = fgSb.ToString();
                            using (Process fgProc = Process.GetProcessById(fgPid)) fgExe = fgProc.ProcessName;
                        }
                    }
                    catch { }
                }
                lock (stateLock)
                {
                    wFgExe = fgExe;
                    wFgTitle = fgTitle;
                    wRunning = r;
                    wSmtc = sok;
                    wTitle = st;
                    wArtist = sa;
                    wStatus = sstat;
                    wHwnd = main;
                    wFound = found;
                    wAd = ad && found == null;
                    if (readVol) wVol = v;
                }
            }
            catch { }
            n++;
            wake.WaitOne(DateTime.Now < fastUntil ? 250 : 1000);
        }
    }

    // ---------- Animation bei neuem Song ----------
    void ShowToast()
    {
        try
        {
            if (!Config.Toast || isAd || !hasSong || Config.Popup) return;
            if (toast != null && !toast.IsDisposed) toast.Close();
            Image c = null;
            try { c = new Bitmap(cover); } catch { }
            toast = new SongToast(c, trackTitle, trackArtist, BG, ACC, Color.White, Color.FromArgb(165, 165, 165));
            toast.Start();
        }
        catch { }
    }

    // ---------- Lautstaerke von Spotify schnell mitlesen (ca. 4x pro Sekunde) ----------
    void VolSyncLoop()
    {
        while (!Exiting)
        {
            Thread.Sleep(250);
            try
            {
                if (!Config.Volume || volDrag || !running) continue;
                float v = SpotifyAudio.Get();
                if (v >= 0f) wVol = v;
            }
            catch { }
        }
    }

    public void ReconnectDiscord()
    {
        DiscordRpc.Status = "reconnecting ...";
        rpc.Reconnect();
    }

    // ---------- Mini-Wellen ----------
    Rectangle WaveRect()
    {
        return new Rectangle((int)(6 * scale), (int)(62 * scale), (int)(46 * scale), (int)(22 * scale));
    }

    void WaveLoop()
    {
        while (!Exiting)
        {
            Thread.Sleep(80);
            try
            {
                bool on = Config.Waves && Config.Volume && playing && !isAd && Visible;
                if (!on)
                {
                    if (waveActive)
                    {
                        waveActive = false;
                        wavePeak = 0f;
                        BeginInvoke(new Action(delegate { Invalidate(WaveRect()); }));
                    }
                    continue;
                }
                waveActive = true;
                wavePeak = SpotifyAudio.Peak();
                BeginInvoke(new Action(delegate { Invalidate(WaveRect()); }));
            }
            catch { }
        }
    }

    void DrawWaves(Graphics g)
    {
        float s = scale;
        double ph = DateTime.Now.Ticks / 10000000.0 * 9.0;
        float lvl = (float)Math.Min(1.0, Math.Sqrt(wavePeak) * 1.25);
        using (SolidBrush b = new SolidBrush(GREEN))
            for (int i = 0; i < 6; i++)
            {
                float f = (float)(0.35 + 0.65 * Math.Abs(Math.Sin(ph + i * 1.3)));
                float h = Math.Max(1.5f * s, 13f * s * lvl * f);
                g.FillRectangle(b, (10 + i * 6.2f) * s, 80f * s - h, 3.4f * s, h);
            }
    }

    // ---------- Ausweich-Modus ----------
    void UpdateDodge()
    {
        bool want = Config.Dodge && Visible && !dragging && !resizing && !volDrag && !seeking && Bounds.Contains(Cursor.Position);
        if (want == dodged) return;
        dodged = want;
        SetOpacity(want ? 0.15 : Math.Max(0.2, Math.Min(1.0, Config.Opacity / 100.0)));
    }

    // ---------- Spiele-Profile ----------
    // Passt das vorderste Programm zu einem Profil, wird es uebernommen; beim Verlassen geht alles zurueck.
    void ApplyProfiles()
    {
        try
        {
            if (!GameProfiles.Any && activeProfile == null) return;
            string fe, ft;
            lock (stateLock) { fe = wFgExe; ft = wFgTitle; }
            bool editing = settings != null && !settings.IsDisposed && settings.Visible;
            GameProfile m = null;
            if (!editing)
            {
                if (fe.Length == 0) return;
                if (string.Equals(fe, OwnExe, StringComparison.OrdinalIgnoreCase)) return;   // Overlay selbst: nichts aendern
                m = GameProfiles.Find(fe, ft);
            }
            SwitchProfile(m);
        }
        catch { }
    }

    void SwitchProfile(GameProfile m)
    {
        if (m == activeProfile) return;
        if (m != null)
        {
            if (activeProfile == null)
            {
                Config.BaseScale = ScalePercent;
                Config.BaseOpacity = Config.Opacity;
                Config.BasePopup = Config.Popup;
                Config.BaseVolume = Config.Volume;
                Config.BaseX = Left;
                Config.BaseY = Top;
                baseClick = clickThrough;
                Config.ProfileOn = true;
            }
            activeProfile = m;
            PreviewScale(m.Scale);
            Config.Opacity = m.Opacity;
            Config.Popup = m.Popup;
            Config.Volume = m.Volume;
            ApplySettings();
            if (m.HasPos)
            {
                Point pp = new Point(m.X, m.Y);
                foreach (Screen sc in Screen.AllScreens)
                    if (sc.WorkingArea.Contains(pp)) { Location = pp; break; }
            }
            if (clickThrough != m.Click) SetClickThrough(m.Click);
        }
        else
        {
            activeProfile = null;
            Config.ProfileOn = false;
            PreviewScale(Config.BaseScale);
            Config.Opacity = Config.BaseOpacity;
            Config.Popup = Config.BasePopup;
            Config.Volume = Config.BaseVolume;
            ApplySettings();
            if (Config.BaseX != int.MinValue) Location = new Point(Config.BaseX, Config.BaseY);
            if (clickThrough != baseClick) SetClickThrough(baseClick);
        }
    }

    void VolumeLoop()
    {
        while (!Exiting)
        {
            volEvent.WaitOne();
            if (Exiting) return;
            SpotifyAudio.Set(pendingVol);
        }
    }

    // ---------- Aufbau ----------
    public Overlay()
    {
        GameProfiles.Load();
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        FormBorderStyle = FormBorderStyle.None;
        TopMost = true;
        ShowInTaskbar = false;
        Text = "Spotify Overlay";
        BackColor = BG;
        StartPosition = FormStartPosition.Manual;
        scale = Config.Scale / 100f;
        SetOpacity(Math.Max(0.2, Math.Min(1.0, Config.Opacity / 100.0)));
        if (!pp) TransparencyKey = Config.BgOff ? KEY : Color.Empty;
        Location = new Point(Screen.PrimaryScreen.WorkingArea.Right - (int)(BaseW * scale) - 20, 20);
        if (Config.X != int.MinValue && Config.Y != int.MinValue)
        {
            Point p = new Point(Config.X, Config.Y);
            foreach (Screen sc in Screen.AllScreens)
                if (sc.WorkingArea.Contains(p)) { Location = p; break; }
        }

        try { appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
        cover = Round(Fallback(), 112);

        hideTimer = new System.Windows.Forms.Timer();
        hideTimer.Interval = 6000;
        hideTimer.Tick += delegate
        {
            hideTimer.Stop();
            if (Config.Popup && !ClientRectangle.Contains(PointToClient(Cursor.Position))) Visible = false;
        };

        tray = new NotifyIcon();
        if (appIcon != null) tray.Icon = appIcon;
        tray.Text = "Spotify Overlay";
        tray.DoubleClick += delegate { TaskbarClick(); };
        BuildMenu();
        tray.Visible = true;

        // Sekundentakt: nur leichte Arbeit (Zeit weiterzaehlen, zeichnen)
        System.Windows.Forms.Timer poll = new System.Windows.Forms.Timer();
        poll.Interval = 250;
        poll.Tick += delegate { Tick(); };
        poll.Start();

        // Immer ganz oben halten, ohne Fokus
        System.Windows.Forms.Timer top = new System.Windows.Forms.Timer();
        top.Interval = 2000;
        top.Tick += delegate { if (Visible) SetWindowPos(Handle, (IntPtr)(-1), 0, 0, 0, 0, 0x0013); };
        top.Start();

        // Update-Suche: 20 Sekunden nach dem Start, dann alle 6 Stunden
        System.Windows.Forms.Timer upd = new System.Windows.Forms.Timer();
        upd.Interval = 20000;
        upd.Tick += delegate { upd.Interval = 6 * 3600 * 1000; StartUpdateCheck(false, null); };
        upd.Start();

        Thread w = new Thread(WorkerLoop);
        w.IsBackground = true;
        w.Start();
        Thread vt = new Thread(VolumeLoop);
        vt.IsBackground = true;
        vt.Start();
        Thread wvt = new Thread(WaveLoop);
        wvt.IsBackground = true;
        wvt.Start();
        Thread vst = new Thread(VolSyncLoop);
        vst.IsBackground = true;
        vst.Start();

        ApplyScale(scale);
        if (Config.Popup) hideTimer.Start();
    }

    void BuildMenu()
    {
        ContextMenuStrip m = new ContextMenuStrip();
        m.Items.Add(Lang.T("tray_toggle"), null, delegate { TaskbarClick(); });
        m.Items.Add(Lang.T("tray_lyrics"), null, delegate { ToggleLyrics(); });
        ToolStripMenuItem ct = new ToolStripMenuItem(Lang.T("tray_click"));
        ct.Checked = clickThrough;
        ct.Click += delegate { SetClickThrough(!clickThrough); };
        m.Items.Add(ct);
        m.Items.Add(Lang.T("tray_playlists"), null, delegate { OpenPlaylists(); });
        m.Items.Add(Lang.T("tray_settings"), null, delegate { OpenSettings(); });
        m.Items.Add(Lang.T("tray_update"), null, delegate { StartUpdateCheck(true, null); });
        m.Items.Add(new ToolStripSeparator());
        m.Items.Add(Lang.T("tray_exit"), null, delegate { ExitAll(); });
        tray.ContextMenuStrip = m;
    }

    void OpenSettings()
    {
        SwitchProfile(null);   // in den Einstellungen gilt immer der normale Stand
        if (settings == null || settings.IsDisposed) settings = new SettingsForm(this);
        settings.Show();
        settings.Activate();
    }

    // Songtext-Fenster ein-/ausblenden
    void ToggleLyrics()
    {
        if (lyrics != null && !lyrics.IsDisposed)
        {
            lyrics.Close();
            lyrics = null;
        }
        else
        {
            lyrics = new LyricsForm(this);
            lyrics.Show();
            ApplyLyricsClickThrough();
        }
        Invalidate();
    }

    // Im Durchklickbar-Modus geht das Songtext-Fenster auch fuer die Maus durch
    void ApplyLyricsClickThrough()
    {
        try
        {
            if (lyrics != null && !lyrics.IsDisposed && lyrics.IsHandleCreated)
            {
                int ex = GetWindowLong(lyrics.Handle, -20);
                ex = clickThrough ? (ex | 0x20) : (ex & ~0x20);
                SetWindowLong(lyrics.Handle, -20, ex);
                lyrics.Invalidate();
            }
        }
        catch { }
    }

    // Durchklickbar: Mausklicks gehen durch das Overlay hindurch zum Spiel
    void SetClickThrough(bool on)
    {
        clickThrough = on;
        try
        {
            int ex = GetWindowLong(Handle, -20);
            ex = on ? (ex | 0x20) : (ex & ~0x20);
            SetWindowLong(Handle, -20, ex);
        }
        catch { }
        BuildMenu();
        ApplyLyricsClickThrough();
        Invalidate();
    }

    public void ShowWelcome()
    {
        WelcomeForm w = new WelcomeForm();
        w.Show();
    }

    public void ResetPosition()
    {
        Location = new Point(Screen.PrimaryScreen.WorkingArea.Right - (int)(BaseW * scale) - 20, 20);
        SavePosition();
    }

    public int ScalePercent { get { return (int)Math.Round(scale * 100); } }

    // Vorschau der Groesse, gespeichert wird erst mit SavePosition()
    public void PreviewScale(int percent)
    {
        ApplyScale(percent / 100f);
    }

    // Lesezugriff fuer das Songtext-Fenster
    public string CurTitle { get { return trackTitle; } }
    public string CurArtist { get { return trackArtist; } }
    public double PosSec { get { return posSec; } }
    public double DurSec { get { return durSec; } }
    public bool IsAd { get { return isAd; } }
    public bool IsRunning { get { return running; } }

    void OpenPlaylists()
    {
        if (playlists == null || playlists.IsDisposed) playlists = new PlaylistForm();
        playlists.Show();
        playlists.Activate();
    }

    // Wird von den Einstellungen aufgerufen
    public void ApplySettings()
    {
        SetOpacity(Math.Max(0.2, Math.Min(1.0, Config.Opacity / 100.0)));
        if (!pp) TransparencyKey = Config.BgOff ? KEY : Color.Empty;
        Invalidate();
        if (lyrics != null && !lyrics.IsDisposed) lyrics.ApplyOpacity();
        BuildMenu();
        ApplyScale(scale);
        if (!Config.Follow) followHidden = false;
        if (Config.Popup) ShowPopup();
        else { hideTimer.Stop(); if (!Visible) Visible = true; }
        rpc.Reset();
        HotKeys.Apply(Handle);
        Invalidate();
        if (host != null) host.RefreshThumb();
    }

    void Balloon(string text)
    {
        try { BeginInvoke(new Action(delegate { tray.ShowBalloonTip(4000, "Spotify Overlay", text, ToolTipIcon.Info); })); } catch { }
    }

    // Aus den Einstellungen: Ergebnis kommt als Text zurueck
    public void CheckForUpdates(Action<string> report)
    {
        StartUpdateCheck(true, report);
    }

    void StartUpdateCheck(bool manual, Action<string> report)
    {
        Thread t = new Thread(delegate()
        {
            // Rueckmeldung nur bei manueller Suche: in den Einstellungen als Text, sonst als Sprechblase
            Action<string> say = delegate(string text)
            {
                if (!manual) return;
                if (report == null) { Balloon(text); return; }
                try { BeginInvoke(new Action(delegate { report(text); })); } catch { }
            };
            try
            {
                if (!Updater.Configured) { say(Lang.T("upd_norepo")); return; }
                UpdateInfo info;
                if (!Updater.Latest(out info)) { say(Lang.T("upd_fail")); return; }
                if (info.Ver > Updater.Current)
                {
                    say(Lang.T("upd_avail", info.Ver));
                    BeginInvoke(new Action(delegate
                    {
                        if (updateWin != null && !updateWin.IsDisposed) return;
                        updateWin = new UpdateForm(this, info, Config.AutoUpdate);
                        updateWin.Show();
                    }));
                }
                else say(Lang.T("upd_none"));
            }
            catch { say(Lang.T("upd_fail")); }
        });
        t.IsBackground = true;
        t.Start();
    }

    // ---------- Zeichnen ----------
    RectangleF BtnRect(int i)
    {
        if (i == 3) return new RectangleF(302 * scale, 7 * scale, 22 * scale, 22 * scale);   // Songtext
        if (i == 4) return new RectangleF(326 * scale, 7 * scale, 22 * scale, 22 * scale);   // Playlists
        if (i == 5) return new RectangleF(350 * scale, 7 * scale, 22 * scale, 22 * scale);   // Einstellungen
        return new RectangleF(BtnX[i] * scale, 5 * scale, 24 * scale, 24 * scale);
    }

    RectangleF VolRect() { return new RectangleF(70 * scale, 63 * scale, 214 * scale, 20 * scale); }
    RectangleF SeekRect() { return new RectangleF(56 * scale, 50 * scale, 302 * scale, 12 * scale); }
    RectangleF GripRect() { return new RectangleF(Width - 16, Height - 16, 16, 16); }

    static void DrawBtn(Graphics g, int kind, RectangleF r, bool hover, bool isPlaying)
    {
        Color c = hover ? ACC : Color.White;
        float cx = r.X + r.Width / 2f, cy = r.Y + r.Height / 2f, s = Math.Min(r.Width, r.Height) * 0.30f;
        using (SolidBrush br = new SolidBrush(c))
        {
            if (kind == 1)
            {
                if (isPlaying)
                {
                    float bw = s * 0.6f;
                    g.FillRectangle(br, cx - s * 0.8f, cy - s, bw, s * 2);
                    g.FillRectangle(br, cx + s * 0.8f - bw, cy - s, bw, s * 2);
                }
                else
                    g.FillPolygon(br, new PointF[] { new PointF(cx - s * 0.7f, cy - s), new PointF(cx + s * 0.95f, cy), new PointF(cx - s * 0.7f, cy + s) });
            }
            else if (kind == 2)
            {
                g.FillPolygon(br, new PointF[] { new PointF(cx - s * 0.9f, cy - s), new PointF(cx + s * 0.5f, cy), new PointF(cx - s * 0.9f, cy + s) });
                g.FillRectangle(br, cx + s * 0.6f, cy - s, s * 0.38f, s * 2);
            }
            else
            {
                g.FillPolygon(br, new PointF[] { new PointF(cx + s * 0.9f, cy - s), new PointF(cx - s * 0.5f, cy), new PointF(cx + s * 0.9f, cy + s) });
                g.FillRectangle(br, cx - s * 0.98f, cy - s, s * 0.38f, s * 2);
            }
        }
    }

    static void DrawList(Graphics g, RectangleF r, bool hover)
    {
        Color c = hover ? ACC : Color.FromArgb(200, 200, 200);
        float cx = r.X + r.Width / 2f, cy = r.Y + r.Height / 2f, u = r.Width * 0.09f;
        using (Pen p = new Pen(c, Math.Max(1.4f, u * 1.1f)))
        using (SolidBrush b = new SolidBrush(c))
            for (int i = -1; i <= 1; i++)
            {
                float y = cy + i * u * 3.2f;
                g.FillEllipse(b, cx - u * 4.4f, y - u * 0.9f, u * 1.8f, u * 1.8f);
                g.DrawLine(p, cx - u * 1.8f, y, cx + u * 4.4f, y);
            }
    }

    static void DrawLyrics(Graphics g, RectangleF r, bool hover, bool active)
    {
        Color c = (hover || active) ? ACC : Color.FromArgb(200, 200, 200);
        float x = r.X + r.Width * 0.14f, w = r.Width * 0.72f, cy = r.Y + r.Height / 2f, gap = r.Height * 0.25f;
        using (Pen p = new Pen(c, Math.Max(1.5f, r.Height * 0.085f)))
        {
            p.StartCap = LineCap.Round;
            p.EndCap = LineCap.Round;
            g.DrawLine(p, x, cy - gap, x + w, cy - gap);
            g.DrawLine(p, x, cy, x + w * 0.7f, cy);
            g.DrawLine(p, x, cy + gap, x + w * 0.85f, cy + gap);
        }
    }

    static void DrawGear(Graphics g, RectangleF r, bool hover)
    {
        Color c = hover ? ACC : Color.FromArgb(200, 200, 200);
        float cx = r.X + r.Width / 2f, cy = r.Y + r.Height / 2f;
        float R = Math.Min(r.Width, r.Height) * 0.45f;
        using (Pen p = new Pen(c, Math.Max(1.4f, R * 0.28f)))
        {
            g.DrawEllipse(p, cx - R * 0.58f, cy - R * 0.58f, R * 1.16f, R * 1.16f);
            for (int i = 0; i < 8; i++)
            {
                double a = i * Math.PI / 4;
                float ca = (float)Math.Cos(a), sa = (float)Math.Sin(a);
                g.DrawLine(p, cx + ca * R * 0.8f, cy + sa * R * 0.8f, cx + ca * R * 1.05f, cy + sa * R * 1.05f);
            }
        }
    }

    static void DrawSpeaker(Graphics g, float x, float y, float h)
    {
        using (SolidBrush b = new SolidBrush(Color.FromArgb(165, 165, 165)))
            g.FillPolygon(b, new PointF[] {
                new PointF(x, y + h * 0.3f), new PointF(x + h * 0.3f, y + h * 0.3f), new PointF(x + h * 0.7f, y),
                new PointF(x + h * 0.7f, y + h), new PointF(x + h * 0.3f, y + h * 0.7f), new PointF(x, y + h * 0.7f) });
        using (Pen p = new Pen(Color.FromArgb(165, 165, 165), Math.Max(1f, h * 0.1f)))
            g.DrawArc(p, x + h * 0.55f, y + h * 0.2f, h * 0.5f, h * 0.6f, -50, 100);
    }

    static void DrawSpotifyLogo(Graphics g, float x, float y, float d)
    {
        using (SolidBrush b = new SolidBrush(GREEN)) g.FillEllipse(b, x, y, d, d);
        using (Pen p = new Pen(BG, Math.Max(1f, d * 0.1f)))
        {
            p.StartCap = LineCap.Round;
            p.EndCap = LineCap.Round;
            g.DrawArc(p, x + d * 0.20f, y + d * 0.30f, d * 0.60f, d * 0.50f, 205, 130);
            g.DrawArc(p, x + d * 0.26f, y + d * 0.45f, d * 0.48f, d * 0.40f, 205, 130);
            g.DrawArc(p, x + d * 0.32f, y + d * 0.58f, d * 0.36f, d * 0.30f, 205, 130);
        }
    }

    static string FormatTime(double sec)
    {
        int t = (int)sec;
        return (t / 60) + ":" + (t % 60).ToString("00");
    }

    void Render(Graphics g)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBilinear;
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        g.Clear(pp ? Color.FromArgb(PpBgAlpha(), BG) : (Config.BgOff ? KEY : BG));
        if (fTitle == null) return;

        float s = scale;
        string title = trackTitle, artist = trackArtist;
        if (!running) { title = Lang.T("notStarted"); artist = ""; }
        else if (isAd) { title = Lang.T("ad_title"); artist = adInfo; }
        else if (!hasSong) { title = Lang.T("idle"); artist = ""; }

        if (coverScaled != null) g.DrawImage(coverScaled, 8 * s, 8 * s, coverScaled.Width, coverScaled.Height);
        DrawSpotifyLogo(g, 8 * s, 50 * s, 11 * s);

        using (StringFormat left = new StringFormat(StringFormatFlags.NoWrap))
        using (StringFormat right = new StringFormat(StringFormatFlags.NoWrap))
        using (SolidBrush white = new SolidBrush(Color.White))
        using (SolidBrush grey = new SolidBrush(Color.FromArgb(165, 165, 165)))
        {
            left.Trimming = StringTrimming.EllipsisCharacter;
            right.Alignment = StringAlignment.Far;

            g.DrawString(title, fTitle, white, new RectangleF(56 * s, 6 * s, 168 * s, 20 * s), left);
            g.DrawString(artist, fSub, grey, new RectangleF(56 * s, 24 * s, 168 * s, 16 * s), left);
            g.DrawString("Spotify", fTiny, grey, new RectangleF(21 * s, 49 * s, 40 * s, 14 * s), left);

            if (durSec > 0)
            {
                g.DrawString(FormatTime(posSec), fTiny, grey, new RectangleF(56 * s, 40 * s, 60 * s, 14 * s), left);
                g.DrawString(FormatTime(durSec), fTiny, grey, new RectangleF(298 * s, 40 * s, 60 * s, 14 * s), right);
            }

            // Lautstaerke-Regler (nur Spotify)
            if (Config.Volume)
            {
                DrawSpeaker(g, 56 * s, 66 * s, 12 * s);
                float tx = 74 * s, tw = 206 * s, ty = 71.5f * s;
                using (SolidBrush track = new SolidBrush(Color.FromArgb(62, 62, 62)))
                    g.FillRectangle(track, tx, ty, tw, 3 * s);
                if (vol >= 0)
                {
                    using (SolidBrush fill = new SolidBrush(ACC))
                        g.FillRectangle(fill, tx, ty, tw * vol, 3 * s);
                    float kx = tx + tw * vol;
                    g.FillEllipse(white, kx - 4.5f * s, ty + 1.5f * s - 4.5f * s, 9 * s, 9 * s);
                    g.DrawString(((int)Math.Round(vol * 100)) + "%", fTiny, grey, new RectangleF(288 * s, 65 * s, 46 * s, 14 * s), left);
                }
                else
                    g.DrawString("--", fTiny, grey, new RectangleF(288 * s, 65 * s, 46 * s, 14 * s), left);
            }
        }

        // Fortschrittsbalken (anklickbar zum Vorspulen)
        float bx = 56 * s, bw = 302 * s, by = 56 * s, bh = 3 * s;
        using (SolidBrush track = new SolidBrush(Color.FromArgb(62, 62, 62)))
            g.FillRectangle(track, bx, by, bw, bh);
        if (durSec > 0)
        {
            float frac = (float)Math.Max(0, Math.Min(1, posSec / durSec));
            using (SolidBrush fill = new SolidBrush(ACC))
                g.FillRectangle(fill, bx, by, bw * frac, bh);
            if (seeking)
                using (SolidBrush k = new SolidBrush(Color.White))
                    g.FillEllipse(k, bx + bw * frac - 4 * s, by + bh / 2 - 4 * s, 8 * s, 8 * s);
        }

        if (Config.Waves && Config.Volume && wavePeak > 0.001f) DrawWaves(g);

        for (int i = 0; i < 3; i++) DrawBtn(g, i, BtnRect(i), hoverBtn == i, playing);
        DrawLyrics(g, BtnRect(3), hoverBtn == 3, lyrics != null && !lyrics.IsDisposed);
        DrawList(g, BtnRect(4), hoverBtn == 4);
        DrawGear(g, BtnRect(5), hoverBtn == 5);

        // Durchklickbar-Modus: oranger Rahmen
        if (clickThrough)
            using (Pen cp = new Pen(ACC, 3f))
                g.DrawRectangle(cp, 1, 1, Width - 3, Height - 3);

        // Anfasser zum Groesse aendern
        using (Pen gp = new Pen(Color.FromArgb(110, 110, 110), 1.3f))
            for (int k = 1; k <= 3; k++)
                g.DrawLine(gp, Width - 2 - k * 4, Height - 3, Width - 3, Height - 2 - k * 4);
    }

    protected override void OnPaintBackground(PaintEventArgs e) { }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (pp) { PpRefresh(); return; }
        Render(e.Graphics);
    }

    // ---------- Erweiterte Transparenz (nur wenn eingeschaltet, Neustart noetig) ----------
    // Das Fenster wird dann mit Alpha pro Pixel gezeichnet, so kann der Hintergrund
    // durchsichtiger sein als Text und Cover.
    [StructLayout(LayoutKind.Sequential)]
    struct PpPoint { public int X, Y; public PpPoint(int x, int y) { X = x; Y = y; } }
    [StructLayout(LayoutKind.Sequential)]
    struct PpSize { public int W, H; public PpSize(int w, int h) { W = w; H = h; } }
    [StructLayout(LayoutKind.Sequential)]
    struct PpBlend { public byte Op, Flags, Alpha, Format; }

    [DllImport("user32.dll", EntryPoint = "UpdateLayeredWindow", SetLastError = true)]
    static extern bool PpUpdate(IntPtr hwnd, IntPtr hdcDst, ref PpPoint pptDst, ref PpSize psize, IntPtr hdcSrc, ref PpPoint pptSrc, int crKey, ref PpBlend pblend, int flags);
    [DllImport("user32.dll", EntryPoint = "GetDC")]
    static extern IntPtr PpGetDC(IntPtr h);
    [DllImport("user32.dll", EntryPoint = "ReleaseDC")]
    static extern int PpReleaseDC(IntPtr h, IntPtr dc);
    [DllImport("gdi32.dll", EntryPoint = "CreateCompatibleDC")]
    static extern IntPtr PpCreateDC(IntPtr dc);
    [DllImport("gdi32.dll", EntryPoint = "DeleteDC")]
    static extern bool PpDeleteDC(IntPtr dc);
    [DllImport("gdi32.dll", EntryPoint = "SelectObject")]
    static extern IntPtr PpSelect(IntPtr dc, IntPtr obj);
    [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
    static extern bool PpDeleteObj(IntPtr obj);

    static int PpBgAlpha()
    {
        if (Config.BgOff) return 0;
        int a = (int)(Config.BgAlpha * 2.55);
        return a < 0 ? 0 : (a > 255 ? 255 : a);
    }

    // Gesamt-Deckkraft: normal ueber Form.Opacity, mit Alpha pro Pixel ueber die Blend-Funktion
    void SetOpacity(double v)
    {
        if (pp) { ppOpacity = v; PpRefresh(); }
        else Opacity = v;
    }

    void PpRefresh()
    {
        if (!pp || !IsHandleCreated || Width < 1 || Height < 1) return;
        try
        {
            using (Bitmap bmp = new Bitmap(Width, Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                using (Graphics g = Graphics.FromImage(bmp)) Render(g);
                IntPtr screenDc = PpGetDC(IntPtr.Zero);
                IntPtr memDc = PpCreateDC(screenDc);
                IntPtr hBmp = IntPtr.Zero, old = IntPtr.Zero;
                try
                {
                    hBmp = bmp.GetHbitmap(Color.FromArgb(0));
                    old = PpSelect(memDc, hBmp);
                    PpSize size = new PpSize(Width, Height);
                    PpPoint src = new PpPoint(0, 0);
                    PpPoint dst = new PpPoint(Left, Top);
                    PpBlend blend = new PpBlend();
                    blend.Op = 0;
                    blend.Flags = 0;
                    blend.Alpha = (byte)Math.Max(1, Math.Min(255, (int)(ppOpacity * 255)));
                    blend.Format = 1;
                    PpUpdate(Handle, screenDc, ref dst, ref size, memDc, ref src, 0, ref blend, 2);
                }
                finally
                {
                    if (old != IntPtr.Zero) PpSelect(memDc, old);
                    if (hBmp != IntPtr.Zero) PpDeleteObj(hBmp);
                    PpDeleteDC(memDc);
                    PpReleaseDC(IntPtr.Zero, screenDc);
                }
            }
        }
        catch { }
    }

    // Bei Alpha pro Pixel kommt kein normales Neuzeichnen: jede Aenderung stoesst das Bild neu an
    protected override void OnInvalidated(InvalidateEventArgs e)
    {
        base.OnInvalidated(e);
        if (pp && IsHandleCreated && !ppQueued)
        {
            ppQueued = true;
            try { BeginInvoke(new Action(delegate { ppQueued = false; PpRefresh(); })); }
            catch { ppQueued = false; }
        }
    }

    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        if (pp && Visible) PpRefresh();
    }

    // Bild fuer die Vorschau (Taskleiste, Alt+Tab).
    // Das Bild hat genau die verlangte Groesse; das Overlay wird mit richtigem Seitenverhaeltnis
    // in die Mitte gesetzt, damit Windows es nicht zusammendrueckt.
    public Bitmap Snapshot(int maxW, int maxH)
    {
        Bitmap full = new Bitmap(Math.Max(1, Width), Math.Max(1, Height));
        using (Graphics g = Graphics.FromImage(full)) Render(g);
        if (maxW <= 0 || maxH <= 0) return full;

        Bitmap bmp = new Bitmap(maxW, maxH);
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.FromArgb(16, 16, 16));
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            float k = Math.Min((maxW - 8f) / full.Width, (maxH - 8f) / full.Height);
            if (k < 0.1f) k = 0.1f;
            float w = full.Width * k, h = full.Height * k;
            g.DrawImage(full, (maxW - w) / 2f, (maxH - h) / 2f, w, h);
        }
        full.Dispose();
        return bmp;
    }

    // ---------- Maus ----------
    int HitBtn(Point p)
    {
        for (int i = 0; i < 6; i++) if (BtnRect(i).Contains(p)) return i;
        return -1;
    }

    double SeekFrac(int x)
    {
        return Math.Max(0, Math.Min(1, (x - 56 * scale) / (302 * scale)));
    }

    void SetVolFromX(int x)
    {
        float v = (x - 74 * scale) / (206 * scale);
        if (v < 0f) v = 0f;
        if (v > 1f) v = 1f;
        vol = v;
        pendingVol = v;
        volEvent.Set();   // wird im Hintergrund gesetzt
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            if (GripRect().Contains(e.Location))
            {
                resizing = true;
                resizeStartX = Cursor.Position.X;
                resizeStartScale = scale;
            }
            else if (Config.Volume && VolRect().Contains(e.Location))
            {
                volDrag = true;
                SetVolFromX(e.X);
            }
            else if (durSec > 0 && !isAd && SeekRect().Contains(e.Location))
            {
                seeking = true;
                posSec = SeekFrac(e.X) * durSec;
                Invalidate();
            }
            else
            {
                pressedBtn = HitBtn(e.Location);
                if (pressedBtn < 0)
                {
                    dragging = true;
                    dragOffset = new Point(Cursor.Position.X - Left, Cursor.Position.Y - Top);
                }
            }
        }
        base.OnMouseDown(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (resizing)
        {
            int dx = Cursor.Position.X - resizeStartX;
            ApplyScale((BaseW * resizeStartScale + dx) / BaseW);
        }
        else if (volDrag) SetVolFromX(e.X);
        else if (seeking) { posSec = SeekFrac(e.X) * durSec; Invalidate(); }
        else if (dragging && e.Button == MouseButtons.Left)
            Location = new Point(Cursor.Position.X - dragOffset.X, Cursor.Position.Y - dragOffset.Y);
        else
        {
            int h = HitBtn(e.Location);
            if (h != hoverBtn) { hoverBtn = h; Invalidate(); }
            if (GripRect().Contains(e.Location)) Cursor = Cursors.SizeNWSE;
            else if (h >= 0 || (Config.Volume && VolRect().Contains(e.Location)) || (durSec > 0 && !isAd && SeekRect().Contains(e.Location)))
                Cursor = Cursors.Hand;
            else Cursor = Cursors.Default;
        }
        base.OnMouseMove(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (pressedBtn >= 0 && HitBtn(e.Location) == pressedBtn)
        {
            if (pressedBtn == 0) Cmd("prev", CMD_PREV);
            else if (pressedBtn == 1) Cmd("play", CMD_PLAY);
            else if (pressedBtn == 2) Cmd("next", CMD_NEXT);
            else if (pressedBtn == 3) ToggleLyrics();
            else if (pressedBtn == 4) OpenPlaylists();
            else OpenSettings();
        }
        pressedBtn = -1;

        if (seeking)
        {
            seeking = false;
            double target = SeekFrac(e.X) * durSec;
            posSec = target;
            ThreadPool.QueueUserWorkItem(delegate { Media.Seek(target); Thread.Sleep(900); SyncTimeline(); });
        }
        if (resizing || dragging)
        {
            resizing = false;
            dragging = false;
            SavePosition();
        }
        if (volDrag) volHold = DateTime.Now.AddSeconds(4);   // Wert vom Hintergrund erst spaeter wieder uebernehmen
        volDrag = false;
        base.OnMouseUp(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        if (hoverBtn != -1) { hoverBtn = -1; Invalidate(); }
        base.OnMouseLeave(e);
    }

    public void SavePosition()
    {
        Config.Scale = (int)Math.Round(scale * 100);
        Config.X = Left;
        Config.Y = Top;
        Config.Save();
    }

    // ---------- Fenster-Logik ----------
    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        PpRefresh();
        host = new TaskbarHost(this, appIcon);
        host.Show();
        SetWindowPos(Handle, (IntPtr)(-1), 0, 0, 0, 0, 0x0013);
        HotKeys.Apply(Handle);

        // Erster Start: kurze Tour zeigen
        if (!Config.Welcome)
        {
            System.Windows.Forms.Timer wt = new System.Windows.Forms.Timer();
            wt.Interval = 1500;
            wt.Tick += delegate { wt.Stop(); wt.Dispose(); ShowWelcome(); };
            wt.Start();
        }
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == 0x0312) { HotKeyPressed((int)m.WParam); return; }
        base.WndProc(ref m);
    }

    void HotKeyPressed(int id)
    {
        if (id == 1) ChangeVol(Config.VolStep / 100f);
        else if (id == 2) ChangeVol(-Config.VolStep / 100f);
        else if (id == 3) Cmd("next", CMD_NEXT);
        else if (id == 4) Cmd("prev", CMD_PREV);
        else if (id == 5) Cmd("play", CMD_PLAY);
        else if (id == 6) TaskbarClick();
        else if (id == 7) SetClickThrough(!clickThrough);
        else if (id == 8) ToggleLyrics();
    }

    void ChangeVol(float step)
    {
        ThreadPool.QueueUserWorkItem(delegate
        {
            float v = SpotifyAudio.Get();
            if (v < 0f) return;
            v = Math.Max(0f, Math.Min(1f, v + step));
            SpotifyAudio.Set(v);
            try
            {
                BeginInvoke(new Action(delegate
                {
                    vol = v;
                    volHold = DateTime.Now.AddSeconds(4);
                    Invalidate();
                    if (Config.Popup) ShowPopup();
                }));
            }
            catch { }
        });
    }

    public void TaskbarClick()
    {
        if ((DateTime.Now - lastToggle).TotalMilliseconds < 500) return;
        lastToggle = DateTime.Now;
        hideTimer.Stop();
        if (Visible) Visible = false;
        else
        {
            Visible = true;
            SetWindowPos(Handle, (IntPtr)(-1), 0, 0, 0, 0, 0x0013);
            if (Config.Popup) hideTimer.Start();
        }
    }

    public void ExitAll()
    {
        if (Exiting) return;
        Exiting = true;
        volEvent.Set();
        wake.Set();
        try { if (lyrics != null && !lyrics.IsDisposed) lyrics.Close(); } catch { }
        SavePosition();
        rpc.Close();
        try { Streamer.Stop(); } catch { }
        try { HotKeys.Remove(Handle); } catch { }
        try { tray.Visible = false; tray.Dispose(); } catch { }
        try { if (host != null) host.Close(); } catch { }
        Application.Exit();
    }

    void ApplyScale(float s)
    {
        if (s < 0.6f) s = 0.6f;
        if (s > 2.5f) s = 2.5f;
        bool changed = Math.Abs(s - scale) > 0.001f || fTitle == null;
        scale = (float)Math.Round(s, 2);
        Size = new Size((int)(BaseW * scale), (int)(BaseH * scale));
        Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, Width + 1, Height + 1, (int)(12 * scale), (int)(12 * scale)));
        if (changed || coverScaled == null) { MakeFonts(); RebuildCover(); }
        Invalidate();
    }

    void ShowPopup()
    {
        if (!Visible) Visible = true;
        SetWindowPos(Handle, (IntPtr)(-1), 0, 0, 0, 0, 0x0013);
        hideTimer.Stop();
        hideTimer.Start();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (tray != null) { try { tray.Visible = false; } catch { } }
        base.OnFormClosing(e);
    }

    // ---------- Takt der Oberflaeche (leicht, 4x pro Sekunde) ----------
    void Tick()
    {
        string found, st, sa;
        bool r, ad, smtc;
        int sstat;
        float v;
        lock (stateLock) { found = wFound; r = wRunning; ad = wAd; v = wVol; smtc = wSmtc; st = wTitle; sa = wArtist; sstat = wStatus; }

        // Werbung: Fenstertitel oder Titel der Mediensitzung
        if (smtc && Array.IndexOf(AdWords, st.Trim().ToLower()) >= 0) ad = true;

        bool changed = false;
        if (r != running) { running = r; changed = true; }
        bool hold = DateTime.Now < holdPlayUntil;   // gerade selbst auf Play/Pause geklickt

        // Overlay folgt Spotify: zeigen, wenn Spotify laeuft, verstecken, wenn es zu ist
        if (Config.Follow && !Exiting && (DateTime.Now - startedAt).TotalSeconds > 3)
        {
            if (!r)
            {
                if (Visible) { followHidden = true; Visible = false; }
            }
            else if (followHidden)
            {
                followHidden = false;
                if (!Visible && !Config.Popup)
                {
                    Visible = true;
                    SetWindowPos(Handle, (IntPtr)(-1), 0, 0, 0, 0, 0x0013);
                }
            }
        }

        if (!r)
        {
            playing = false; hasSong = false; isAd = false;
            if (trackTitle != "") { trackTitle = ""; trackArtist = ""; changed = true; }
        }
        else if (ad)
        {
            if (!isAd) changed = true;
            isAd = true;
            bool pl = smtc ? sstat == 4 : true;
            if (hold) pl = playing;
            if (pl != playing) changed = true;
            playing = pl;
        }
        else
        {
            if (isAd) changed = true;
            isAd = false;
            string t = null, a = null;
            bool pl = false;
            if (smtc && st.Length > 0) { t = st; a = sa; pl = sstat == 4; }
            else if (found != null)
            {
                int i = found.IndexOf(" - ");
                a = found.Substring(0, i);
                t = found.Substring(i + 3);
                pl = true;
            }
            if (hold) pl = playing;
            if (t != null)
            {
                if (a != trackArtist || t != trackTitle || !hasSong || pl != playing) changed = true;
                trackArtist = a;
                trackTitle = t;
                hasSong = true;
                playing = pl;
            }
            else
            {
                if (playing && !hold) changed = true;
                if (!hold) playing = false;
            }
        }

        DateTime now = DateTime.Now;
        double dt = (now - lastTick).TotalSeconds;
        lastTick = now;
        if (playing && durSec > 0 && !seeking)
        {
            int oldSec = (int)posSec;
            posSec += dt;
            if (posSec > durSec) posSec = durSec;
            if ((int)posSec != oldSec) changed = true;
        }

        tick++;
        if (playing && durSec > 0 && tick % 40 == 0 && !seeking)
        {
            Thread ts = new Thread(SyncTimeline);
            ts.IsBackground = true;
            ts.Start();
        }

        if (Config.Volume && !volDrag && DateTime.Now > volHold && Math.Abs(v - vol) > 0.001f) { vol = v; changed = true; }

        string key = isAd ? "ad" : (playing || hasSong ? trackTitle + "|" + trackArtist : lastKey);
        if ((playing || isAd) && key != lastKey)
        {
            lastKey = key;
            posSec = 0; durSec = 0;
            adInfo = "";
            if (Config.Popup) ShowPopup();
            if (Config.Toast && !Config.Popup && !isAd) { toastPending = true; toastAt = DateTime.Now.AddMilliseconds(1200); }
            if (host != null) host.RefreshThumb();
            Thread t2 = new Thread(LoadCover);
            t2.IsBackground = true;
            t2.Start();
            changed = true;
        }

        if (changed) Invalidate();

        rpc.Update(Config.Discord, DiscordRpc.DefaultId, trackTitle, trackArtist, playing && !isAd, posSec, durSec, running && hasSong && !isAd, Config.DiscordCover);
        Streamer.Update(trackTitle, trackArtist, playing, running && hasSong && !isAd, posSec, durSec);
        ApplyProfiles();
        UpdateDodge();
        if (toastPending && DateTime.Now >= toastAt) { toastPending = false; ShowToast(); }
    }

    [STAThread]
    static void Main()
    {
        bool created;
        using (Mutex m = new Mutex(true, "SpotifyOverlay_SingleInstance", out created))
        {
            if (!created) return;
            Config.Load();
            Application.EnableVisualStyles();
            Application.Run(new Overlay());
        }
    }
}
