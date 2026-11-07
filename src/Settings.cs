// Spotify Overlay - funny.games
using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;

// Einstellungen: Seitenleiste mit fuenf Bereichen. Aenderungen wirken sofort zur Vorschau,
// gespeichert wird erst mit "Speichern".
class SettingsForm : Form
{
    static readonly Color BG = Color.FromArgb(24, 24, 24);
    static readonly Color SIDE = Color.FromArgb(18, 18, 18);
    static readonly Color FIELD = Color.FromArgb(38, 38, 38);
    static readonly Color CARD = Color.FromArgb(32, 32, 32);
    static readonly Color DIM = Color.FromArgb(160, 160, 160);
    static readonly Color GREEN = Color.FromArgb(29, 185, 84);
    static readonly string[] NavKeys = { "tab_general", "tab_controls", "tab_connections", "tab_updates", "tab_help" };
    static readonly string[] HkText = { "hk_volup", "hk_voldown", "hk_next", "hk_prev", "hk_play", "hk_toggle", "hk_click", "hk_lyrics" };
    const int HK = 8;

    Overlay ov;
    bool loading = true, dirty = false;
    NavItem[] nav = new NavItem[5];
    Panel[] pages = new Panel[5];
    Label lSideVer, lSaved;
    FlatBtn bSave, bClose;
    System.Windows.Forms.Timer savedTimer;

    // Stand beim Oeffnen (zum Zuruecksetzen)
    string snapLang, snapSpId;
    int snapOpacity, snapScale;
    bool snapPopup, snapVolume, snapDiscord, snapCover, snapUpdate;
    string[] snapHk = new string[HK];

    // Allgemein
    Label hGeneral, gLook, gBehavior, lLang, lOpacity, lOpVal, lOpHint, lSize, lSizeVal;
    ComboBox cbLang;
    Slider slOpacity, slSize;
    FlatBtn bResetPos;
    Toggle tPopup, tVolume, tAuto;
    // Steuerung
    Label hControls, lHint;
    Label[] hkLabels = new Label[HK];
    KeyBox[] hkBoxes = new KeyBox[HK];
    // Verbindungen
    Label hConn, gDiscord, lStatus, gSpotify, lSpId, lSpHint;
    Toggle tDiscord, tCover;
    TextBox tSpId;
    FlatBtn bCopy;
    // Updates
    Label hUpd, lVersion, lRepo, lResult;
    Toggle tUpdate;
    FlatBtn bCheck;
    // Hilfe und Kontakt
    Label hHelp, lCardTitle, lCardText, lGuide;
    Panel card;
    RichTextBox rtb;
    FlatBtn bDiscord, bGitHub, bIssue, bTour;

    public SettingsForm(Overlay overlay)
    {
        ov = overlay;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(820, 610);
        BackColor = BG;
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10f);
        try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

        BuildSidebar();
        for (int i = 0; i < 5; i++)
        {
            Panel p = new Panel();
            p.Location = new Point(190, 0);
            p.Size = new Size(630, 546);
            p.BackColor = BG;
            p.Visible = false;
            Controls.Add(p);
            pages[i] = p;
        }
        BuildBottomBar();
        BuildGeneral(pages[0]);
        BuildControls(pages[1]);
        BuildConnections(pages[2]);
        BuildUpdates(pages[3]);
        BuildHelp(pages[4]);

        LoadValues();
        TakeSnapshot();
        Texts();
        ShowTab(0);
        loading = false;
        WireEvents();

        savedTimer = new System.Windows.Forms.Timer();
        savedTimer.Interval = 2200;
        savedTimer.Tick += delegate { savedTimer.Stop(); lSaved.Text = ""; };

        System.Windows.Forms.Timer t = new System.Windows.Forms.Timer();
        t.Interval = 1000;
        t.Tick += delegate { lStatus.Text = Lang.T("dc_status", DiscordRpc.Status); };
        t.Start();
        Disposed += delegate { t.Stop(); t.Dispose(); savedTimer.Dispose(); };
    }

    // ---------- Bausteine ----------
    Label Lbl(Control p, int x, int y, int w, int h, float size, Color c, FontStyle st, Color back)
    {
        Label l = new Label();
        l.Location = new Point(x, y);
        l.Size = new Size(w, h);
        l.BackColor = back;
        l.UseMnemonic = false;   // "&" in "Hilfe & Kontakt" nicht als Tastenkuerzel behandeln
        l.ForeColor = c;
        l.Font = new Font("Segoe UI", size, st);
        p.Controls.Add(l);
        return l;
    }

    Label Lbl(Control p, int x, int y, int w, int h, float size, Color c, FontStyle st)
    {
        return Lbl(p, x, y, w, h, size, c, st, BG);
    }

    Label Head(Panel p)
    {
        return Lbl(p, 36, 26, 560, 36, 17f, Color.White, FontStyle.Bold);
    }

    Label Group(Panel p, int y)
    {
        return Lbl(p, 36, y, 560, 22, 9.5f, GREEN, FontStyle.Bold);
    }

    TextBox Box(Panel p, int x, int y, int w)
    {
        TextBox t = new TextBox();
        t.Location = new Point(x, y);
        t.Size = new Size(w, 28);
        t.BackColor = FIELD;
        t.ForeColor = Color.White;
        t.BorderStyle = BorderStyle.FixedSingle;
        p.Controls.Add(t);
        return t;
    }

    Toggle Tog(Panel p, int y)
    {
        Toggle t = new Toggle();
        t.Location = new Point(36, y);
        t.Size = new Size(560, 26);
        p.Controls.Add(t);
        return t;
    }

    void BuildSidebar()
    {
        Panel side = new Panel();
        side.Location = new Point(0, 0);
        side.Size = new Size(190, 610);
        side.BackColor = SIDE;
        Controls.Add(side);

        Label title = new Label();
        title.Text = "Spotify Overlay";
        title.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
        title.ForeColor = Color.White;
        title.BackColor = SIDE;
        title.Location = new Point(20, 24);
        title.Size = new Size(160, 26);
        side.Controls.Add(title);

        for (int i = 0; i < 5; i++)
        {
            NavItem n = new NavItem();
            n.Location = new Point(0, 80 + i * 42);
            n.Size = new Size(190, 40);
            int idx = i;
            n.Click += delegate { ShowTab(idx); };
            side.Controls.Add(n);
            nav[i] = n;
        }

        lSideVer = new Label();
        lSideVer.ForeColor = Color.FromArgb(120, 120, 120);
        lSideVer.BackColor = SIDE;
        lSideVer.Font = new Font("Segoe UI", 9f);
        lSideVer.Location = new Point(20, 575);
        lSideVer.Size = new Size(160, 20);
        side.Controls.Add(lSideVer);
    }

    // Untere Leiste mit Speichern
    void BuildBottomBar()
    {
        Panel bar = new Panel();
        bar.Location = new Point(190, 546);
        bar.Size = new Size(630, 64);
        bar.BackColor = Color.FromArgb(20, 20, 20);
        Controls.Add(bar);

        lSaved = Lbl(bar, 36, 20, 240, 24, 10f, GREEN, FontStyle.Bold, Color.FromArgb(20, 20, 20));

        bClose = new FlatBtn("", FIELD, Color.White);
        bClose.Location = new Point(366, 12);
        bClose.Size = new Size(110, 40);
        bClose.Click += delegate { Close(); };
        bar.Controls.Add(bClose);

        bSave = new FlatBtn("", GREEN, Color.Black);
        bSave.Location = new Point(486, 12);
        bSave.Size = new Size(110, 40);
        bSave.Click += delegate { SaveAll(); };
        bar.Controls.Add(bSave);
    }

    void ShowTab(int i)
    {
        for (int k = 0; k < 5; k++)
        {
            pages[k].Visible = (k == i);
            nav[k].Selected = (k == i);
        }
    }

    void BuildGeneral(Panel p)
    {
        hGeneral = Head(p);

        gLook = Group(p, 78);
        lLang = Lbl(p, 36, 116, 280, 24, 10f, Color.White, FontStyle.Regular);
        cbLang = new ComboBox();
        cbLang.DropDownStyle = ComboBoxStyle.DropDownList;
        cbLang.Items.AddRange(new object[] { "English", "Deutsch" });
        cbLang.Location = new Point(366, 112);
        cbLang.Size = new Size(230, 28);
        cbLang.FlatStyle = FlatStyle.Flat;
        cbLang.BackColor = FIELD;
        cbLang.ForeColor = Color.White;
        p.Controls.Add(cbLang);

        lOpacity = Lbl(p, 36, 162, 300, 24, 10f, Color.White, FontStyle.Regular);
        slOpacity = new Slider();
        slOpacity.Minimum = 20;
        slOpacity.Maximum = 100;
        slOpacity.Location = new Point(340, 158);
        slOpacity.Size = new Size(200, 28);
        p.Controls.Add(slOpacity);
        lOpVal = Lbl(p, 548, 162, 50, 24, 10f, DIM, FontStyle.Regular);

        lSize = Lbl(p, 36, 208, 300, 24, 10f, Color.White, FontStyle.Regular);
        slSize = new Slider();
        slSize.Minimum = 60;
        slSize.Maximum = 250;
        slSize.Location = new Point(340, 204);
        slSize.Size = new Size(200, 28);
        p.Controls.Add(slSize);
        lSizeVal = Lbl(p, 548, 208, 50, 24, 10f, DIM, FontStyle.Regular);

        bResetPos = new FlatBtn("", FIELD, Color.White);
        bResetPos.Location = new Point(36, 252);
        bResetPos.Size = new Size(210, 36);
        bResetPos.Click += delegate { ov.ResetPosition(); };
        p.Controls.Add(bResetPos);

        gBehavior = Group(p, 330);
        tPopup = Tog(p, 364);
        tVolume = Tog(p, 404);
        tAuto = Tog(p, 444);
    }

    void BuildControls(Panel p)
    {
        hControls = Head(p);
        lHint = Lbl(p, 36, 70, 560, 56, 9.5f, DIM, FontStyle.Regular);
        for (int i = 0; i < HK; i++)
        {
            hkLabels[i] = Lbl(p, 36, 138 + i * 46 + 4, 300, 24, 10f, Color.White, FontStyle.Regular);
            hkBoxes[i] = new KeyBox();
            hkBoxes[i].Location = new Point(366, 138 + i * 46);
            hkBoxes[i].Size = new Size(230, 28);
            p.Controls.Add(hkBoxes[i]);
        }
    }

    void BuildConnections(Panel p)
    {
        hConn = Head(p);
        gDiscord = Group(p, 80);
        tDiscord = Tog(p, 110);
        tCover = Tog(p, 148);
        lStatus = Lbl(p, 36, 190, 560, 22, 9.5f, DIM, FontStyle.Regular);

        gSpotify = Group(p, 252);
        lSpId = Lbl(p, 36, 284, 560, 22, 9.5f, DIM, FontStyle.Regular);
        tSpId = Box(p, 36, 308, 560);
        lSpHint = Lbl(p, 36, 346, 560, 44, 9f, DIM, FontStyle.Regular);
        bCopy = new FlatBtn("", FIELD, Color.White);
        bCopy.Location = new Point(36, 396);
        bCopy.Size = new Size(150, 34);
        bCopy.Click += delegate
        {
            try { Clipboard.SetText(SpotifyApi.Redirect); } catch { }
            bCopy.Text = Lang.T("set_copied");
        };
        p.Controls.Add(bCopy);
    }

    void BuildUpdates(Panel p)
    {
        hUpd = Head(p);
        lVersion = Lbl(p, 36, 84, 560, 24, 10.5f, Color.White, FontStyle.Regular);
        lRepo = Lbl(p, 36, 112, 560, 24, 9.5f, DIM, FontStyle.Regular);
        tUpdate = Tog(p, 164);
        bCheck = new FlatBtn("", GREEN, Color.Black);
        bCheck.Location = new Point(36, 216);
        bCheck.Size = new Size(220, 40);
        bCheck.Click += delegate
        {
            lResult.Text = Lang.T("set_checking");
            ov.CheckForUpdates(delegate(string msg) { lResult.Text = msg; });
        };
        p.Controls.Add(bCheck);
        lResult = Lbl(p, 36, 270, 560, 60, 10f, Color.White, FontStyle.Regular);
    }

    void BuildHelp(Panel p)
    {
        hHelp = Head(p);

        // Karte mit Discord und Links
        card = new Panel();
        card.Location = new Point(36, 72);
        card.Size = new Size(560, 158);
        card.BackColor = CARD;
        p.Controls.Add(card);

        Panel stripe = new Panel();
        stripe.Location = new Point(0, 0);
        stripe.Size = new Size(4, 158);
        stripe.BackColor = Color.FromArgb(88, 101, 242);
        card.Controls.Add(stripe);

        lCardTitle = Lbl(card, 22, 14, 520, 26, 13f, Color.White, FontStyle.Bold, CARD);
        lCardText = Lbl(card, 22, 42, 520, 22, 9.5f, DIM, FontStyle.Regular, CARD);

        bDiscord = new FlatBtn("", Color.FromArgb(88, 101, 242), Color.White);
        bDiscord.Location = new Point(22, 78);
        bDiscord.Size = new Size(200, 36);
        bDiscord.Click += delegate { Open(Contact.Discord); };
        card.Controls.Add(bDiscord);

        bGitHub = new FlatBtn("", FIELD, Color.White);
        bGitHub.Location = new Point(230, 78);
        bGitHub.Size = new Size(140, 36);
        bGitHub.Click += delegate { Open(Contact.GitHub); };
        card.Controls.Add(bGitHub);

        bIssue = new FlatBtn("", FIELD, Color.White);
        bIssue.Location = new Point(22, 118);
        bIssue.Size = new Size(200, 32);
        bIssue.Click += delegate { Open(Contact.GitHub + "/issues/new"); };
        card.Controls.Add(bIssue);

        bTour = new FlatBtn("", FIELD, Color.White);
        bTour.Location = new Point(230, 118);
        bTour.Size = new Size(200, 32);
        bTour.Click += delegate { ov.ShowWelcome(); };
        card.Controls.Add(bTour);

        lGuide = Group(p, 250);
        rtb = new RichTextBox();
        rtb.Location = new Point(36, 280);
        rtb.Size = new Size(560, 250);
        rtb.ReadOnly = true;
        rtb.BorderStyle = BorderStyle.None;
        rtb.BackColor = FIELD;
        rtb.ForeColor = Color.White;
        rtb.ScrollBars = RichTextBoxScrollBars.Vertical;
        p.Controls.Add(rtb);
    }

    static void Open(string url)
    {
        try { if (!string.IsNullOrEmpty(url)) Process.Start(url); } catch { }
    }

    // ---------- Werte, Speichern, Zuruecksetzen ----------
    void LoadValues()
    {
        cbLang.SelectedIndex = Config.Language == "de" ? 1 : 0;
        slOpacity.Value = Math.Max(20, Math.Min(100, Config.Opacity));
        lOpVal.Text = slOpacity.Value + " %";
        slSize.Value = Math.Max(60, Math.Min(250, ov.ScalePercent));
        lSizeVal.Text = slSize.Value + " %";
        tPopup.Checked = Config.Popup;
        tVolume.Checked = Config.Volume;
        tAuto.Checked = AutostartOn();
        tDiscord.Checked = Config.Discord;
        tCover.Checked = Config.DiscordCover;
        tUpdate.Checked = Config.AutoUpdate;
        tSpId.Text = Config.SpotifyId ?? "";
        for (int i = 0; i < HK; i++) hkBoxes[i].Text = Config.Hk[i];
    }

    void TakeSnapshot()
    {
        snapLang = Config.Language;
        snapOpacity = Config.Opacity;
        snapScale = ov.ScalePercent;
        snapPopup = Config.Popup;
        snapVolume = Config.Volume;
        snapDiscord = Config.Discord;
        snapCover = Config.DiscordCover;
        snapUpdate = Config.AutoUpdate;
        snapSpId = Config.SpotifyId ?? "";
        for (int i = 0; i < HK; i++) snapHk[i] = Config.Hk[i];
    }

    void Revert()
    {
        Config.Language = snapLang;
        Lang.Cur = snapLang;
        Config.Opacity = snapOpacity;
        Config.Popup = snapPopup;
        Config.Volume = snapVolume;
        Config.Discord = snapDiscord;
        Config.DiscordCover = snapCover;
        Config.AutoUpdate = snapUpdate;
        Config.SpotifyId = snapSpId;
        for (int i = 0; i < HK; i++) Config.Hk[i] = snapHk[i];
        ov.PreviewScale(snapScale);
        ov.ApplySettings();
        dirty = false;
    }

    void SaveAll()
    {
        Config.Save();
        SetAutostart(tAuto.Checked);
        ov.SavePosition();     // speichert auch die Groesse
        TakeSnapshot();
        dirty = false;
        lSaved.Text = "\u2713 " + Lang.T("set_saved");
        savedTimer.Stop();
        savedTimer.Start();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (dirty)
        {
            DialogResult r = MessageBox.Show(Lang.T("set_unsaved"), Lang.T("set_title"), MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (r == DialogResult.Cancel) { e.Cancel = true; return; }
            if (r == DialogResult.Yes) SaveAll();
            else Revert();
        }
        base.OnFormClosing(e);
    }

    void WireEvents()
    {
        cbLang.SelectedIndexChanged += delegate
        {
            if (loading) return;
            Config.Language = cbLang.SelectedIndex == 1 ? "de" : "en";
            Lang.Cur = Config.Language;
            Texts();
            Apply();
        };
        slOpacity.ValueChanged += delegate { lOpVal.Text = slOpacity.Value + " %"; if (!loading) Apply(); };
        slSize.ValueChanged += delegate { lSizeVal.Text = slSize.Value + " %"; if (!loading) { ov.PreviewScale(slSize.Value); dirty = true; } };
        tPopup.CheckedChanged += delegate { if (!loading) Apply(); };
        tVolume.CheckedChanged += delegate { if (!loading) Apply(); };
        tAuto.CheckedChanged += delegate { if (!loading) dirty = true; };
        tDiscord.CheckedChanged += delegate { if (!loading) Apply(); };
        tCover.CheckedChanged += delegate { if (!loading) Apply(); };
        tUpdate.CheckedChanged += delegate { if (!loading) Apply(); };
        tSpId.TextChanged += delegate { if (!loading) Apply(); };
        for (int i = 0; i < HK; i++) hkBoxes[i].TextChanged += delegate { if (!loading) Apply(); };
    }

    void Texts()
    {
        Text = Lang.T("set_title");
        for (int i = 0; i < 5; i++) nav[i].Text = Lang.T(NavKeys[i]);
        lSideVer.Text = Lang.T("lbl_version", App.Version);
        bSave.Text = Lang.T("set_save");
        bClose.Text = Lang.T("set_close");

        hGeneral.Text = Lang.T("tab_general");
        gLook.Text = Lang.T("grp_look").ToUpper();
        lLang.Text = Lang.T("set_lang");
        lOpacity.Text = Lang.T("set_opacity");
        lSize.Text = Lang.T("set_size");
        bResetPos.Text = Lang.T("set_resetpos");
        gBehavior.Text = Lang.T("grp_behavior").ToUpper();
        tPopup.Text = Lang.T("set_popup");
        tVolume.Text = Lang.T("set_volume");
        tAuto.Text = Lang.T("set_auto");

        hControls.Text = Lang.T("tab_controls");
        lHint.Text = Lang.T("hk_hint");
        for (int i = 0; i < HK; i++) hkLabels[i].Text = Lang.T(HkText[i]);

        hConn.Text = Lang.T("tab_connections");
        gDiscord.Text = Lang.T("grp_discord").ToUpper();
        tDiscord.Text = Lang.T("set_discord");
        tCover.Text = Lang.T("set_dccover");
        lStatus.Text = Lang.T("dc_status", DiscordRpc.Status);
        gSpotify.Text = Lang.T("grp_spotify").ToUpper();
        lSpId.Text = Lang.T("set_spid");
        lSpHint.Text = Lang.T("set_spid_hint");
        bCopy.Text = Lang.T("set_copy");

        hUpd.Text = Lang.T("tab_updates");
        lVersion.Text = Lang.T("set_version", App.Version);
        lRepo.Text = Updater.Configured ? Lang.T("set_repo", Updater.Repo) : Lang.T("set_norepo");
        tUpdate.Text = Lang.T("set_update");
        bCheck.Text = Lang.T("set_check");

        hHelp.Text = Lang.T("tab_help");
        lCardTitle.Text = Lang.T("help_card_title");
        lCardText.Text = Contact.HasDiscord ? Lang.T("help_card_text") : Lang.T("set_nodiscord");
        bDiscord.Text = Lang.T("set_discord_btn");
        bDiscord.Visible = Contact.HasDiscord;
        bGitHub.Text = Lang.T("set_github_btn");
        bGitHub.Visible = Contact.GitHub.Length > 0;
        bGitHub.Location = new Point(Contact.HasDiscord ? 230 : 22, 78);
        bIssue.Text = Lang.T("set_issue");
        bIssue.Visible = Contact.GitHub.Length > 0;
        bTour.Text = Lang.T("set_tour");
        lGuide.Text = Lang.T("set_guide").ToUpper();
        FillGuide();
    }

    // Anleitung: Ueberschriften ("## ...") gruen und fett, Text normal
    void FillGuide()
    {
        rtb.Clear();
        foreach (string raw in Lang.T("help_text").Split('\n'))
        {
            string line = raw.TrimEnd('\r');
            rtb.SelectionStart = rtb.TextLength;
            rtb.SelectionLength = 0;
            if (line.StartsWith("## "))
            {
                rtb.SelectionFont = new Font("Segoe UI", 10.5f, FontStyle.Bold);
                rtb.SelectionColor = GREEN;
                rtb.SelectedText = line.Substring(3) + "\n";
            }
            else
            {
                rtb.SelectionFont = new Font("Segoe UI", 9.5f, FontStyle.Regular);
                rtb.SelectionColor = Color.FromArgb(215, 215, 215);
                rtb.SelectedText = line + "\n";
            }
        }
        rtb.Select(0, 0);
    }

    // Werte uebernehmen (Vorschau), gespeichert wird erst mit "Speichern"
    void Apply()
    {
        Config.Opacity = slOpacity.Value;
        Config.Popup = tPopup.Checked;
        Config.Volume = tVolume.Checked;
        Config.Discord = tDiscord.Checked;
        Config.DiscordCover = tCover.Checked;
        Config.AutoUpdate = tUpdate.Checked;
        Config.SpotifyId = tSpId.Text.Trim();
        for (int i = 0; i < HK; i++) Config.Hk[i] = hkBoxes[i].Text;
        dirty = true;
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
