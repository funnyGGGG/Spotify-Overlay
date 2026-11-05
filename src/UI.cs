// Spotify Overlay - funny.games
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
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

// Feld, in das man ein Tastenkuerzel eintippt (jede Taste, mit oder ohne Strg/Alt/Umschalt/Win)
class KeyBox : TextBox
{
    [DllImport("user32.dll")]
    static extern short GetAsyncKeyState(int vk);

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
        bool win = (GetAsyncKeyState(0x5B) & 0x8000) != 0 || (GetAsyncKeyState(0x5C) & 0x8000) != 0;
        if (win) s += "Win+";
        if ((keyData & Keys.Control) != 0) s += "Ctrl+";
        if ((keyData & Keys.Alt) != 0) s += "Alt+";
        if ((keyData & Keys.Shift) != 0) s += "Shift+";
        Text = s + k.ToString();
        return true;
    }
}
