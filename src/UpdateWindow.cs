// Spotify Overlay - funny.games
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.Windows.Forms;

// Fenster, das ein Update anzeigt, herunterlaedt und startet
class UpdateForm : Form
{
    static readonly Color BG = Color.FromArgb(24, 24, 24);

    Overlay ov;
    UpdateInfo info;
    Label lTitle, lVer, lStatus;
    TextBox tNotes;
    Bar bar;
    FlatBtn bNow, bLater;
    bool busy = false;
    bool rollback = false;

    public UpdateForm(Overlay overlay, UpdateInfo update, bool startNow)
    {
        ov = overlay;
        info = update;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(460, 330);
        BackColor = BG;
        ForeColor = Color.White;
        Text = "Spotify Overlay";
        TopMost = true;
        try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

        lTitle = new Label();
        lTitle.Text = Lang.T("upd_title");
        lTitle.Font = new Font("Segoe UI", 16f, FontStyle.Bold);
        lTitle.ForeColor = Color.FromArgb(29, 185, 84);
        lTitle.Location = new Point(20, 16);
        lTitle.Size = new Size(420, 34);
        Controls.Add(lTitle);

        lVer = new Label();
        lVer.Text = Lang.T("upd_versions", App.Version, info.Ver.ToString());
        lVer.Font = new Font("Segoe UI", 10.5f);
        lVer.ForeColor = Color.FromArgb(190, 190, 190);
        lVer.Location = new Point(22, 54);
        lVer.Size = new Size(420, 24);
        Controls.Add(lVer);

        tNotes = new TextBox();
        tNotes.Multiline = true;
        tNotes.ReadOnly = true;
        tNotes.ScrollBars = ScrollBars.Vertical;
        tNotes.Location = new Point(22, 88);
        tNotes.Size = new Size(416, 110);
        tNotes.BackColor = Color.FromArgb(34, 34, 34);
        tNotes.ForeColor = Color.White;
        tNotes.BorderStyle = BorderStyle.None;
        string notesText = string.IsNullOrEmpty(info.Notes) ? "" : Lang.T("upd_notes") + ":\r\n" + info.Notes.Replace("\n", "\r\n");
        if (Contact.HasDonate)
        {
            string dl = "";
            foreach (System.Collections.Generic.KeyValuePair<string, string> dn in Contact.DonateLinks()) dl += "\r\n" + dn.Key + ": " + dn.Value;
            notesText += (notesText.Length > 0 ? "\r\n\r\n" : "") + Lang.T("donate_update") + dl;
        }
        tNotes.Text = notesText;
        Controls.Add(tNotes);

        lStatus = new Label();
        lStatus.Location = new Point(22, 210);
        lStatus.Size = new Size(416, 22);
        lStatus.ForeColor = Color.FromArgb(190, 190, 190);
        Controls.Add(lStatus);

        bar = new Bar();
        bar.Location = new Point(22, 238);
        bar.Size = new Size(416, 8);
        Controls.Add(bar);

        bLater = new FlatBtn(Lang.T("upd_later"), Color.FromArgb(40, 40, 40), Color.White);
        bLater.Location = new Point(188, 274);
        bLater.Click += delegate { Close(); };
        Controls.Add(bLater);

        bNow = new FlatBtn(Lang.T("upd_now"), Color.FromArgb(29, 185, 84), Color.Black);
        bNow.Location = new Point(318, 274);
        bNow.Click += delegate { Start(); };
        Controls.Add(bNow);

        if (startNow) Shown += delegate { Start(); };
    }

    // Fuer "Version wechseln": gleiche Abfolge wie beim Update, aber mit einer aelteren oder anderen Version
    public void SetRollback()
    {
        rollback = true;
        lTitle.Text = Lang.T("roll_title");
        bNow.Text = Lang.T("roll_now");
        lStatus.Text = Lang.T("roll_note");
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (busy) e.Cancel = true;   // waehrend des Herunterladens nicht schliessen
        base.OnFormClosing(e);
    }

    void Start()
    {
        if (busy) return;
        busy = true;
        bNow.Enabled = false;
        bLater.Enabled = false;
        string file = Updater.TempFile();

        WebClient wc = new WebClient();
        wc.Headers[HttpRequestHeader.UserAgent] = "SpotifyOverlay";
        wc.DownloadProgressChanged += delegate(object s, DownloadProgressChangedEventArgs e)
        {
            bar.Value = e.ProgressPercentage;
            lStatus.Text = Lang.T("upd_down", e.ProgressPercentage);
        };
        wc.DownloadFileCompleted += delegate(object s, AsyncCompletedEventArgs e)
        {
            wc.Dispose();
            if (e.Error != null)
            {
                busy = false;
                lStatus.Text = Lang.T("pl_err", e.Error.Message);
                bLater.Enabled = true;
                bNow.Enabled = true;
                return;
            }
            bar.Value = 100;
            lStatus.Text = Lang.T("upd_inst");
            if (rollback)
            {
                // damit die gewaehlte Version bleibt, wird die automatische Update-Suche ausgeschaltet
                Config.AutoUpdate = false;
                Config.Save();
            }
            try { Process.Start(file, "/update"); } catch { }
            busy = false;
            ov.ExitAll();
        };
        ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
        wc.DownloadFileAsync(new Uri(info.Url), file);
    }
}
