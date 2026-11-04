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
        // Uninstall.exe loescht sich nach dem Beenden selbst
        try
        {
            string un = Path.Combine(dir, "Uninstall.exe");
            ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", "/c ping 127.0.0.1 -n 3 >nul & del /f /q \"" + un + "\" & rmdir \"" + dir + "\"");
            psi.CreateNoWindow = true;
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            psi.UseShellExecute = false;
            Process.Start(psi);
        }
        catch { }
        Say(step, "st_done", 100);
    }
}

class SetupForm : Form
{
    [DllImport("user32.dll")] static extern bool ReleaseCapture();
    [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr h, int msg, int w, int l);
    [DllImport("gdi32.dll")] static extern IntPtr CreateRoundRectRgn(int l, int t, int r, int b, int w, int h);

    static readonly Color BG = Color.FromArgb(18, 18, 18);
    static readonly Color PANEL = Color.FromArgb(34, 34, 34);
    static readonly Color GREEN = Color.FromArgb(29, 185, 84);
    static readonly Color GREY = Color.FromArgb(170, 170, 170);

    bool uninstall;
    Image logo;
    string dir;
    Panel pOpt, pProg, pDone;
    Label lLang, lIntro, lInfo, lFolder, lStatus, lDoneTitle, lDoneText, lUnQ, lUnText;
    ComboBox cbLang;
    TextBox txtDir;
    CheckBox cDesk, cAuto, cDiscord, cUpdate, cRun;
    FlatBtn bBrowse, bCancel, bInstall, bFinish, bUnNo, bUnYes;
    Bar bar;

    public SetupForm(bool uninstallMode)
    {
        uninstall = uninstallMode;
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(560, 470);
        BackColor = BG;
        Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, Width + 1, Height + 1, 18, 18));
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
        close.Location = new Point(528, 10);
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
        Show(pOpt);
    }

    Panel Page()
    {
        Panel p = new Panel();
        p.BackColor = BG;
        p.Location = new Point(0, 100);
        p.Size = new Size(560, 370);
        p.Visible = false;
        Controls.Add(p);
        return p;
    }

    void Show(Panel p)
    {
        pOpt.Visible = false;
        pProg.Visible = false;
        pDone.Visible = false;
        p.Visible = true;
    }

    Label Lbl(Panel p, int x, int y, int w, int h, float size, Color c, FontStyle st)
    {
        Label l = new Label();
        l.Location = new Point(x, y);
        l.Size = new Size(w, h);
        l.ForeColor = c;
        l.BackColor = BG;
        l.Font = new Font("Segoe UI", size, st);
        p.Controls.Add(l);
        return l;
    }

    CheckBox Chk(Panel p, int x, int y, bool on)
    {
        CheckBox c = new CheckBox();
        c.Location = new Point(x, y);
        c.Size = new Size(500, 24);
        c.Checked = on;
        c.ForeColor = Color.White;
        c.BackColor = BG;
        c.Font = new Font("Segoe UI", 10f);
        c.FlatStyle = FlatStyle.Flat;
        c.Cursor = Cursors.Hand;
        p.Controls.Add(c);
        return c;
    }

    void BuildOptions()
    {
        lLang = Lbl(pOpt, 28, 12, 120, 22, 10f, GREY, FontStyle.Regular);
        cbLang = new ComboBox();
        cbLang.DropDownStyle = ComboBoxStyle.DropDownList;
        cbLang.Items.AddRange(new object[] { "English", "Deutsch" });
        cbLang.SelectedIndex = Lang.Cur == "de" ? 1 : 0;
        cbLang.Location = new Point(150, 9);
        cbLang.Size = new Size(140, 26);
        cbLang.FlatStyle = FlatStyle.Flat;
        cbLang.BackColor = PANEL;
        cbLang.ForeColor = Color.White;
        cbLang.Font = new Font("Segoe UI", 10f);
        cbLang.SelectedIndexChanged += delegate
        {
            Lang.Cur = cbLang.SelectedIndex == 1 ? "de" : "en";
            ApplyLang();
        };
        pOpt.Controls.Add(cbLang);

        lIntro = Lbl(pOpt, 28, 50, 510, 24, 11f, Color.White, FontStyle.Bold);
        lInfo = Lbl(pOpt, 28, 76, 510, 56, 9.5f, GREY, FontStyle.Regular);
        lFolder = Lbl(pOpt, 28, 142, 300, 20, 9.5f, GREY, FontStyle.Regular);

        txtDir = new TextBox();
        txtDir.Text = dir;
        txtDir.Location = new Point(28, 166);
        txtDir.Size = new Size(384, 28);
        txtDir.BackColor = PANEL;
        txtDir.ForeColor = Color.White;
        txtDir.BorderStyle = BorderStyle.FixedSingle;
        txtDir.Font = new Font("Segoe UI", 10f);
        pOpt.Controls.Add(txtDir);

        bBrowse = new FlatBtn("", PANEL, Color.White);
        bBrowse.Location = new Point(420, 163);
        bBrowse.Size = new Size(110, 31);
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

        cDesk = Chk(pOpt, 28, 212, true);
        cAuto = Chk(pOpt, 28, 240, false);
        cDiscord = Chk(pOpt, 28, 268, true);
        cUpdate = Chk(pOpt, 28, 296, true);

        bCancel = new FlatBtn("", PANEL, Color.White);
        bCancel.Location = new Point(262, 326);
        bCancel.Click += delegate { Close(); };
        bInstall = new FlatBtn("", GREEN, Color.Black);
        bInstall.Location = new Point(402, 326);
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
        lUnQ = Lbl(pOpt, 28, 20, 510, 28, 12f, Color.White, FontStyle.Bold);
        lUnText = Lbl(pOpt, 28, 56, 510, 60, 10f, GREY, FontStyle.Regular);
        bUnNo = new FlatBtn("", PANEL, Color.White);
        bUnNo.Location = new Point(262, 326);
        bUnNo.Click += delegate { Close(); };
        bUnYes = new FlatBtn("", Color.FromArgb(200, 60, 50), Color.White);
        bUnYes.Location = new Point(402, 326);
        bUnYes.Click += delegate { Work(); };
        pOpt.Controls.Add(bUnNo);
        pOpt.Controls.Add(bUnYes);
    }

    void BuildProgress()
    {
        lStatus = Lbl(pProg, 28, 90, 500, 26, 11f, Color.White, FontStyle.Bold);
        bar = new Bar();
        bar.Location = new Point(28, 130);
        bar.Size = new Size(504, 8);
        pProg.Controls.Add(bar);
    }

    void BuildDone()
    {
        lDoneTitle = Lbl(pDone, 28, 20, 500, 34, 18f, GREEN, FontStyle.Bold);
        lDoneText = Lbl(pDone, 28, 72, 510, 130, 10f, GREY, FontStyle.Regular);
        cRun = Chk(pDone, 28, 226, true);
        if (uninstall) cRun.Visible = false;
        bFinish = new FlatBtn("", GREEN, Color.Black);
        bFinish.Location = new Point(402, 326);
        bFinish.Click += delegate
        {
            if (!uninstall && cRun.Checked)
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
        if (uninstall)
        {
            lUnQ.Text = Lang.T("uninst_q");
            lUnText.Text = Lang.T("uninst_text", dir);
            bUnNo.Text = Lang.T("btn_cancel");
            bUnYes.Text = Lang.T("btn_uninstall");
        }
        else
        {
            lLang.Text = Lang.T("lang_label");
            lIntro.Text = Lang.T("intro");
            lInfo.Text = Lang.T("intro2");
            lFolder.Text = Lang.T("folder");
            bBrowse.Text = Lang.T("browse");
            cDesk.Text = Lang.T("chk_desktop");
            cAuto.Text = Lang.T("chk_auto");
            cDiscord.Text = Lang.T("chk_discord");
            cUpdate.Text = Lang.T("chk_update");
            bCancel.Text = Lang.T("btn_cancel");
            bInstall.Text = Lang.T("btn_install");
        }
        cRun.Text = Lang.T("run_now");
        bFinish.Text = Lang.T("btn_finish");
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        g.Clear(BG);
        using (LinearGradientBrush b = new LinearGradientBrush(new Rectangle(0, 0, Width, 100), Color.FromArgb(14, 70, 38), BG, 90f))
            g.FillRectangle(b, 0, 0, Width, 100);
        using (LinearGradientBrush b = new LinearGradientBrush(new Rectangle(0, 0, Width, 4), GREEN, Color.FromArgb(232, 120, 40), 0f))
            g.FillRectangle(b, 0, 0, Width, 4);
        if (logo != null) g.DrawImage(logo, new Rectangle(28, 24, 58, 58));
        using (Font f1 = new Font("Segoe UI", 20f, FontStyle.Bold, GraphicsUnit.Pixel))
        using (Font f2 = new Font("Segoe UI", 13f, FontStyle.Regular, GraphicsUnit.Pixel))
        using (SolidBrush grey = new SolidBrush(GREY))
        {
            g.DrawString("Spotify Overlay", f1, Brushes.White, 100, 28);
            g.DrawString((uninstall ? Lang.T("sub_uninstall") : Lang.T("sub_install", App.Version)) + "  ·  funny.games", f2, grey, 101, 58);
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && e.Y < 100) { ReleaseCapture(); SendMessage(Handle, 0xA1, 2, 0); }
        base.OnMouseDown(e);
    }

    void Ui(Action a) { try { BeginInvoke(a); } catch { } }

    void Work()
    {
        Show(pProg);
        bool desk = !uninstall && cDesk.Checked, auto = !uninstall && cAuto.Checked;
        bool dc = !uninstall && cDiscord.Checked, upd = !uninstall && cUpdate.Checked;
        Action<string, int> step = delegate(string text, int pct)
        {
            Ui(delegate { lStatus.Text = text; bar.Value = pct; });
            Thread.Sleep(300);
        };

        Thread t = new Thread(delegate()
        {
            try
            {
                if (uninstall) Installer.Uninstall(dir, step);
                else Installer.Install(dir, desk, auto, dc, upd, step);
                Ui(delegate
                {
                    lDoneTitle.Text = uninstall ? Lang.T("done_un") : Lang.T("done_inst");
                    lDoneText.Text = uninstall ? Lang.T("done_un_text") : Lang.T("done_text", dir);
                    Show(pDone);
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

    [STAThread]
    static void Main(string[] args)
    {
        Config.Load();
        bool un = false, silent = false;
        foreach (string a in args)
        {
            if (a.ToLower() == "/uninstall") un = true;
            if (a.ToLower() == "/silent") silent = true;
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

        Application.EnableVisualStyles();
        Application.Run(new SetupForm(un));
    }
}
