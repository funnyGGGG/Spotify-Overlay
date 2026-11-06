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
    const float BaseW = 352f;
    static readonly int[] BtnX = { 228, 252, 276 };
    static readonly string[] AdWords = { "advertisement", "werbung", "anuncio", "publicidad", "publicité", "publicite", "pubblicità", "pubblicita", "reclame" };
    static readonly Color BG = Color.FromArgb(24, 24, 24);
    static readonly Color GREEN = Color.FromArgb(29, 185, 84);
    static readonly Color ACC = Color.FromArgb(232, 120, 40);

    // Fenster und Helfer
    NotifyIcon tray;
    TaskbarHost host;
    SettingsForm settings;
    PlaylistForm playlists;
    UpdateForm updateWin;
    Icon appIcon;
    Image cover;
    Bitmap coverScaled;
    Font fTitle, fSub, fTiny;
    DiscordRpc rpc = new DiscordRpc();
    System.Windows.Forms.Timer hideTimer;
    DateTime lastToggle = DateTime.MinValue;
    public bool Exiting = false;

    // Werte, die der Hintergrund-Thread liefert
    readonly object stateLock = new object();
    string wFound = null;
    bool wRunning = false, wAd = false;
    IntPtr wHwnd = IntPtr.Zero;
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
            cp.ExStyle |= 0x08000000 | 0x80;   // NOACTIVATE + TOOLWINDOW: nimmt dem Spiel nie den Fokus
            return cp;
        }
    }

    protected override bool ShowWithoutActivation { get { return true; } }

    // Befehl direkt an das Spotify-Fenster schicken: sofort und nur fuer Spotify (nicht fuer YouTube & Co).
    // Gibt es das Fenster nicht, wird die Windows-Mediensitzung von Spotify benutzt.
    void Cmd(string name, int appCommand)
    {
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
        int n = 0;
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
                float v = -1f;
                if (Config.Volume && !volDrag && n % 2 == 0) v = SpotifyAudio.Get();
                lock (stateLock)
                {
                    wRunning = r;
                    wHwnd = main;
                    wFound = found;
                    wAd = ad && found == null;
                    if (Config.Volume && n % 2 == 0 && !volDrag) wVol = v;
                }
            }
            catch { }
            n++;
            Thread.Sleep(1000);
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
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        FormBorderStyle = FormBorderStyle.None;
        TopMost = true;
        ShowInTaskbar = false;
        BackColor = BG;
        StartPosition = FormStartPosition.Manual;
        scale = Config.Scale / 100f;
        Opacity = Config.Opacity / 100.0;
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
        poll.Interval = 1000;
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

        ApplyScale(scale);
        if (Config.Popup) hideTimer.Start();
    }

    void BuildMenu()
    {
        ContextMenuStrip m = new ContextMenuStrip();
        m.Items.Add(Lang.T("tray_toggle"), null, delegate { TaskbarClick(); });
        m.Items.Add(Lang.T("tray_playlists"), null, delegate { OpenPlaylists(); });
        m.Items.Add(Lang.T("tray_settings"), null, delegate { OpenSettings(); });
        m.Items.Add(Lang.T("tray_update"), null, delegate { StartUpdateCheck(true, null); });
        m.Items.Add(new ToolStripSeparator());
        m.Items.Add(Lang.T("tray_exit"), null, delegate { ExitAll(); });
        tray.ContextMenuStrip = m;
    }

    void OpenSettings()
    {
        if (settings == null || settings.IsDisposed) settings = new SettingsForm(this);
        settings.Show();
        settings.Activate();
    }

    void OpenPlaylists()
    {
        if (playlists == null || playlists.IsDisposed) playlists = new PlaylistForm();
        playlists.Show();
        playlists.Activate();
    }

    // Wird von den Einstellungen aufgerufen
    public void ApplySettings()
    {
        Opacity = Config.Opacity / 100.0;
        BuildMenu();
        ApplyScale(scale);
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
        if (i == 3) return new RectangleF(300 * scale, 7 * scale, 22 * scale, 22 * scale);
        if (i == 4) return new RectangleF(326 * scale, 7 * scale, 22 * scale, 22 * scale);
        return new RectangleF(BtnX[i] * scale, 5 * scale, 24 * scale, 24 * scale);
    }

    RectangleF VolRect() { return new RectangleF(70 * scale, 63 * scale, 214 * scale, 20 * scale); }
    RectangleF SeekRect() { return new RectangleF(56 * scale, 50 * scale, 278 * scale, 12 * scale); }
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
        g.Clear(BG);
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
                g.DrawString(FormatTime(durSec), fTiny, grey, new RectangleF(274 * s, 40 * s, 60 * s, 14 * s), right);
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
        float bx = 56 * s, bw = 278 * s, by = 56 * s, bh = 3 * s;
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

        for (int i = 0; i < 3; i++) DrawBtn(g, i, BtnRect(i), hoverBtn == i, playing);
        DrawList(g, BtnRect(3), hoverBtn == 3);
        DrawGear(g, BtnRect(4), hoverBtn == 4);

        // Anfasser zum Groesse aendern
        using (Pen gp = new Pen(Color.FromArgb(110, 110, 110), 1.3f))
            for (int k = 1; k <= 3; k++)
                g.DrawLine(gp, Width - 2 - k * 4, Height - 3, Width - 3, Height - 2 - k * 4);
    }

    protected override void OnPaintBackground(PaintEventArgs e) { }

    protected override void OnPaint(PaintEventArgs e)
    {
        Render(e.Graphics);
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
        for (int i = 0; i < 5; i++) if (BtnRect(i).Contains(p)) return i;
        return -1;
    }

    double SeekFrac(int x)
    {
        return Math.Max(0, Math.Min(1, (x - 56 * scale) / (278 * scale)));
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
            else if (pressedBtn == 3) OpenPlaylists();
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

    void SavePosition()
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
        host = new TaskbarHost(this, appIcon);
        host.Show();
        SetWindowPos(Handle, (IntPtr)(-1), 0, 0, 0, 0, 0x0013);
        HotKeys.Apply(Handle);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == 0x0312) { HotKeyPressed((int)m.WParam); return; }
        base.WndProc(ref m);
    }

    void HotKeyPressed(int id)
    {
        if (id == 1) ChangeVol(0.05f);
        else if (id == 2) ChangeVol(-0.05f);
        else if (id == 3) Cmd("next", CMD_NEXT);
        else if (id == 4) Cmd("prev", CMD_PREV);
        else if (id == 5) Cmd("play", CMD_PLAY);
        else if (id == 6) TaskbarClick();
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
        SavePosition();
        rpc.Close();
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

    // ---------- Sekundentakt (leicht) ----------
    void Tick()
    {
        string found;
        bool r, ad;
        float v;
        lock (stateLock) { found = wFound; r = wRunning; ad = wAd; v = wVol; }

        bool changed = false;
        if (r != running) { running = r; changed = true; }

        if (!r)
        {
            playing = false; hasSong = false; isAd = false;
            if (trackTitle != "") { trackTitle = ""; trackArtist = ""; changed = true; }
        }
        else if (ad)
        {
            if (!isAd) changed = true;
            isAd = true;
            playing = true;
        }
        else
        {
            if (isAd) changed = true;
            isAd = false;
            if (found != null)
            {
                int i = found.IndexOf(" - ");
                string a = found.Substring(0, i), t = found.Substring(i + 3);
                if (a != trackArtist || t != trackTitle) changed = true;
                trackArtist = a;
                trackTitle = t;
                if (!hasSong) changed = true;
                hasSong = true;
                if (!playing) changed = true;
                playing = true;
            }
            else
            {
                if (playing) changed = true;
                playing = false;
            }
        }

        DateTime now = DateTime.Now;
        double dt = (now - lastTick).TotalSeconds;
        lastTick = now;
        if (playing && durSec > 0 && !seeking)
        {
            posSec += dt;
            if (posSec > durSec) posSec = durSec;
            changed = true;
        }

        tick++;
        if (playing && durSec > 0 && tick % 10 == 0 && !seeking)
        {
            Thread ts = new Thread(SyncTimeline);
            ts.IsBackground = true;
            ts.Start();
        }

        if (Config.Volume && !volDrag && DateTime.Now > volHold && Math.Abs(v - vol) > 0.001f) { vol = v; changed = true; }

        string key = isAd ? "ad" : (playing ? trackTitle : lastKey);
        if (playing && key != lastKey)
        {
            lastKey = key;
            posSec = 0; durSec = 0;
            adInfo = "";
            if (Config.Popup) ShowPopup();
            if (host != null) host.RefreshThumb();
            Thread t = new Thread(LoadCover);
            t.IsBackground = true;
            t.Start();
            changed = true;
        }

        if (changed) Invalidate();

        rpc.Update(Config.Discord, DiscordRpc.EffectiveId(Config.DiscordId), trackTitle, trackArtist, playing && !isAd, posSec, durSec, running && hasSong && !isAd);
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
