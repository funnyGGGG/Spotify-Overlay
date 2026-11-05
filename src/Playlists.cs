// Spotify Overlay - funny.games
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

class PlaylistForm : Form
{
    static readonly Color BG = Color.FromArgb(24, 24, 24);
    static readonly Color PANEL = Color.FromArgb(40, 40, 40);

    ListBox list;
    Label info;
    FlatBtn bPlay, bRefresh, bConnect, bLogout;
    List<string> ids = new List<string>();

    public PlaylistForm()
    {
        Text = Lang.T("pl_title");
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(420, 500);
        BackColor = BG;
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10f);
        try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

        info = new Label();
        info.Location = new Point(16, 12);
        info.Size = new Size(388, 60);
        info.ForeColor = Color.FromArgb(180, 180, 180);
        info.BackColor = BG;
        Controls.Add(info);

        list = new ListBox();
        list.Location = new Point(16, 76);
        list.Size = new Size(388, 340);
        list.BackColor = PANEL;
        list.ForeColor = Color.White;
        list.BorderStyle = BorderStyle.None;
        list.Font = new Font("Segoe UI", 10.5f);
        list.ItemHeight = 22;
        list.DoubleClick += delegate { PlaySelected(); };
        Controls.Add(list);

        bConnect = new FlatBtn(Lang.T("pl_connect"), Color.FromArgb(29, 185, 84), Color.Black);
        bConnect.Location = new Point(16, 440);
        bConnect.Size = new Size(200, 36);
        bConnect.Click += delegate { Connect(); };
        Controls.Add(bConnect);

        bPlay = new FlatBtn(Lang.T("pl_play"), Color.FromArgb(29, 185, 84), Color.Black);
        bPlay.Location = new Point(16, 440);
        bPlay.Size = new Size(120, 36);
        bPlay.Click += delegate { PlaySelected(); };
        Controls.Add(bPlay);

        bRefresh = new FlatBtn(Lang.T("pl_refresh"), PANEL, Color.White);
        bRefresh.Location = new Point(146, 440);
        bRefresh.Size = new Size(120, 36);
        bRefresh.Click += delegate { Load(); };
        Controls.Add(bRefresh);

        bLogout = new FlatBtn(Lang.T("pl_logout"), PANEL, Color.White);
        bLogout.Location = new Point(276, 440);
        bLogout.Size = new Size(128, 36);
        bLogout.Click += delegate { SpotifyApi.Logout(); Setup(); };
        Controls.Add(bLogout);

        Setup();
    }

    void Say(string text)
    {
        try { BeginInvoke(new Action(delegate { info.Text = text; })); } catch { }
    }

    // zeigt je nach Stand die passenden Bedienelemente
    void Setup()
    {
        bool hasId = !string.IsNullOrEmpty((Config.SpotifyId ?? "").Trim());
        bool logged = hasId && SpotifyApi.LoggedIn;
        list.Visible = logged;
        bPlay.Visible = logged;
        bRefresh.Visible = logged;
        bLogout.Visible = logged;
        bConnect.Visible = hasId && !logged;
        list.Items.Clear();
        ids.Clear();
        if (!hasId) info.Text = Lang.T("pl_noid");
        else if (!logged) info.Text = "";
        else Load();
    }

    void Connect()
    {
        info.Text = Lang.T("pl_connecting");
        bConnect.Enabled = false;
        Thread t = new Thread(delegate()
        {
            try
            {
                SpotifyApi.Login(Config.SpotifyId.Trim());
                BeginInvoke(new Action(delegate { bConnect.Enabled = true; Setup(); }));
            }
            catch (Exception ex)
            {
                BeginInvoke(new Action(delegate { bConnect.Enabled = true; info.Text = Lang.T("pl_err", ex.Message); }));
            }
        });
        t.IsBackground = true;
        t.Start();
    }

    void Load()
    {
        info.Text = Lang.T("pl_loading");
        Thread t = new Thread(delegate()
        {
            try
            {
                List<KeyValuePair<string, string>> pls = SpotifyApi.Playlists();
                BeginInvoke(new Action(delegate
                {
                    list.Items.Clear();
                    ids.Clear();
                    foreach (KeyValuePair<string, string> p in pls) { list.Items.Add(p.Key); ids.Add(p.Value); }
                    info.Text = pls.Count == 0 ? Lang.T("pl_empty") : "";
                }));
            }
            catch (Exception ex) { Say(Lang.T("pl_err", ex.Message)); }
        });
        t.IsBackground = true;
        t.Start();
    }

    void PlaySelected()
    {
        int i = list.SelectedIndex;
        if (i < 0 || i >= ids.Count) return;
        string id = ids[i];
        Thread t = new Thread(delegate()
        {
            try { Say(SpotifyApi.Play(id) ? Lang.T("pl_started") : Lang.T("pl_opened")); }
            catch (Exception ex) { Say(Lang.T("pl_err", ex.Message)); }
        });
        t.IsBackground = true;
        t.Start();
    }
}
