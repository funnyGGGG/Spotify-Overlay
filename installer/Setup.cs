// Spotify Overlay - funny.games
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

// Die eigentliche Installation (ohne Oberflaeche, wird auch fuer /silent genutzt)
static class Installer
{
    const string UninstKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\SpotifyOverlay";
    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public static string StartMenuDir()
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "Spotify Overlay");
    }

    public static string DesktopLink()
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Spotify Overlay.lnk");
    }

    public static string FindInstallDir()
    {
        try
        {
            using (RegistryKey k = Registry.CurrentUser.OpenSubKey(Config.Key))
            {
                string d = k == null ? null : k.GetValue("InstallDir") as string;
                if (!string.IsNullOrEmpty(d)) return d;
            }
        }
        catch { }
        if (Directory.Exists(@"D:\")) return @"D:\SpotifyOverlay";
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SpotifyOverlay");
    }

    static void KillRunning()
    {
        foreach (Process p in Process.GetProcessesByName("SpotifyOverlay"))
        {
            try { p.Kill(); p.WaitForExit(3000); } catch { }
        }
    }

    static void MakeShortcut(string lnk, string target, string dir, string args)
    {
        try
        {
            Type t = Type.GetTypeFromProgID("WScript.Shell");
            dynamic sh = Activator.CreateInstance(t);
            dynamic sc = sh.CreateShortcut(lnk);
            sc.TargetPath = target;
            sc.WorkingDirectory = dir;
            sc.IconLocation = target;
            if (args != null) sc.Arguments = args;
            sc.Save();
        }
        catch { }
    }

    static void Say(Action<string, int> step, string key, int pct)
    {
        if (step != null) step(Lang.T(key), pct);
    }

    public static void Install(string dir, bool desktop, bool autostart, bool discord, bool updates, Action<string, int> step)
    {
        Say(step, "st_kill", 8);
        KillRunning();

        Say(step, "st_dir", 20);
        Directory.CreateDirectory(dir);

        Say(step, "st_copy", 40);
        string exe = Path.Combine(dir, "SpotifyOverlay.exe");
        using (Stream rs = Assembly.GetExecutingAssembly().GetManifestResourceStream("payload.exe"))
        using (FileStream fs = new FileStream(exe, FileMode.Create, FileAccess.Write))
            rs.CopyTo(fs);
        string un = Path.Combine(dir, "Uninstall.exe");
        if (!string.Equals(Application.ExecutablePath, un, StringComparison.OrdinalIgnoreCase))
            File.Copy(Application.ExecutablePath, un, true);

        Say(step, "st_links", 65);
        Directory.CreateDirectory(StartMenuDir());
        MakeShortcut(Path.Combine(StartMenuDir(), "Spotify Overlay.lnk"), exe, dir, null);
        MakeShortcut(Path.Combine(StartMenuDir(), "Uninstall.lnk"), un, dir, "/uninstall");
        if (desktop) MakeShortcut(DesktopLink(), exe, dir, null);

        Say(step, "st_reg", 85);
        Config.Load();
        Config.Language = Lang.Cur;
        Config.Discord = discord;
        Config.AutoUpdate = updates;
        Config.Save();
        using (RegistryKey k = Registry.CurrentUser.CreateSubKey(Config.Key))
            k.SetValue("InstallDir", dir);
        using (RegistryKey k = Registry.CurrentUser.CreateSubKey(UninstKey))
        {
            k.SetValue("DisplayName", "Spotify Overlay");
            k.SetValue("DisplayIcon", exe);
            k.SetValue("DisplayVersion", App.Version);
            k.SetValue("Publisher", "funny.games");
            k.SetValue("InstallLocation", dir);
            k.SetValue("UninstallString", "\"" + un + "\" /uninstall");
            k.SetValue("NoModify", 1);
            k.SetValue("NoRepair", 1);
        }
        using (RegistryKey run = Registry.CurrentUser.CreateSubKey(RunKey))
        {
            if (autostart) run.SetValue("SpotifyOverlay", "\"" + exe + "\"");
            else run.DeleteValue("SpotifyOverlay", false);
        }
        Say(step, "st_done", 100);
    }

    public static void Uninstall(string dir, Action<string, int> step)
    {
        Say(step, "st_un1", 15);
        KillRunning();
        Say(step, "st_un2", 40);
        try { File.Delete(Path.Combine(dir, "SpotifyOverlay.exe")); } catch { }
        Say(step, "st_un3", 65);
        try { Directory.Delete(StartMenuDir(), true); } catch { }
        try { File.Delete(DesktopLink()); } catch { }
        Say(step, "st_un4", 85);
        try
        {
            using (RegistryKey run = Registry.CurrentUser.OpenSubKey(RunKey, true))
                if (run != null) run.DeleteValue("SpotifyOverlay", false);
            Registry.CurrentUser.DeleteSubKeyTree(UninstKey, false);
            Registry.CurrentUser.DeleteSubKeyTree(Config.Key, false);
        }
        catch { }
        // Uninstall.exe wurde aus dem Temp-Ordner gestartet und ist nicht mehr im Installationsordner
        try { File.Delete(Path.Combine(dir, "Uninstall.exe")); } catch { }
        try { Directory.Delete(dir, false); } catch { }
        Say(step, "st_done", 100);
    }
}

// Installer-Fenster: Seitenleiste mit den Schritten links, Inhalt rechts
class SetupForm : Form
{
    [DllImport("user32.dll")] static extern bool ReleaseCapture();
    [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr h, int msg, int w, int l);
    [DllImport("gdi32.dll")] static extern IntPtr CreateRoundRectRgn(int l, int t, int r, int b, int w, int h);

    static readonly Color BG = Color.FromArgb(24, 24, 24);
    static readonly Color SIDE = Color.FromArgb(16, 16, 16);
    static readonly Color FIELD = Color.FromArgb(40, 40, 40);
    static readonly Color GREEN = Color.FromArgb(29, 185, 84);
    static readonly Color GREY = Color.FromArgb(160, 160, 160);
    const int SideW = 210;

    bool uninstall, updating;
    int step = 0;
    Image logo;
    string dir;
    Panel pOpt, pProg, pDone;
    Label lHead, lIntro, lInfo, lLang, lFolder, lStatus, lDoneHead, lDoneText, lUnText, lProgHead;
    ComboBox cbLang;
    TextBox txtDir;
    Toggle tDesk, tAuto, tDiscord, tUpdate, tRun;
    FlatBtn bBrowse, bCancel, bInstall, bFinish;
    Bar bar;

    public SetupForm(bool uninstallMode, bool updateMode)
    {
        uninstall = uninstallMode;
        updating = updateMode;
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(740, 470);
        BackColor = BG;
        Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, Width + 1, Height + 1, 14, 14));
        try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
        try
        {
            using (Stream rs = Assembly.GetExecutingAssembly().GetManifestResourceStream("cover.png"))
                if (rs != null) logo = new Bitmap(rs);
        }
        catch { }

        dir = Installer.FindInstallDir();

        Label close = new Label();
        close.Text = "\u2715";
        close.ForeColor = GREY;
        close.BackColor = BG;
        close.AutoSize = true;
        close.Font = new Font("Segoe UI", 11f);
        close.Cursor = Cursors.Hand;
        close.Location = new Point(708, 10);
        close.Click += delegate { Close(); };
        close.MouseEnter += delegate { close.ForeColor = Color.White; };
        close.MouseLeave += delegate { close.ForeColor = GREY; };
        Controls.Add(close);

        pOpt = Page();
        pProg = Page();
        pDone = Page();
        if (uninstall) BuildUninstall(); else BuildOptions();
        BuildProgress();
        BuildDone();
        ApplyLang();
        Show(pOpt, 0);

        if (updating)
        {
            // Update: keine Fragen, der Fortschritt wird sofort gezeigt
            Shown += delegate { Work(); };
        }
    }

    Panel Page()
    {
        Panel p = new Panel();
        p.BackColor = BG;
        p.Location = new Point(SideW + 32, 40);
        p.Size = new Size(740 - SideW - 64, 400);
        p.Visible = false;
        Controls.Add(p);
        return p;
    }

    void Show(Panel p, int idx)
    {
        pOpt.Visible = false;
        pProg.Visible = false;
        pDone.Visible = false;
        p.Visible = true;
        step = idx;
        Invalidate();
    }

    Label Lbl(Panel p, int x, int y, int w, int h, float size, Color c, FontStyle st)
    {
        Label l = new Label();
        l.Location = new Point(x, y);
        l.Size = new Size(w, h);
        l.ForeColor = c;
        l.BackColor = BG;
        l.UseMnemonic = false;
        l.Font = new Font("Segoe UI", size, st);
        p.Controls.Add(l);
        return l;
    }

    Toggle Tog(Panel p, int y, bool on)
    {
        Toggle t = new Toggle();
        t.Location = new Point(0, y);
        t.Size = new Size(468, 26);
        t.Checked = on;
        p.Controls.Add(t);
        return t;
    }

    void BuildOptions()
    {
        lHead = Lbl(pOpt, 0, 0, 468, 34, 18f, Color.White, FontStyle.Bold);
        lIntro = Lbl(pOpt, 0, 40, 468, 22, 10.5f, Color.White, FontStyle.Regular);
        lInfo = Lbl(pOpt, 0, 64, 468, 58, 9.5f, GREY, FontStyle.Regular);

        lLang = Lbl(pOpt, 0, 138, 120, 24, 10f, GREY, FontStyle.Regular);
        cbLang = new ComboBox();
        cbLang.DropDownStyle = ComboBoxStyle.DropDownList;
        cbLang.Items.AddRange(new object[] { "English", "Deutsch" });
        cbLang.SelectedIndex = Lang.Cur == "de" ? 1 : 0;
        cbLang.Location = new Point(120, 134);
        cbLang.Size = new Size(170, 28);
        cbLang.FlatStyle = FlatStyle.Flat;
        cbLang.BackColor = FIELD;
        cbLang.ForeColor = Color.White;
        cbLang.Font = new Font("Segoe UI", 10f);
        cbLang.SelectedIndexChanged += delegate
        {
            Lang.Cur = cbLang.SelectedIndex == 1 ? "de" : "en";
            ApplyLang();
        };
        pOpt.Controls.Add(cbLang);

        lFolder = Lbl(pOpt, 0, 180, 300, 22, 9.5f, GREY, FontStyle.Regular);
        txtDir = new TextBox();
        txtDir.Text = dir;
        txtDir.Location = new Point(0, 204);
        txtDir.Size = new Size(350, 28);
        txtDir.BackColor = FIELD;
        txtDir.ForeColor = Color.White;
        txtDir.BorderStyle = BorderStyle.FixedSingle;
        txtDir.Font = new Font("Segoe UI", 10f);
        pOpt.Controls.Add(txtDir);

        bBrowse = new FlatBtn("", FIELD, Color.White);
        bBrowse.Location = new Point(358, 201);
        bBrowse.Size = new Size(110, 32);
        bBrowse.Click += delegate
        {
            using (FolderBrowserDialog fb = new FolderBrowserDialog())
            {
                fb.Description = Lang.T("browse_desc");
                if (fb.ShowDialog() == DialogResult.OK)
                    txtDir.Text = Path.Combine(fb.SelectedPath, "SpotifyOverlay");
            }
        };
        pOpt.Controls.Add(bBrowse);

        tDesk = Tog(pOpt, 244, true);
        tAuto = Tog(pOpt, 274, false);
        tDiscord = Tog(pOpt, 304, true);
        tUpdate = Tog(pOpt, 334, true);

        bCancel = new FlatBtn("", FIELD, Color.White);
        bCancel.Location = new Point(204, 366);
        bCancel.Size = new Size(120, 38);
        bCancel.Click += delegate { Close(); };
        bInstall = new FlatBtn("", GREEN, Color.Black);
        bInstall.Location = new Point(334, 366);
        bInstall.Size = new Size(134, 38);
        bInstall.Click += delegate
        {
            string d = txtDir.Text.Trim();
            if (d.Length < 4) { MessageBox.Show(Lang.T("bad_dir")); return; }
            dir = d;
            Work();
        };
        pOpt.Controls.Add(bCancel);
        pOpt.Controls.Add(bInstall);
    }

    void BuildUninstall()
    {
        lHead = Lbl(pOpt, 0, 0, 468, 34, 18f, Color.White, FontStyle.Bold);
        lIntro = Lbl(pOpt, 0, 46, 468, 28, 11.5f, Color.White, FontStyle.Regular);
        lUnText = Lbl(pOpt, 0, 84, 468, 70, 10f, GREY, FontStyle.Regular);
        bCancel = new FlatBtn("", FIELD, Color.White);
        bCancel.Location = new Point(204, 366);
        bCancel.Size = new Size(120, 38);
        bCancel.Click += delegate { Close(); };
        bInstall = new FlatBtn("", Color.FromArgb(200, 60, 50), Color.White);
        bInstall.Location = new Point(334, 366);
        bInstall.Size = new Size(134, 38);
        bInstall.Click += delegate { Work(); };
        pOpt.Controls.Add(bCancel);
        pOpt.Controls.Add(bInstall);
    }

    void BuildProgress()
    {
        lProgHead = Lbl(pProg, 0, 0, 468, 34, 18f, Color.White, FontStyle.Bold);
        lStatus = Lbl(pProg, 0, 110, 468, 26, 11f, Color.White, FontStyle.Regular);
        bar = new Bar();
        bar.Location = new Point(0, 146);
        bar.Size = new Size(468, 6);
        pProg.Controls.Add(bar);
    }

    void BuildDone()
    {
        lDoneHead = Lbl(pDone, 0, 0, 468, 34, 18f, GREEN, FontStyle.Bold);
        lDoneText = Lbl(pDone, 0, 50, 468, 140, 10f, GREY, FontStyle.Regular);
        tRun = Tog(pDone, 206, true);
        if (uninstall) tRun.Visible = false;
        bFinish = new FlatBtn("", GREEN, Color.Black);
        bFinish.Location = new Point(334, 366);
        bFinish.Size = new Size(134, 38);
        bFinish.Click += delegate
        {
            if (!uninstall && tRun.Checked)
            {
                try { Process.Start(Path.Combine(dir, "SpotifyOverlay.exe")); } catch { }
            }
            Close();
        };
        pDone.Controls.Add(bFinish);
    }

    // Alle Texte in der gewaehlten Sprache setzen
    void ApplyLang()
    {
        lProgHead.Text = Lang.T("head_working");
        if (uninstall)
        {
            lHead.Text = Lang.T("head_uninstall");
            lIntro.Text = Lang.T("uninst_q");
            lUnText.Text = Lang.T("uninst_text", dir);
            bCancel.Text = Lang.T("btn_cancel");
            bInstall.Text = Lang.T("btn_uninstall");
        }
        else
        {
            lHead.Text = updating ? Lang.T("head_update") : Lang.T("head_install");
            lIntro.Text = Lang.T("intro");
            lInfo.Text = Lang.T("intro2");
            lLang.Text = Lang.T("lang_label");
            lFolder.Text = Lang.T("folder");
            bBrowse.Text = Lang.T("browse");
            tDesk.Text = Lang.T("chk_desktop");
            tAuto.Text = Lang.T("chk_auto");
            tDiscord.Text = Lang.T("chk_discord");
            tUpdate.Text = Lang.T("chk_update");
            bCancel.Text = Lang.T("btn_cancel");
            bInstall.Text = Lang.T("btn_install");
        }
        tRun.Text = Lang.T("run_now");
        bFinish.Text = Lang.T("btn_finish");
        Invalidate();
    }

    // Seitenleiste mit Logo und den drei Schritten
    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        g.Clear(BG);
        using (SolidBrush b = new SolidBrush(SIDE)) g.FillRectangle(b, 0, 0, SideW, Height);
        using (Pen p = new Pen(Color.FromArgb(34, 34, 34))) g.DrawLine(p, SideW, 0, SideW, Height);

        if (logo != null) g.DrawImage(logo, new Rectangle(28, 32, 52, 52));
        using (Font f1 = new Font("Segoe UI", 17f, FontStyle.Bold, GraphicsUnit.Pixel))
        using (Font f2 = new Font("Segoe UI", 12f, FontStyle.Regular, GraphicsUnit.Pixel))
        using (Font f3 = new Font("Segoe UI", 13f, FontStyle.Regular, GraphicsUnit.Pixel))
        using (Font f4 = new Font("Segoe UI", 12f, FontStyle.Bold, GraphicsUnit.Pixel))
        using (SolidBrush white = new SolidBrush(Color.White))
        using (SolidBrush grey = new SolidBrush(GREY))
        using (SolidBrush green = new SolidBrush(GREEN))
        using (SolidBrush dark = new SolidBrush(Color.FromArgb(90, 90, 90)))
        {
            g.DrawString("Spotify Overlay", f1, white, 28, 98);
            g.DrawString(Lang.T("lbl_version", App.Version), f2, grey, 28, 122);

            string[] names = { Lang.T("step_setup"), Lang.T("step_install"), Lang.T("step_done") };
            for (int i = 0; i < 3; i++)
            {
                int y = 190 + i * 44;
                bool past = i < step, now = i == step;
                if (past || now)
                    g.FillEllipse(green, 28, y, 22, 22);
                else
                    using (Pen p = new Pen(Color.FromArgb(70, 70, 70), 1.5f)) g.DrawEllipse(p, 28, y, 22, 22);

                if (past)
                    using (Pen p = new Pen(Color.Black, 2f))
                        g.DrawLines(p, new Point[] { new Point(34, y + 11), new Point(38, y + 15), new Point(45, y + 7) });
                else
                    g.DrawString((i + 1).ToString(), f4, now ? (Brush)new SolidBrush(Color.Black) : dark, new RectangleF(28, y + 3, 22, 18), new StringFormat { Alignment = StringAlignment.Center });

                g.DrawString(names[i], f3, (past || now) ? white : grey, 62, y + 2);
            }
            g.DrawString("funny.games", f2, dark, 28, Height - 40);
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && e.Y < 60) { ReleaseCapture(); SendMessage(Handle, 0xA1, 2, 0); }
        base.OnMouseDown(e);
    }

    void Ui(Action a) { try { BeginInvoke(a); } catch { } }

    void Work()
    {
        Show(pProg, 1);
        bool desk = !uninstall && tDesk.Checked, auto = !uninstall && tAuto.Checked;
        bool dc = !uninstall && tDiscord.Checked, upd = !uninstall && tUpdate.Checked;
        if (updating)
        {
            // bisherige Einstellungen des Nutzers beibehalten
            desk = File.Exists(Installer.DesktopLink());
            using (RegistryKey rk = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))
                auto = rk != null && rk.GetValue("SpotifyOverlay") != null;
            dc = Config.Discord;
            upd = Config.AutoUpdate;
            lProgHead.Text = Lang.T("head_update");
        }
        Action<string, int> report = delegate(string text, int pct)
        {
            Ui(delegate { lStatus.Text = text; bar.Value = pct; });
            Thread.Sleep(300);
        };

        Thread t = new Thread(delegate()
        {
            try
            {
                if (uninstall) Installer.Uninstall(dir, report);
                else Installer.Install(dir, desk, auto, dc, upd, report);

                if (updating)
                {
                    Ui(delegate
                    {
                        lStatus.Text = Lang.T("st_done");
                        step = 2;
                        Invalidate();
                        try { Process.Start(Path.Combine(dir, "SpotifyOverlay.exe")); } catch { }
                        System.Windows.Forms.Timer close = new System.Windows.Forms.Timer();
                        close.Interval = 1200;
                        close.Tick += delegate { Close(); };
                        close.Start();
                    });
                    return;
                }
                Ui(delegate
                {
                    lDoneHead.Text = uninstall ? Lang.T("done_un") : Lang.T("done_inst");
                    lDoneText.Text = uninstall ? Lang.T("done_un_text") : Lang.T("done_text", dir);
                    Show(pDone, 2);
                });
            }
            catch (Exception ex)
            {
                Ui(delegate
                {
                    MessageBox.Show(Lang.T("err", ex.Message), "Spotify Overlay");
                    Close();
                });
            }
        });
        t.IsBackground = true;
        t.Start();
    }

    static bool FromTemp(string[] args)
    {
        foreach (string a in args) if (a.ToLower() == "/fromtemp") return true;
        return false;
    }

    [STAThread]
    static void Main(string[] args)
    {
        Config.Load();
        bool un = false, silent = false, upd = false;
        foreach (string a in args)
        {
            if (a.ToLower() == "/uninstall") un = true;
            if (a.ToLower() == "/silent") silent = true;
            if (a.ToLower() == "/update") upd = true;
        }

        if (silent)
        {
            // Update im Hintergrund: Einstellungen des Nutzers bleiben erhalten
            try
            {
                string d = Installer.FindInstallDir();
                bool auto = false;
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))
                    auto = k != null && k.GetValue("SpotifyOverlay") != null;
                Thread.Sleep(1500);
                Installer.Install(d, File.Exists(Installer.DesktopLink()), auto, Config.Discord, Config.AutoUpdate, null);
                Process.Start(Path.Combine(d, "SpotifyOverlay.exe"));
            }
            catch { }
            return;
        }

        if (un && !FromTemp(args))
        {
            // Aus dem Installationsordner heraus kann sich das Programm nicht selbst loeschen:
            // darum zuerst eine Kopie im Temp-Ordner starten
            try
            {
                string tmp = Path.Combine(Path.GetTempPath(), "SpotifyOverlay_Uninstall.exe");
                File.Copy(Application.ExecutablePath, tmp, true);
                Process.Start(tmp, "/uninstall /fromtemp");
                return;
            }
            catch { }
        }
        Application.EnableVisualStyles();
        if (upd) Thread.Sleep(1500);   // alte Version beendet sich gerade
        Application.Run(new SetupForm(un, upd));
    }
}
