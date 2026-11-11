// Spotify Overlay - funny.games
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
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
    static readonly string[] NavKeys = { "tab_general", "tab_controls", "tab_connections", "tab_streamer", "tab_profiles", "tab_updates", "tab_help" };
    static readonly string[] HkText = { "hk_volup", "hk_voldown", "hk_next", "hk_prev", "hk_play", "hk_toggle", "hk_click", "hk_lyrics" };
    const int HK = 8;

    Overlay ov;
    bool loading = true, dirty = false;
    const int TABS = 7;
    NavItem[] nav = new NavItem[TABS];
    Panel[] pages = new Panel[TABS];
    Label lSideVer, lSaved;
    FlatBtn bSave, bClose;
    System.Windows.Forms.Timer savedTimer;

    // Stand beim Oeffnen (zum Zuruecksetzen)
    string snapLang, snapSpId;
    int snapOpacity, snapScale;
    bool snapPopup, snapVolume, snapDiscord, snapCover, snapUpdate, snapFollow, snapStrFile, snapStrWeb, snapDodge, snapWaves;
    string snapStrFmt, snapProfiles;
    int snapVolStep;
    string[] snapHk = new string[HK];

    // Allgemein
    Label hGeneral, gLook, gBehavior, lLang, lOpacity, lOpVal, lOpHint, lSize, lSizeVal;
    ComboBox cbLang;
    Slider slOpacity, slSize;
    FlatBtn bResetPos;
    Toggle tPopup, tVolume, tAuto, tFollow, tDodge, tWaves;
    // Steuerung
    Label hControls, lHint, lVolStep, lVolStepVal;
    Slider slVolStep;
    Label[] hkLabels = new Label[HK];
    KeyBox[] hkBoxes = new KeyBox[HK];
    // Verbindungen
    Label hConn, gDiscord, lStatus, gSpotify, lSpId, lSpHint;
    Toggle tDiscord, tCover;
    TextBox tSpId;
    FlatBtn bCopy;
    // Streamer
    Label hStr, lStrInfo, gStrFile, lStrFmt, lStrPath, gStrWeb, lStrUrl, lStrHint;
    Toggle tStrFile, tStrWeb;
    TextBox tStrFmt;
    FlatBtn bStrFolder, bStrCopy;
    // Spiele-Profile
    Label hProf, lProfInfo, lProfApp, gProfEdit, lProfSize, lProfSizeVal, lProfOp, lProfOpVal, lProfPos;
    ListBox lbProf;
    ComboBox cbProfApp;
    FlatBtn bProfAdd, bProfDel, bProfPos, bProfPosClear;
    Slider slProfSize, slProfOp;
    Toggle tProfPopup, tProfClick, tProfVol;
    bool profLoading = false;
    // Updates
    Label hUpd, lVersion, lRepo, lResult;
    Toggle tUpdate;
    FlatBtn bCheck;
    // Hilfe und Kontakt
    Label hHelp, lCardTitle, lCardText, lGuide;
    Panel card;
    RichTextBox rtb;
    FlatBtn bDiscord, bGitHub, bIssue, bTour, bDonate;

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
        for (int i = 0; i < TABS; i++)
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
        BuildStreamer(pages[3]);
        BuildProfiles(pages[4]);
        BuildUpdates(pages[5]);
        BuildHelp(pages[6]);

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

        for (int i = 0; i < TABS; i++)
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
        bSave.Click += delegate { SaveAll(); Close(); };
        bar.Controls.Add(bSave);
    }

    void ShowTab(int i)
    {
        for (int k = 0; k < TABS; k++)
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

        gBehavior = Group(p, 306);
        tPopup = Tog(p, 336);
        tVolume = Tog(p, 370);
        tAuto = Tog(p, 404);
        tFollow = Tog(p, 438);
        tDodge = Tog(p, 472);
        tWaves = Tog(p, 506);
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
        lVolStep = Lbl(p, 36, 510, 300, 24, 10f, Color.White, FontStyle.Regular);
        slVolStep = new Slider();
        slVolStep.Minimum = 1;
        slVolStep.Maximum = 25;
        slVolStep.Location = new Point(340, 506);
        slVolStep.Size = new Size(200, 28);
        p.Controls.Add(slVolStep);
        lVolStepVal = Lbl(p, 548, 510, 50, 24, 10f, DIM, FontStyle.Regular);
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

    void BuildStreamer(Panel p)
    {
        hStr = Head(p);
        lStrInfo = Lbl(p, 36, 66, 560, 40, 9.5f, DIM, FontStyle.Regular);

        gStrFile = Group(p, 118);
        tStrFile = Tog(p, 148);
        lStrFmt = Lbl(p, 36, 186, 560, 22, 9.5f, DIM, FontStyle.Regular);
        tStrFmt = Box(p, 36, 210, 560);
        lStrPath = Lbl(p, 36, 246, 560, 22, 9f, DIM, FontStyle.Regular);
        bStrFolder = new FlatBtn("", FIELD, Color.White);
        bStrFolder.Location = new Point(36, 274);
        bStrFolder.Size = new Size(200, 34);
        bStrFolder.Click += delegate
        {
            try { Directory.CreateDirectory(Streamer.Folder); Process.Start(Streamer.Folder); } catch { }
        };
        p.Controls.Add(bStrFolder);

        gStrWeb = Group(p, 332);
        tStrWeb = Tog(p, 362);
        lStrUrl = Lbl(p, 36, 402, 560, 24, 10.5f, Color.White, FontStyle.Bold);
        bStrCopy = new FlatBtn("", FIELD, Color.White);
        bStrCopy.Location = new Point(36, 432);
        bStrCopy.Size = new Size(200, 34);
        bStrCopy.Click += delegate
        {
            try { Clipboard.SetText(Streamer.Url); } catch { }
            bStrCopy.Text = Lang.T("set_copied");
        };
        p.Controls.Add(bStrCopy);
        lStrHint = Lbl(p, 36, 476, 560, 60, 9f, DIM, FontStyle.Regular);
    }

    void BuildProfiles(Panel p)
    {
        hProf = Head(p);
        lProfInfo = Lbl(p, 36, 62, 560, 44, 9.5f, DIM, FontStyle.Regular);

        lbProf = new ListBox();
        lbProf.Location = new Point(36, 112);
        lbProf.Size = new Size(240, 132);
        lbProf.BackColor = FIELD;
        lbProf.ForeColor = Color.White;
        lbProf.BorderStyle = BorderStyle.None;
        lbProf.Font = new Font("Segoe UI", 10.5f);
        lbProf.SelectedIndexChanged += delegate { ProfShow(); };
        p.Controls.Add(lbProf);

        lProfApp = Lbl(p, 296, 108, 300, 48, 9f, DIM, FontStyle.Regular);
        cbProfApp = new ComboBox();
        cbProfApp.DropDownStyle = ComboBoxStyle.DropDown;
        cbProfApp.Location = new Point(296, 160);
        cbProfApp.Size = new Size(300, 28);
        cbProfApp.FlatStyle = FlatStyle.Flat;
        cbProfApp.BackColor = FIELD;
        cbProfApp.ForeColor = Color.White;
        cbProfApp.DropDown += delegate { ProfFillApps(); };
        p.Controls.Add(cbProfApp);

        bProfAdd = new FlatBtn("", GREEN, Color.Black);
        bProfAdd.Location = new Point(296, 200);
        bProfAdd.Size = new Size(144, 34);
        bProfAdd.Click += delegate { ProfAdd(); };
        p.Controls.Add(bProfAdd);

        bProfDel = new FlatBtn("", FIELD, Color.White);
        bProfDel.Location = new Point(452, 200);
        bProfDel.Size = new Size(144, 34);
        bProfDel.Click += delegate { ProfDelete(); };
        p.Controls.Add(bProfDel);

        gProfEdit = Group(p, 262);
        lProfSize = Lbl(p, 36, 296, 300, 24, 10f, Color.White, FontStyle.Regular);
        slProfSize = new Slider();
        slProfSize.Minimum = 60;
        slProfSize.Maximum = 250;
        slProfSize.Location = new Point(340, 292);
        slProfSize.Size = new Size(200, 28);
        slProfSize.ValueChanged += delegate { lProfSizeVal.Text = slProfSize.Value + " %"; ProfEdit(); };
        p.Controls.Add(slProfSize);
        lProfSizeVal = Lbl(p, 548, 296, 50, 24, 10f, DIM, FontStyle.Regular);

        lProfOp = Lbl(p, 36, 336, 300, 24, 10f, Color.White, FontStyle.Regular);
        slProfOp = new Slider();
        slProfOp.Minimum = 20;
        slProfOp.Maximum = 100;
        slProfOp.Location = new Point(340, 332);
        slProfOp.Size = new Size(200, 28);
        slProfOp.ValueChanged += delegate { lProfOpVal.Text = slProfOp.Value + " %"; ProfEdit(); };
        p.Controls.Add(slProfOp);
        lProfOpVal = Lbl(p, 548, 336, 50, 24, 10f, DIM, FontStyle.Regular);

        tProfPopup = Tog(p, 376);
        tProfClick = Tog(p, 410);
        tProfVol = Tog(p, 444);
        tProfPopup.CheckedChanged += delegate { ProfEdit(); };
        tProfClick.CheckedChanged += delegate { ProfEdit(); };
        tProfVol.CheckedChanged += delegate { ProfEdit(); };

        lProfPos = Lbl(p, 36, 482, 560, 22, 9.5f, DIM, FontStyle.Regular);
        bProfPos = new FlatBtn("", FIELD, Color.White);
        bProfPos.Location = new Point(36, 508);
        bProfPos.Size = new Size(300, 32);
        bProfPos.Click += delegate { ProfSetPos(true); };
        p.Controls.Add(bProfPos);
        bProfPosClear = new FlatBtn("", FIELD, Color.White);
        bProfPosClear.Location = new Point(346, 508);
        bProfPosClear.Size = new Size(120, 32);
        bProfPosClear.Click += delegate { ProfSetPos(false); };
        p.Controls.Add(bProfPosClear);

        ProfRefreshList();
        ProfShow();
    }

    void ProfRefreshList()
    {
        int keep = lbProf.SelectedIndex;
        lbProf.Items.Clear();
        foreach (GameProfile g in GameProfiles.Items) lbProf.Items.Add(g.Exe);
        if (keep >= 0 && keep < lbProf.Items.Count) lbProf.SelectedIndex = keep;
    }

    // Laufende Programme mit Fenster als Vorschlaege
    void ProfFillApps()
    {
        string cur = cbProfApp.Text;
        List<string> names = new List<string>();
        try
        {
            foreach (Process pr in Process.GetProcesses())
            {
                try
                {
                    if (pr.MainWindowTitle.Length > 0 && !names.Contains(pr.ProcessName)) names.Add(pr.ProcessName);
                }
                catch { }
                pr.Dispose();
            }
        }
        catch { }
        names.Sort(StringComparer.OrdinalIgnoreCase);
        string[] ex = new string[] { "RobloxPlayerBeta", "javaw:Minecraft" };
        foreach (string e in ex) if (!names.Contains(e)) names.Add(e);
        cbProfApp.Items.Clear();
        cbProfApp.Items.AddRange(names.ToArray());
        cbProfApp.Text = cur;
    }

    // Zeigt die Werte des gewaehlten Profils
    void ProfShow()
    {
        int i = lbProf.SelectedIndex;
        bool has = i >= 0 && i < GameProfiles.Items.Count;
        profLoading = true;
        slProfSize.Enabled = has;
        slProfOp.Enabled = has;
        tProfPopup.Enabled = has;
        tProfClick.Enabled = has;
        tProfVol.Enabled = has;
        bProfPos.Enabled = has;
        bProfPosClear.Enabled = has;
        if (has)
        {
            GameProfile g = GameProfiles.Items[i];
            slProfSize.Value = Math.Max(60, Math.Min(250, g.Scale));
            slProfOp.Value = Math.Max(20, Math.Min(100, g.Opacity));
            tProfPopup.Checked = g.Popup;
            tProfClick.Checked = g.Click;
            tProfVol.Checked = g.Volume;
            lProfPos.Text = g.HasPos ? Lang.T("prof_pos", g.X, g.Y) : Lang.T("prof_pos_none");
        }
        else
        {
            slProfSize.Value = 100;
            slProfOp.Value = 70;
            tProfPopup.Checked = false;
            tProfClick.Checked = false;
            tProfVol.Checked = true;
            lProfPos.Text = Lang.T("prof_pick");
        }
        lProfSizeVal.Text = slProfSize.Value + " %";
        lProfOpVal.Text = slProfOp.Value + " %";
        profLoading = false;
    }

    void ProfEdit()
    {
        if (loading || profLoading) return;
        int i = lbProf.SelectedIndex;
        if (i < 0 || i >= GameProfiles.Items.Count) return;
        GameProfile g = GameProfiles.Items[i];
        g.Scale = slProfSize.Value;
        g.Opacity = slProfOp.Value;
        g.Popup = tProfPopup.Checked;
        g.Click = tProfClick.Checked;
        g.Volume = tProfVol.Checked;
        GameProfiles.Store();
        dirty = true;
    }

    void ProfAdd()
    {
        string n = (cbProfApp.Text ?? "").Trim();
        if (n.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) n = n.Substring(0, n.Length - 4);
        if (n.Length == 0) return;
        int at = -1;
        for (int i = 0; i < GameProfiles.Items.Count; i++)
            if (string.Equals(GameProfiles.Items[i].Exe, n, StringComparison.OrdinalIgnoreCase)) at = i;
        if (at < 0)
        {
            GameProfile g = new GameProfile();
            g.Exe = n;
            g.Scale = Math.Max(60, Math.Min(250, ov.ScalePercent));
            g.Opacity = Config.Opacity;
            g.Popup = Config.Popup;
            g.Click = false;
            g.Volume = Config.Volume;
            GameProfiles.Items.Add(g);
            GameProfiles.Store();
            dirty = true;
            at = GameProfiles.Items.Count - 1;
        }
        ProfRefreshList();
        lbProf.SelectedIndex = at;
    }

    void ProfDelete()
    {
        int i = lbProf.SelectedIndex;
        if (i < 0 || i >= GameProfiles.Items.Count) return;
        GameProfiles.Items.RemoveAt(i);
        GameProfiles.Store();
        dirty = true;
        lbProf.SelectedIndex = -1;
        ProfRefreshList();
        ProfShow();
    }

    // Position des Overlays im Profil merken (oder loeschen)
    void ProfSetPos(bool set)
    {
        int i = lbProf.SelectedIndex;
        if (i < 0 || i >= GameProfiles.Items.Count) return;
        GameProfile g = GameProfiles.Items[i];
        g.X = set ? ov.Left : int.MinValue;
        g.Y = set ? ov.Top : int.MinValue;
        GameProfiles.Store();
        dirty = true;
        ProfShow();
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

        bDonate = new FlatBtn("", GREEN, Color.Black);
        bDonate.Location = new Point(438, 118);
        bDonate.Size = new Size(112, 32);
        bDonate.Click += delegate { Open(Contact.Donate); };
        card.Controls.Add(bDonate);

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
        tFollow.Checked = Config.Follow;
        tStrFile.Checked = Config.StreamFile;
        tStrWeb.Checked = Config.StreamWeb;
        tStrFmt.Text = Config.StreamFormat ?? "";
        tDodge.Checked = Config.Dodge;
        tWaves.Checked = Config.Waves;
        slVolStep.Value = Math.Max(1, Math.Min(25, Config.VolStep));
        lVolStepVal.Text = slVolStep.Value + " %";
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
        snapFollow = Config.Follow;
        snapStrFile = Config.StreamFile;
        snapStrWeb = Config.StreamWeb;
        snapStrFmt = Config.StreamFormat ?? "";
        snapDodge = Config.Dodge;
        snapWaves = Config.Waves;
        snapVolStep = Config.VolStep;
        snapProfiles = Config.Profiles ?? "";
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
        Config.Follow = snapFollow;
        Config.StreamFile = snapStrFile;
        Config.StreamWeb = snapStrWeb;
        Config.StreamFormat = snapStrFmt;
        Config.Dodge = snapDodge;
        Config.Waves = snapWaves;
        Config.VolStep = snapVolStep;
        Config.Profiles = snapProfiles;
        GameProfiles.Load();
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
        tFollow.CheckedChanged += delegate { if (!loading) Apply(); };
        tStrFile.CheckedChanged += delegate { if (!loading) Apply(); };
        tStrWeb.CheckedChanged += delegate { if (!loading) Apply(); };
        tStrFmt.TextChanged += delegate { if (!loading) Apply(); };
        tDodge.CheckedChanged += delegate { if (!loading) Apply(); };
        tWaves.CheckedChanged += delegate { if (!loading) Apply(); };
        slVolStep.ValueChanged += delegate { lVolStepVal.Text = slVolStep.Value + " %"; if (!loading) Apply(); };
        tSpId.TextChanged += delegate { if (!loading) Apply(); };
        for (int i = 0; i < HK; i++) hkBoxes[i].TextChanged += delegate { if (!loading) Apply(); };
    }

    void Texts()
    {
        Text = Lang.T("set_title");
        for (int i = 0; i < TABS; i++) nav[i].Text = Lang.T(NavKeys[i]);
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
        tFollow.Text = Lang.T("set_follow");
        tDodge.Text = Lang.T("set_dodge");
        tWaves.Text = Lang.T("set_waves");

        hControls.Text = Lang.T("tab_controls");
        lHint.Text = Lang.T("hk_hint");
        lVolStep.Text = Lang.T("set_volstep");
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

        hStr.Text = Lang.T("tab_streamer");
        lStrInfo.Text = Lang.T("str_info");
        gStrFile.Text = Lang.T("str_grp_file").ToUpper();
        tStrFile.Text = Lang.T("str_file");
        lStrFmt.Text = Lang.T("str_format");
        lStrPath.Text = Lang.T("str_path", Streamer.FilePath);
        bStrFolder.Text = Lang.T("str_folder");
        gStrWeb.Text = Lang.T("str_grp_web").ToUpper();
        tStrWeb.Text = Lang.T("str_web");
        lStrUrl.Text = Streamer.Url;
        bStrCopy.Text = Lang.T("set_copy");
        lStrHint.Text = Lang.T("str_hint");

        hProf.Text = Lang.T("tab_profiles");
        lProfInfo.Text = Lang.T("prof_info");
        lProfApp.Text = Lang.T("prof_app");
        bProfAdd.Text = Lang.T("prof_add");
        bProfDel.Text = Lang.T("prof_del");
        gProfEdit.Text = Lang.T("prof_grp").ToUpper();
        lProfSize.Text = Lang.T("set_size");
        lProfOp.Text = Lang.T("prof_op");
        tProfPopup.Text = Lang.T("set_popup");
        tProfClick.Text = Lang.T("prof_click");
        tProfVol.Text = Lang.T("set_volume");
        bProfPos.Text = Lang.T("prof_pos_now");
        bProfPosClear.Text = Lang.T("prof_pos_clear");
        ProfShow();

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
        bDonate.Text = Lang.T("set_donate");
        bDonate.Visible = Contact.HasDonate;
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
        if (Contact.HasDonate)
        {
            rtb.SelectionStart = rtb.TextLength;
            rtb.SelectionLength = 0;
            rtb.SelectionFont = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            rtb.SelectionColor = GREEN;
            rtb.SelectedText = "\n" + Lang.T("donate_head") + "\n";
            rtb.SelectionStart = rtb.TextLength;
            rtb.SelectionLength = 0;
            rtb.SelectionFont = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            rtb.SelectionColor = Color.FromArgb(215, 215, 215);
            rtb.SelectedText = Lang.T("donate_text") + "\n";
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
        Config.Follow = tFollow.Checked;
        Config.StreamFile = tStrFile.Checked;
        Config.StreamWeb = tStrWeb.Checked;
        Config.StreamFormat = tStrFmt.Text;
        Config.Dodge = tDodge.Checked;
        Config.Waves = tWaves.Checked;
        Config.VolStep = slVolStep.Value;
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
