// Spotify Overlay - funny.games
using System;
using System.Drawing;
using System.Windows.Forms;

// Anleitung in der App
class HelpForm : Form
{
    public HelpForm()
    {
        Text = Lang.T("help_title");
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(560, 600);
        BackColor = Color.FromArgb(24, 24, 24);
        try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

        TextBox t = new TextBox();
        t.Multiline = true;
        t.ReadOnly = true;
        t.ScrollBars = ScrollBars.Vertical;
        t.Dock = DockStyle.Fill;
        t.BorderStyle = BorderStyle.None;
        t.BackColor = Color.FromArgb(24, 24, 24);
        t.ForeColor = Color.White;
        t.Font = new Font("Segoe UI", 10f);
        t.Text = Lang.T("help_text").Replace("\n", "\r\n");
        Controls.Add(t);
        t.SelectionStart = 0;
        t.SelectionLength = 0;
    }
}
