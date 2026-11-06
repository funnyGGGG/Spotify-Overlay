// Spotify Overlay - funny.games
using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;

// Einstellungen: Seitenleiste mit fuenf Bereichen
class SettingsForm : Form
{
    static readonly Color BG = Color.FromArgb(24, 24, 24);
    static readonly Color SIDE = Color.FromArgb(18, 18, 18);
    static readonly Color FIELD = Color.FromArgb(38, 38, 38);
    static readonly Color DIM = Color.FromArgb(160, 160, 160);
    static readonly Color GREEN = Color.FromArgb(29, 185, 84);
    static readonly string[] NavKeys = { "tab_general", "tab_controls", "tab_connections", "tab_updates", "tab_help" };
    static readonly string[] HkText = { "hk_volup", "hk_voldown", "hk_next", "hk_prev", "hk_play", "hk_toggle" };

    Overlay ov;
    bool loading = true;
    NavItem[] nav = new NavItem[5];
    Panel[] pages = new Panel[5];
    Label lSideVer;

    // Allgemein
    Label hGeneral, lLang, lOpacity, lOpVal;
    ComboBox cbLang;
    Slider slOpacity;
    Toggle tPopup, tVolume, tAuto;
    // Steuerung
    Label hControls, lHint;
    Label[] hkLabels = new Label[6];
    KeyBox[] hkBoxes = new KeyBox[6];
    // Verbindungen
    Label hConn, gDiscord, lId, lStatus, gSpotify, lSpId, lSpHint;
    Toggle tDiscord;
    TextBox tId, tSpId;
    FlatBtn bCopy;
    // Updates
    Label hUpd, lVersion, lRepo, lResult;
    Toggle tUpdate;
    FlatBtn bCheck;
    // Hilfe und Kontakt
    Label hHelp, lContact, lNoDiscord;
    TextBox tHelp;
    FlatBtn bDiscord, bGitHub;

    public SettingsForm(Overlay overlay)
    {
        ov = overlay;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(800, 560);
        BackColor = BG;
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10f);
        try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

        BuildSidebar();
        for (int i = 0; i < 5; i++)
        {
            Panel p = new Panel();
            p.Location = new Point(190, 0);
            p.Size = new Size(610, 560);
            p.BackColor = BG;
            p.Visible = false;
            Controls.Add(p);
            pages[i] = p;
        }
        BuildGeneral(pages[0]);
        BuildControls(pages[1]);
        BuildConnections(pages[2]);
        BuildUpdates(pages[3]);
        BuildHelp(pages[4]);

        LoadValues();
        Texts();
        ShowTab(0);
        loading = false;
        WireEvents();

        System.Windows.Forms.Timer t = new System.Windows.Forms.Timer();
        t.Interval = 1000;
        t.Tick += delegate { lStatus.Text = Lang.T("dc_status", DiscordRpc.Status); };
        t.Start();
        Disposed += delegate { t.Stop(); t.Dispose(); };
    }

    // ---------- Bausteine ----------
    Label Lbl(Panel p, int x, int y, int w, int h, float size, Color c, FontStyle st)
    {
        Label l = new Label();
        l.Location = new Point(x, y);
        l.Size = new Size(w, h);
        l.BackColor = BG;
        l.UseMnemonic = false;   // "&" in "Hilfe & Kontakt" nicht als Tastenkuerzel behandeln
        l.ForeColor = c;
        l.Font = new Font("Segoe UI", size, st);
        p.Controls.Add(l);
        return l;
    }

    Label Head(Panel p)
    {
        return Lbl(p, 36, 26, 540, 36, 17f, Color.White, FontStyle.Bold);
    }

    Label Group(Panel p, int y)
    {
        return Lbl(p, 36, y, 540, 22, 9.5f, GREEN, FontStyle.Bold);
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
        t.Size = new Size(534, 26);
        p.Controls.Add(t);
        return t;
    }

    void BuildSidebar()
    {
        Panel side = new Panel();
        side.Location = new Point(0, 0);
        side.Size = new Size(190, 560);
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
        lSideVer.Location = new Point(20, 520);
        lSideVer.Size = new Size(160, 20);
        side.Controls.Add(lSideVer);
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
        lLang = Lbl(p, 36, 92, 280, 24, 10f, Color.White, FontStyle.Regular);
        cbLang = new ComboBox();
        cbLang.DropDownStyle = ComboBoxStyle.DropDownList;
        cbLang.Items.AddRange(new object[] { "English", "Deutsch" });
        cbLang.Location = new Point(340, 88);
        cbLang.Size = new Size(230, 28);
        cbLang.FlatStyle = FlatStyle.Flat;
        cbLang.BackColor = FIELD;
        cbLang.ForeColor = Color.White;
        p.Controls.Add(cbLang);

        lOpacity = Lbl(p, 36, 146, 280, 24, 10f, Color.White, FontStyle.Regular);
        slOpacity = new Slider();
        slOpacity.Minimum = 40;
        slOpacity.Maximum = 100;
        slOpacity.Location = new Point(320, 142);
        slOpacity.Size = new Size(200, 28);
        p.Controls.Add(slOpacity);
        lOpVal = Lbl(p, 526, 146, 50, 24, 10f, DIM, FontStyle.Regular);

        tPopup = Tog(p, 206);
        tVolume = Tog(p, 246);
        tAuto = Tog(p, 286);
    }

    void BuildControls(Panel p)
    {
        hControls = Head(p);
        lHint = Lbl(p, 36, 70, 534, 56, 9.5f, DIM, FontStyle.Regular);
        for (int i = 0; i < 6; i++)
        {
            hkLabels[i] = Lbl(p, 36, 144 + i * 46 + 4, 290, 24, 10f, Color.White, FontStyle.Regular);
            hkBoxes[i] = new KeyBox();
            hkBoxes[i].Location = new Point(340, 144 + i * 46);
            hkBoxes[i].Size = new Size(230, 28);
            p.Controls.Add(hkBoxes[i]);
        }
    }

    void BuildConnections(Panel p)
    {
        hConn = Head(p);
        gDiscord = Group(p, 80);
        tDiscord = Tog(p, 108);
        lStatus = Lbl(p, 36, 146, 534, 22, 9.5f, DIM, FontStyle.Regular);
        lId = Lbl(p, 36, 180, 534, 22, 9.5f, DIM, FontStyle.Regular);
        tId = Box(p, 36, 204, 534);

        gSpotify = Group(p, 262);
        lSpId = Lbl(p, 36, 292, 534, 22, 9.5f, DIM, FontStyle.Regular);
        tSpId = Box(p, 36, 316, 534);
        lSpHint = Lbl(p, 36, 352, 534, 44, 9f, DIM, FontStyle.Regular);
        bCopy = new FlatBtn("", FIELD, Color.White);
        bCopy.Location = new Point(36, 404);
        bCopy.Size = new Size(150, 32);
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
        lVersion = Lbl(p, 36, 84, 534, 24, 10.5f, Color.White, FontStyle.Regular);
        lRepo = Lbl(p, 36, 112, 534, 24, 9.5f, DIM, FontStyle.Regular);
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
        lResult = Lbl(p, 36, 270, 534, 60, 10f, Color.White, FontStyle.Regular);
    }

    void BuildHelp(Panel p)
    {
        hHelp = Head(p);
        tHelp = new TextBox();
        tHelp.Multiline = true;
        tHelp.ReadOnly = true;
        tHelp.ScrollBars = ScrollBars.Vertical;
        tHelp.Location = new Point(36, 74);
        tHelp.Size = new Size(538, 330);
        tHelp.BackColor = FIELD;
        tHelp.ForeColor = Color.White;
        tHelp.BorderStyle = BorderStyle.None;
        tHelp.Font = new Font("Segoe UI", 9.5f);
        p.Controls.Add(tHelp);

        lContact = Lbl(p, 36, 416, 534, 24, 10.5f, Color.White, FontStyle.Bold);
        bDiscord = new FlatBtn("", Color.FromArgb(88, 101, 242), Color.White);
        bDiscord.Location = new Point(36, 452);
        bDiscord.Size = new Size(200, 40);
        bDiscord.Click += delegate { Open(Contact.Discord); };
        p.Controls.Add(bDiscord);
        bGitHub = new FlatBtn("", FIELD, Color.White);
        bGitHub.Location = new Point(248, 452);
        bGitHub.Size = new Size(170, 40);
        bGitHub.Click += delegate { Open(Contact.GitHub); };
        p.Controls.Add(bGitHub);
        lNoDiscord = Lbl(p, 36, 458, 534, 24, 9.5f, DIM, FontStyle.Regular);
    }

    static void Open(string url)
    {
        try { if (!string.IsNullOrEmpty(url)) Process.Start(url); } catch { }
    }

    // ---------- Werte und Texte ----------
    void LoadValues()
    {
        cbLang.SelectedIndex = Config.Language == "de" ? 1 : 0;
        slOpacity.Value = Math.Max(40, Math.Min(100, Config.Opacity));
        lOpVal.Text = slOpacity.Value + " %";
        tPopup.Checked = Config.Popup;
        tVolume.Checked = Config.Volume;
        tAuto.Checked = AutostartOn();
        tDiscord.Checked = Config.Discord;
        tUpdate.Checked = Config.AutoUpdate;
        tId.Text = Config.DiscordId ?? "";
        tSpId.Text = Config.SpotifyId ?? "";
        for (int i = 0; i < 6; i++) hkBoxes[i].Text = Config.Hk[i];
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
        tPopup.CheckedChanged += delegate { if (!loading) Apply(); };
        tVolume.CheckedChanged += delegate { if (!loading) Apply(); };
        tAuto.CheckedChanged += delegate { if (!loading) SetAutostart(tAuto.Checked); };
        tDiscord.CheckedChanged += delegate { if (!loading) Apply(); };
        tUpdate.CheckedChanged += delegate { if (!loading) Apply(); };
        tId.TextChanged += delegate { if (!loading) Apply(); };
        tSpId.TextChanged += delegate { if (!loading) Apply(); };
        for (int i = 0; i < 6; i++) hkBoxes[i].TextChanged += delegate { if (!loading) Apply(); };
    }

    void Texts()
    {
        Text = Lang.T("set_title");
        for (int i = 0; i < 5; i++) nav[i].Text = Lang.T(NavKeys[i]);
        lSideVer.Text = Lang.T("lbl_version", App.Version);

        hGeneral.Text = Lang.T("tab_general");
        lLang.Text = Lang.T("set_lang");
        lOpacity.Text = Lang.T("set_opacity");
        tPopup.Text = Lang.T("set_popup");
        tVolume.Text = Lang.T("set_volume");
        tAuto.Text = Lang.T("set_auto");

        hControls.Text = Lang.T("tab_controls");
        lHint.Text = Lang.T("hk_hint");
        for (int i = 0; i < 6; i++) hkLabels[i].Text = Lang.T(HkText[i]);

        hConn.Text = Lang.T("tab_connections");
        gDiscord.Text = Lang.T("grp_discord").ToUpper();
        tDiscord.Text = Lang.T("set_discord");
        lId.Text = Lang.T("set_discordid");
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
        tHelp.Text = (Lang.T("help_text") + "\n\n" + Lang.T("set_tip")).Replace("\n", "\r\n");
        tHelp.SelectionStart = 0;
        tHelp.SelectionLength = 0;
        lContact.Text = Lang.T("set_contact");
        bDiscord.Text = Lang.T("set_discord_btn");
        bGitHub.Text = Lang.T("set_github_btn");
        bDiscord.Visible = Contact.HasDiscord;
        lNoDiscord.Visible = !Contact.HasDiscord;
        lNoDiscord.Text = Lang.T("set_nodiscord");
        bGitHub.Visible = Contact.GitHub.Length > 0;
        if (Contact.HasDiscord) bGitHub.Location = new Point(248, 452);
        else
        {
            lNoDiscord.Location = new Point(36, 446);
            bGitHub.Location = new Point(36, 476);
        }
    }

    void Apply()
    {
        Config.Opacity = slOpacity.Value;
        Config.Popup = tPopup.Checked;
        Config.Volume = tVolume.Checked;
        Config.Discord = tDiscord.Checked;
        Config.AutoUpdate = tUpdate.Checked;
        Config.DiscordId = tId.Text.Trim();
        Config.SpotifyId = tSpId.Text.Trim();
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
