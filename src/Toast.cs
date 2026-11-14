// Spotify Overlay - funny.games
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

// Kleine Animation bei einem neuen Song: Cover, Titel und Interpret gleiten kurz herein und wieder hinaus
class SongToast : Form
{
    const int W = 340, H = 88;
    const double InMs = 350, StayMs = 3600, OutMs = 350;

    Image cover;
    string title, artist;
    Color bg, acc, txt, sub;
    System.Windows.Forms.Timer timer;
    DateTime t0;
    int fromX, toX;

    public SongToast(Image cover, string title, string artist, Color bg, Color acc, Color txt, Color sub)
    {
        this.cover = cover;
        this.title = title ?? "";
        this.artist = artist ?? "";
        this.bg = bg;
        this.acc = acc;
        this.txt = txt;
        this.sub = sub;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        BackColor = bg;
        ClientSize = new Size(W, H);
        Rectangle wa = Screen.PrimaryScreen.WorkingArea;
        toX = wa.Right - W - 20;
        fromX = wa.Right;
        Location = new Point(fromX, wa.Bottom - H - 20);
        Opacity = 0;
        using (GraphicsPath gp = Round(new Rectangle(0, 0, W, H), 14)) Region = new Region(gp);
        timer = new System.Windows.Forms.Timer();
        timer.Interval = 15;
        timer.Tick += delegate { Step(); };
    }

    protected override bool ShowWithoutActivation { get { return true; } }

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams cp = base.CreateParams;
            cp.ExStyle |= 0x08000000 | 0x80 | 0x20;   // kein Fokus, nicht in Alt+Tab, Maus geht durch
            return cp;
        }
    }

    public void Start()
    {
        t0 = DateTime.Now;
        Show();
        timer.Start();
    }

    void Step()
    {
        double ms = (DateTime.Now - t0).TotalMilliseconds;
        if (ms < InMs)
        {
            double k = ms / InMs;
            k = 1 - (1 - k) * (1 - k) * (1 - k);
            Left = fromX + (int)((toX - fromX) * k);
            Opacity = 0.95 * k;
        }
        else if (ms < InMs + StayMs)
        {
            Left = toX;
            Opacity = 0.95;
        }
        else if (ms < InMs + StayMs + OutMs)
        {
            double k = (ms - InMs - StayMs) / OutMs;
            Left = toX + (int)((fromX - toX) * k * 0.3);
            Opacity = 0.95 * (1 - k);
        }
        else
        {
            timer.Stop();
            Close();
        }
    }

    static GraphicsPath Round(Rectangle r, int d)
    {
        GraphicsPath p = new GraphicsPath();
        int dd = d * 2;
        p.AddArc(r.X, r.Y, dd, dd, 180, 90);
        p.AddArc(r.Right - dd, r.Y, dd, dd, 270, 90);
        p.AddArc(r.Right - dd, r.Bottom - dd, dd, dd, 0, 90);
        p.AddArc(r.X, r.Bottom - dd, dd, dd, 90, 90);
        p.CloseFigure();
        return p;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        g.Clear(bg);
        using (SolidBrush ab = new SolidBrush(acc)) g.FillRectangle(ab, 0, 0, 5, H);
        if (cover != null)
        {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            using (GraphicsPath gp = Round(new Rectangle(18, 12, 64, 64), 10))
            {
                g.SetClip(gp);
                g.DrawImage(cover, new Rectangle(18, 12, 64, 64));
                g.ResetClip();
            }
        }
        int tx = cover != null ? 96 : 20;
        RectangleF r1 = new RectangleF(tx, 16, W - tx - 14, 26);
        RectangleF r2 = new RectangleF(tx, 44, W - tx - 14, 22);
        using (StringFormat sf = new StringFormat())
        using (Font f1 = new Font("Segoe UI", 12.5f, FontStyle.Bold))
        using (Font f2 = new Font("Segoe UI", 10f))
        using (SolidBrush b1 = new SolidBrush(txt))
        using (SolidBrush b2 = new SolidBrush(sub))
        {
            sf.Trimming = StringTrimming.EllipsisCharacter;
            sf.FormatFlags = StringFormatFlags.NoWrap;
            g.DrawString(title, f1, b1, r1, sf);
            g.DrawString(artist, f2, b2, r2, sf);
        }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        timer.Stop();
        timer.Dispose();
        if (cover != null) cover.Dispose();
        base.OnFormClosed(e);
    }
}
