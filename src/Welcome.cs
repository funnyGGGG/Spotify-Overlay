// Spotify Overlay - funny.games
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

// Kurze Tour beim ersten Start
class WelcomeForm : Form
{
    [DllImport("user32.dll")] static extern bool ReleaseCapture();
    [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr h, int msg, int w, int l);
    [DllImport("gdi32.dll")] static extern IntPtr CreateRoundRectRgn(int l, int t, int r, int b, int w, int h);

    static readonly Color BG = Color.FromArgb(24, 24, 24);
    static readonly Color SIDE = Color.FromArgb(16, 16, 16);
    static readonly Color GREEN = Color.FromArgb(29, 185, 84);
    const int Pages = 5;

    int page = 0;
    Image logo;
    FlatBtn bNext, bBack, bSkip;

    public WelcomeForm()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(640, 360);
        BackColor = BG;
        TopMost = true;
        ShowInTaskbar = false;
        Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, Width + 1, Height + 1, 14, 14));
        try
        {
            using (Stream rs = Assembly.GetExecutingAssembly().GetManifestResourceStream("cover.png"))
                if (rs != null) logo = new Bitmap(rs);
        }
        catch { }

        bSkip = new FlatBtn("", BG, Color.FromArgb(170, 170, 170));
        bSkip.Location = new Point(236, 300);
        bSkip.Size = new Size(110, 38);
        bSkip.Click += delegate { Finish(); };
        bBack = new FlatBtn("", Color.FromArgb(40, 40, 40), Color.White);
        bBack.Location = new Point(366, 300);
        bBack.Size = new Size(100, 38);
        bBack.Click += delegate { if (page > 0) { page--; Update2(); } };
        bNext = new FlatBtn("", GREEN, Color.Black);
        bNext.Location = new Point(476, 300);
        bNext.Size = new Size(140, 38);
        bNext.Click += delegate { if (page < Pages - 1) { page++; Update2(); } else Finish(); };
        Controls.Add(bSkip);
        Controls.Add(bBack);
        Controls.Add(bNext);
        Update2();
    }

    void Finish()
    {
        Config.Welcome = true;
        Config.Save();
        Close();
    }

    void Update2()
    {
        bSkip.Text = Lang.T("tour_skip");
        bBack.Text = Lang.T("tour_back");
        bBack.Visible = page > 0;
        bNext.Text = page == Pages - 1 ? Lang.T("tour_done") : Lang.T("tour_next");
        bSkip.Visible = page < Pages - 1;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        g.Clear(BG);
        using (SolidBrush b = new SolidBrush(SIDE)) g.FillRectangle(b, 0, 0, 200, Height);
        if (logo != null) g.DrawImage(logo, new Rectangle(52, 90, 96, 96));

        using (Font f1 = new Font("Segoe UI", 15f, FontStyle.Bold, GraphicsUnit.Pixel))
        using (Font f2 = new Font("Segoe UI", 12f, FontStyle.Regular, GraphicsUnit.Pixel))
        using (Font f3 = new Font("Segoe UI", 24f, FontStyle.Bold, GraphicsUnit.Pixel))
        using (Font f4 = new Font("Segoe UI", 16f, FontStyle.Regular, GraphicsUnit.Pixel))
        using (SolidBrush white = new SolidBrush(Color.White))
        using (SolidBrush grey = new SolidBrush(Color.FromArgb(160, 160, 160)))
        using (SolidBrush green = new SolidBrush(GREEN))
        using (StringFormat center = new StringFormat())
        {
            center.Alignment = StringAlignment.Center;
            g.DrawString("Spotify Overlay", f1, white, new RectangleF(0, 204, 200, 24), center);
            g.DrawString("funny.games", f2, grey, new RectangleF(0, 228, 200, 20), center);

            string n = (page + 1).ToString();
            g.DrawString(Lang.T("tour" + n + "_t"), f3, white, new RectangleF(236, 60, 380, 80));
            g.DrawString(Lang.T("tour" + n + "_x"), f4, grey, new RectangleF(236, 150, 380, 140));

            for (int i = 0; i < Pages; i++)
            {
                if (i == page) g.FillEllipse(green, 236 + i * 22, 34, 10, 10);
                else g.FillEllipse(new SolidBrush(Color.FromArgb(70, 70, 70)), 236 + i * 22, 34, 10, 10);
            }
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && e.Y < 50) { ReleaseCapture(); SendMessage(Handle, 0xA1, 2, 0); }
        base.OnMouseDown(e);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        if (!Config.Welcome) { Config.Welcome = true; Config.Save(); }
        base.OnFormClosed(e);
    }
}
