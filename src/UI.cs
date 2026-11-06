// Spotify Overlay - funny.games
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

// Flacher Button im dunklen Stil
class FlatBtn : Button
{
    public FlatBtn(string text, Color bg, Color fg)
    {
        Text = text;
        BackColor = bg;
        ForeColor = fg;
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        FlatAppearance.MouseOverBackColor = ControlPaint.Light(bg, 0.25f);
        FlatAppearance.MouseDownBackColor = ControlPaint.Dark(bg, 0.1f);
        Font = new Font("Segoe UI Semibold", 10f);
        Cursor = Cursors.Hand;
        TabStop = false;
        Size = new Size(130, 36);
    }
}

// Fortschrittsbalken (gruen -> orange)
class Bar : Control
{
    int val;

    public int Value
    {
        get { return val; }
        set { val = Math.Max(0, Math.Min(100, value)); Invalidate(); }
    }

    public Bar()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.Clear(Color.FromArgb(48, 48, 48));
        int w = (int)(Width * val / 100.0);
        if (w < 2) return;
        using (LinearGradientBrush b = new LinearGradientBrush(new Rectangle(0, 0, w, Height), Color.FromArgb(29, 185, 84), Color.FromArgb(232, 120, 40), 0f))
            g.FillRectangle(b, 0, 0, w, Height);
    }
}

// Feld, in das man ein Tastenkuerzel eintippt (jede Taste, mit oder ohne Strg/Alt/Umschalt)
class KeyBox : TextBox
{
    public KeyBox()
    {
        ReadOnly = true;
        BorderStyle = BorderStyle.FixedSingle;
        BackColor = Color.FromArgb(40, 40, 40);
        ForeColor = Color.White;
        Cursor = Cursors.Hand;
    }

    protected override bool IsInputKey(Keys keyData) { return true; }

    // ProcessCmdKey bekommt wirklich alle Tasten, auch F10, Alt+... und die Medientasten
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        Keys k = keyData & Keys.KeyCode;
        if (k == Keys.Back || k == Keys.Delete) { Text = ""; return true; }
        if (k == Keys.ControlKey || k == Keys.ShiftKey || k == Keys.Menu || k == Keys.LWin || k == Keys.RWin) return true;
        if (k == Keys.None) return true;

        string s = "";
        if ((keyData & Keys.Control) != 0) s += "Ctrl+";
        if ((keyData & Keys.Alt) != 0) s += "Alt+";
        if ((keyData & Keys.Shift) != 0) s += "Shift+";
        Text = s + k.ToString();
        return true;
    }
}

// Schalter (an/aus) mit Beschriftung
class Toggle : Control
{
    bool on;
    public event EventHandler CheckedChanged;

    public bool Checked
    {
        get { return on; }
        set
        {
            if (on == value) return;
            on = value;
            Invalidate();
            if (CheckedChanged != null) CheckedChanged(this, EventArgs.Empty);
        }
    }

    public Toggle()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw, true);
        Cursor = Cursors.Hand;
        Height = 26;
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10f);
    }

    protected override void OnClick(EventArgs e)
    {
        Checked = !on;
        base.OnClick(e);
    }

    protected override void OnTextChanged(EventArgs e) { Invalidate(); base.OnTextChanged(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        g.Clear(Parent != null ? Parent.BackColor : Color.Black);

        int w = 36, h = 20, y = (Height - h) / 2;
        using (GraphicsPath p = new GraphicsPath())
        {
            p.AddArc(0, y, h, h, 90, 180);
            p.AddArc(w - h, y, h, h, 270, 180);
            p.CloseFigure();
            using (SolidBrush b = new SolidBrush(on ? Color.FromArgb(29, 185, 84) : Color.FromArgb(74, 74, 74)))
                g.FillPath(b, p);
        }
        g.FillEllipse(Brushes.White, on ? w - h + 3 : 3, y + 3, h - 6, h - 6);

        using (StringFormat sf = new StringFormat(StringFormatFlags.NoWrap))
        using (SolidBrush t = new SolidBrush(ForeColor))
        {
            sf.LineAlignment = StringAlignment.Center;
            sf.Trimming = StringTrimming.EllipsisCharacter;
            g.DrawString(Text, Font, t, new RectangleF(w + 12, 0, Width - w - 12, Height), sf);
        }
    }
}

// Schieberegler im selben Stil
class Slider : Control
{
    int min = 0, max = 100, val = 50;
    bool drag;
    public event EventHandler ValueChanged;

    public int Minimum { get { return min; } set { min = value; Invalidate(); } }
    public int Maximum { get { return max; } set { max = value; Invalidate(); } }
    public int Value
    {
        get { return val; }
        set
        {
            int v = Math.Max(min, Math.Min(max, value));
            if (v == val) return;
            val = v;
            Invalidate();
            if (ValueChanged != null) ValueChanged(this, EventArgs.Empty);
        }
    }

    public Slider()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw, true);
        Cursor = Cursors.Hand;
        Height = 24;
    }

    void FromX(int x)
    {
        double f = (x - 8) / (double)(Width - 16);
        Value = min + (int)Math.Round(f * (max - min));
    }

    protected override void OnMouseDown(MouseEventArgs e) { drag = true; FromX(e.X); base.OnMouseDown(e); }
    protected override void OnMouseMove(MouseEventArgs e) { if (drag) FromX(e.X); base.OnMouseMove(e); }
    protected override void OnMouseUp(MouseEventArgs e) { drag = false; base.OnMouseUp(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Parent != null ? Parent.BackColor : Color.Black);
        float f = (val - min) / (float)(max - min);
        float x = 8 + f * (Width - 16), cy = Height / 2f;
        using (SolidBrush t = new SolidBrush(Color.FromArgb(70, 70, 70)))
            g.FillRectangle(t, 8, cy - 2, Width - 16, 4);
        using (SolidBrush b = new SolidBrush(Color.FromArgb(29, 185, 84)))
            g.FillRectangle(b, 8, cy - 2, x - 8, 4);
        g.FillEllipse(Brushes.White, x - 8, cy - 8, 16, 16);
    }
}

// Eintrag in der Seitenleiste der Einstellungen
class NavItem : Control
{
    bool sel, hover;

    public bool Selected { get { return sel; } set { sel = value; Invalidate(); } }

    public NavItem()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw, true);
        Cursor = Cursors.Hand;
        Height = 40;
        Font = new Font("Segoe UI", 10.5f);
    }

    protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnTextChanged(EventArgs e) { Invalidate(); base.OnTextChanged(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        g.Clear(sel ? Color.FromArgb(34, 34, 34) : (hover ? Color.FromArgb(26, 26, 26) : Color.FromArgb(18, 18, 18)));
        if (sel)
            using (SolidBrush b = new SolidBrush(Color.FromArgb(29, 185, 84)))
                g.FillRectangle(b, 0, 0, 3, Height);
        using (StringFormat sf = new StringFormat(StringFormatFlags.NoWrap))
        using (SolidBrush t = new SolidBrush(sel ? Color.White : Color.FromArgb(170, 170, 170)))
        {
            sf.LineAlignment = StringAlignment.Center;
            g.DrawString(Text, Font, t, new RectangleF(20, 0, Width - 24, Height), sf);
        }
    }
}
