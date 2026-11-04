// Spotify Overlay - funny.games
using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;

class SettingsForm : Form
{
    static readonly Color BG = Color.FromArgb(24, 24, 24);
    static readonly Color PANEL = Color.FromArgb(40, 40, 40);
    static readonly string[] HkText = { "hk_volup", "hk_voldown", "hk_next", "hk_prev", "hk_play", "hk_toggle" };

    Overlay ov;
    Label lLang, lOpacity, lId, lStatus, lKeys, lHint, lTip;
    ComboBox cbLang;
    TrackBar tbOpacity;
    CheckBox cPopup, cVolume, cDiscord, cUpdate, cAuto;
    TextBox tId;
    Label[] hkLabels = new Label[6];
    KeyBox[] hkBoxes = new KeyBox[6];
    FlatBtn bClose;
    bool loading = true;

    public SettingsForm(Overlay overlay)
    {
        ov = overlay;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(440, 690);
        AutoScroll = true;
        BackColor = BG;
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 9.5f);
        try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

        lLang = Lbl(20, 18, 200);
        cbLang = new ComboBox();
        cbLang.DropDownStyle = ComboBoxStyle.DropDownList;
        cbLang.Items.AddRange(new object[] { "English", "Deutsch" });
        cbLang.Location = new Point(270, 14);
        cbLang.Size = new Size(150, 26);
        cbLang.FlatStyle = FlatStyle.Flat;
        cbLang.BackColor = PANEL;
        cbLang.ForeColor = Color.White;
        Controls.Add(cbLang);

        lOpacity = Lbl(20, 60, 200);
        tbOpacity = new TrackBar();
        tbOpacity.Minimum = 40;
        tbOpacity.Maximum = 100;
        tbOpacity.TickFrequency = 10;
        tbOpacity.Location = new Point(220, 52);
        tbOpacity.Size = new Size(205, 40);
        tbOpacity.BackColor = BG;
        Controls.Add(tbOpacity);

        cPopup = Chk(20, 104);
        cVolume = Chk(20, 134);
        cAuto = Chk(20, 164);
        cUpdate = Chk(20, 194);
        cDiscord = Chk(20, 224);

        lId = Lbl(20, 260, 400);
        tId = new TextBox();
        tId.Location = new Point(20, 284);
        tId.Size = new Size(400, 26);
        tId.BackColor = PANEL;
        tId.ForeColor = Color.White;
        tId.BorderStyle = BorderStyle.FixedSingle;
        Controls.Add(tId);
        lStatus = Lbl(20, 314, 400);
        lStatus.ForeColor = Color.FromArgb(160, 160, 160);

        lKeys = Lbl(20, 356, 400);
        lKeys.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
        for (int i = 0; i < 6; i++)
        {
            hkLabels[i] = Lbl(20, 392 + i * 34, 230);
            hkBoxes[i] = new KeyBox();
            hkBoxes[i].Location = new Point(260, 389 + i * 34);
            hkBoxes[i].Size = new Size(160, 26);
            Controls.Add(hkBoxes[i]);
        }
        lHint = Lbl(20, 600, 400);
        lHint.ForeColor = Color.FromArgb(160, 160, 160);
        lTip = Lbl(20, 624, 400);
        lTip.Size = new Size(400, 36);
        lTip.ForeColor = Color.FromArgb(160, 160, 160);

        bClose = new FlatBtn("", Color.FromArgb(29, 185, 84), Color.Black);
        bClose.Location = new Point(290, 650);
        bClose.Click += delegate { Close(); };
        Controls.Add(bClose);

        // aktuelle Werte laden
        cbLang.SelectedIndex = Config.Language == "de" ? 1 : 0;
        tbOpacity.Value = Math.Max(40, Math.Min(100, Config.Opacity));
        cPopup.Checked = Config.Popup;
        cVolume.Checked = Config.Volume;
        cDiscord.Checked = Config.Discord;
        cUpdate.Checked = Config.AutoUpdate;
        cAuto.Checked = AutostartOn();
        tId.Text = Config.DiscordId ?? "";
        for (int i = 0; i < 6; i++) hkBoxes[i].Text = Config.Hk[i];
        Texts();
        loading = false;

        cbLang.SelectedIndexChanged += delegate
        {
            if (loading) return;
            Config.Language = cbLang.SelectedIndex == 1 ? "de" : "en";
            Lang.Cur = Config.Language;
            Texts();
            Apply();
        };
        tbOpacity.ValueChanged += delegate { if (!loading) Apply(); };
        cPopup.CheckedChanged += delegate { if (!loading) Apply(); };
        cVolume.CheckedChanged += delegate { if (!loading) Apply(); };
        cDiscord.CheckedChanged += delegate { if (!loading) Apply(); };
        cUpdate.CheckedChanged += delegate { if (!loading) Apply(); };
        cAuto.CheckedChanged += delegate { if (!loading) SetAutostart(cAuto.Checked); };
        tId.TextChanged += delegate { if (!loading) Apply(); };
        for (int i = 0; i < 6; i++) hkBoxes[i].TextChanged += delegate { if (!loading) Apply(); };

        System.Windows.Forms.Timer t = new System.Windows.Forms.Timer();
        t.Interval = 1000;
        t.Tick += delegate { lStatus.Text = Lang.T("dc_status", DiscordRpc.Status); };
        t.Start();
        Disposed += delegate { t.Stop(); t.Dispose(); };
    }

    Label Lbl(int x, int y, int w)
    {
        Label l = new Label();
        l.Location = new Point(x, y);
        l.Size = new Size(w, 24);
        l.BackColor = BG;
        l.ForeColor = Color.White;
        Controls.Add(l);
        return l;
    }

    CheckBox Chk(int x, int y)
    {
        CheckBox c = new CheckBox();
        c.Location = new Point(x, y);
        c.Size = new Size(400, 24);
        c.FlatStyle = FlatStyle.Flat;
        c.BackColor = BG;
        c.ForeColor = Color.White;
        c.Cursor = Cursors.Hand;
        Controls.Add(c);
        return c;
    }

    void Texts()
    {
        Text = Lang.T("set_title");
        lLang.Text = Lang.T("set_lang");
        lOpacity.Text = Lang.T("set_opacity");
        cPopup.Text = Lang.T("set_popup");
        cVolume.Text = Lang.T("set_volume");
        cAuto.Text = Lang.T("set_auto");
        cUpdate.Text = Lang.T("set_update");
        cDiscord.Text = Lang.T("set_discord");
        lId.Text = Lang.T("set_discordid");
        lStatus.Text = Lang.T("dc_status", DiscordRpc.Status);
        lKeys.Text = Lang.T("set_keys");
        for (int i = 0; i < 6; i++) hkLabels[i].Text = Lang.T(HkText[i]);
        lHint.Text = Lang.T("hk_hint");
        lTip.Text = Lang.T("set_tip");
        bClose.Text = Lang.T("set_close");
    }

    void Apply()
    {
        Config.Opacity = tbOpacity.Value;
        Config.Popup = cPopup.Checked;
        Config.Volume = cVolume.Checked;
        Config.Discord = cDiscord.Checked;
        Config.AutoUpdate = cUpdate.Checked;
        Config.DiscordId = tId.Text.Trim();
        for (int i = 0; i < 6; i++) Config.Hk[i] = hkBoxes[i].Text;
        Config.Save();
        ov.ApplySettings();
    }

    static bool AutostartOn()
    {
        try
        {
            using (RegistryKey k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))
                return k != null && k.GetValue("SpotifyOverlay") != null;
        }
        catch { return false; }
    }

    static void SetAutostart(bool on)
    {
        try
        {
            using (RegistryKey k = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))
            {
                if (on) k.SetValue("SpotifyOverlay", "\"" + Application.ExecutablePath + "\"");
                else k.DeleteValue("SpotifyOverlay", false);
            }
        }
        catch { }
    }
}
