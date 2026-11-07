// Spotify Overlay - funny.games
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

class LyricLine
{
    public double Time;   // -1 = ohne Zeitangabe
    public string Text;
}

// Songtexte von lrclib.net (offene Schnittstelle, kein Konto noetig)
static class LyricsClient
{
    static string Get(string url)
    {
        ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
        HttpWebRequest rq = (HttpWebRequest)WebRequest.Create(url);
        rq.UserAgent = "SpotifyOverlay (" + Contact.GitHub + ")";
        rq.Timeout = 8000;
        try
        {
            using (WebResponse rs = rq.GetResponse())
            using (StreamReader sr = new StreamReader(rs.GetResponseStream()))
                return sr.ReadToEnd();
        }
        catch (WebException ex)
        {
            HttpWebResponse r = ex.Response as HttpWebResponse;
            if (r != null && (int)r.StatusCode == 404) return null;
            throw;
        }
    }

    static string Esc(string s) { return Uri.EscapeDataString(s ?? ""); }

    // true = etwas gefunden (Text oder "instrumental")
    public static bool Fetch(string artist, string title, double dur, out List<LyricLine> lines, out bool synced, out bool instrumental)
    {
        lines = new List<LyricLine>();
        synced = false;
        instrumental = false;

        string q = "artist_name=" + Esc(artist) + "&track_name=" + Esc(title) + (dur > 1 ? "&duration=" + (int)Math.Round(dur) : "");
        string json = Get("https://lrclib.net/api/get?" + q);
        object o = json == null ? null : Json.Parse(json);
        if (o == null)
        {
            string sj = Get("https://lrclib.net/api/search?artist_name=" + Esc(artist) + "&track_name=" + Esc(title));
            List<object> arr = sj == null ? null : Json.Parse(sj) as List<object>;
            if (arr != null && arr.Count > 0) o = arr[0];
        }
        if (o == null) return false;

        object ins = Json.Get(o, "instrumental");
        if (ins is bool && (bool)ins) { instrumental = true; return true; }

        string sync = Json.Str(o, "syncedLyrics"), plain = Json.Str(o, "plainLyrics");
        if (sync.Length > 0)
        {
            foreach (string raw in sync.Split('\n'))
            {
                MatchCollection ms = Regex.Matches(raw, @"\[(\d+):(\d+(?:\.\d+)?)\]");
                if (ms.Count == 0) continue;
                string text = Regex.Replace(raw, @"\[[^\]]*\]", "").Trim();
                foreach (Match m in ms)
                {
                    LyricLine l = new LyricLine();
                    l.Time = int.Parse(m.Groups[1].Value) * 60 + double.Parse(m.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);
                    l.Text = text.Length == 0 ? "♪" : text;
                    lines.Add(l);
                }
            }
            lines.Sort(delegate(LyricLine a, LyricLine b) { return a.Time.CompareTo(b.Time); });
            synced = lines.Count > 0;
        }
        if (!synced && plain.Length > 0)
        {
            foreach (string raw in plain.Split('\n'))
            {
                LyricLine l = new LyricLine();
                l.Time = -1;
                l.Text = raw.TrimEnd('\r');
                lines.Add(l);
            }
        }
        return lines.Count > 0;
    }
}

// Eigenes Fenster fuer den Songtext: bleibt oben, nimmt dem Spiel nie den Fokus
class LyricsForm : Form
{
    [DllImport("gdi32.dll")]
    static extern IntPtr CreateRoundRectRgn(int l, int t, int r, int b, int w, int h);

    static readonly Color BG = Color.FromArgb(22, 22, 22);

    Overlay ov;
    List<LyricLine> lines = new List<LyricLine>();
    bool synced;
    string status = "";
    string key = "";
    int cur = -1;
    float scroll = 0f;
    DateTime manualUntil = DateTime.MinValue;
    float[] heights = new float[0];
    int measuredWidth = -1;
    Font fLine, fCur, fHead, fSmall;
    System.Windows.Forms.Timer timer;

    public LyricsForm(Overlay overlay)
    {
        ov = overlay;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        BackColor = BG;
        StartPosition = FormStartPosition.Manual;
        MinimumSize = new Size(260, 220);
        Size = new Size(Math.Max(260, Config.LyW), Math.Max(220, Config.LyH));
        Rectangle wa = Screen.PrimaryScreen.WorkingArea;
        Location = new Point(wa.Right - Width - 20, wa.Top + 120);
        if (Config.LyX != int.MinValue && Config.LyY != int.MinValue)
        {
            Point p = new Point(Config.LyX, Config.LyY);
            foreach (Screen sc in Screen.AllScreens)
                if (sc.WorkingArea.Contains(p)) { Location = p; break; }
        }
        ApplyOpacity();
        Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, Width + 1, Height + 1, 14, 14));

        fLine = new Font("Segoe UI", 15f, FontStyle.Regular, GraphicsUnit.Pixel);
        fCur = new Font("Segoe UI", 15f, FontStyle.Bold, GraphicsUnit.Pixel);
        fHead = new Font("Segoe UI", 13f, FontStyle.Bold, GraphicsUnit.Pixel);
        fSmall = new Font("Segoe UI", 11f, FontStyle.Regular, GraphicsUnit.Pixel);

        timer = new System.Windows.Forms.Timer();
        timer.Interval = 70;
        timer.Tick += delegate { Step(); };
        timer.Start();

        ResizeEnd += delegate
        {
            Config.LyX = Left; Config.LyY = Top; Config.LyW = Width; Config.LyH = Height;
            Config.Save();
            Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, Width + 1, Height + 1, 14, 14));
        };
        Resize += delegate { measuredWidth = -1; Invalidate(); };
    }

    public void ApplyOpacity()
    {
        Opacity = Math.Max(0.6, Math.Min(1.0, Config.Opacity / 100.0 + 0.1));
    }

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams cp = base.CreateParams;
            cp.ExStyle |= 0x08000000 | 0x80;   // kein Fokus, nicht in Alt+Tab
            return cp;
        }
    }

    protected override bool ShowWithoutActivation { get { return true; } }

    // Fenster verschieben (Kopfzeile) und Groesse aendern (Ecke unten rechts) macht Windows selbst
    protected override void WndProc(ref Message m)
    {
        if (m.Msg == 0x0084)   // WM_NCHITTEST
        {
            Point p = PointToClient(new Point((short)((long)m.LParam & 0xFFFF), (short)(((long)m.LParam >> 16) & 0xFFFF)));
            if (p.X > Width - 18 && p.Y > Height - 18) { m.Result = (IntPtr)17; return; }   // HTBOTTOMRIGHT
            if (p.Y < 40 && p.X < Width - 44) { m.Result = (IntPtr)2; return; }              // HTCAPTION
        }
        base.WndProc(ref m);
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        if (e.X > Width - 40 && e.Y < 36) Close();
        base.OnMouseClick(e);
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        scroll -= e.Delta / 2f;
        manualUntil = DateTime.Now.AddSeconds(5);
        Invalidate();
        base.OnMouseWheel(e);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        timer.Stop();
        timer.Dispose();
        base.OnFormClosed(e);
    }

    // Song gewechselt? Dann Text neu holen
    void CheckTrack()
    {
        string k = ov.CurArtist + "|" + ov.CurTitle + "|" + ov.IsAd + "|" + ov.IsRunning;
        if (k == key) return;
        key = k;
        lines = new List<LyricLine>();
        synced = false;
        cur = -1;
        scroll = 0;
        measuredWidth = -1;

        if (ov.IsAd) { status = Lang.T("ly_ad"); Invalidate(); return; }
        if (ov.CurTitle.Length == 0) { status = Lang.T("ly_wait"); Invalidate(); return; }

        status = Lang.T("ly_loading");
        string artist = ov.CurArtist, title = ov.CurTitle, mine = key;
        double dur = ov.DurSec;
        Invalidate();

        Thread t = new Thread(delegate()
        {
            List<LyricLine> res = null;
            bool sy = false, ins = false, found = false;
            try { found = LyricsClient.Fetch(artist, title, dur, out res, out sy, out ins); } catch { }
            try
            {
                BeginInvoke(new Action(delegate
                {
                    if (mine != key) return;   // inzwischen anderer Song
                    if (!found) status = Lang.T("ly_none");
                    else if (ins) status = Lang.T("ly_instr");
                    else { lines = res; synced = sy; status = ""; }
                    measuredWidth = -1;
                    Invalidate();
                }));
            }
            catch { }
        });
        t.IsBackground = true;
        t.Start();
    }

    void Layout(Graphics g, int w)
    {
        if (measuredWidth == w && heights.Length == lines.Count) return;
        heights = new float[lines.Count];
        for (int i = 0; i < lines.Count; i++)
            heights[i] = Math.Max(22f, g.MeasureString(lines[i].Text.Length == 0 ? " " : lines[i].Text, fLine, w).Height + 8f);
        measuredWidth = w;
    }

    void Step()
    {
        CheckTrack();
        if (lines.Count == 0 || !synced) return;

        double pos = ov.PosSec;
        int c = -1;
        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i].Time <= pos + 0.15) c = i; else break;
        }
        bool changed = c != cur;
        cur = c;

        if (DateTime.Now > manualUntil && c >= 0 && heights.Length == lines.Count)
        {
            float y = 0;
            for (int i = 0; i < c; i++) y += heights[i];
            float target = y + heights[c] / 2f - (Height - 80) / 2f;
            if (Math.Abs(target - scroll) > 0.6f) { scroll += (target - scroll) * 0.22f; changed = true; }
        }
        if (changed) Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(BG);

        using (SolidBrush white = new SolidBrush(Color.White))
        using (SolidBrush grey = new SolidBrush(Color.FromArgb(120, 120, 120)))
        using (SolidBrush mid = new SolidBrush(Color.FromArgb(165, 165, 165)))
        using (SolidBrush green = new SolidBrush(Color.FromArgb(29, 185, 84)))
        using (StringFormat center = new StringFormat())
        {
            center.Alignment = StringAlignment.Center;

            // Kopfzeile
            g.DrawString(Lang.T("ly_title"), fHead, green, 18, 12);
            string who = ov.CurTitle.Length > 0 ? ov.CurTitle + " - " + ov.CurArtist : "";
            using (StringFormat tr = new StringFormat(StringFormatFlags.NoWrap))
            {
                tr.Trimming = StringTrimming.EllipsisCharacter;
                g.DrawString(who, fSmall, mid, new RectangleF(86, 14, Width - 140, 16), tr);
            }
            g.DrawString("\u2715", fHead, mid, Width - 30, 11);
            using (Pen p = new Pen(Color.FromArgb(44, 44, 44))) g.DrawLine(p, 14, 38, Width - 14, 38);

            Rectangle area = new Rectangle(18, 44, Width - 36, Height - 44 - 26);
            g.SetClip(area);

            if (status.Length > 0 || lines.Count == 0)
            {
                string msg = status.Length > 0 ? status : Lang.T("ly_none");
                g.DrawString(msg, fLine, mid, new RectangleF(area.X, area.Y + area.Height / 2 - 30, area.Width, 80), center);
            }
            else
            {
                Layout(g, area.Width);
                float y = area.Y - scroll;
                for (int i = 0; i < lines.Count; i++)
                {
                    float h = heights[i];
                    if (y + h >= area.Y && y <= area.Bottom)
                    {
                        bool isCur = synced && i == cur;
                        g.DrawString(lines[i].Text, isCur ? fCur : fLine, isCur ? white : (synced ? grey : mid),
                                     new RectangleF(area.X, y + 4, area.Width, h), center);
                    }
                    y += h;
                }
            }
            g.ResetClip();
            g.DrawString(Lang.T("ly_credit"), fSmall, grey, 18, Height - 22);
        }
        using (Pen gp = new Pen(Color.FromArgb(100, 100, 100), 1.3f))
            for (int k = 1; k <= 3; k++)
                g.DrawLine(gp, Width - 2 - k * 4, Height - 3, Width - 3, Height - 2 - k * 4);
    }
}
