// Spotify Overlay - funny.games
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

class PlaylistForm : Form
{
    static readonly Color BG = Color.FromArgb(24, 24, 24);
    static readonly Color PANEL = Color.FromArgb(40, 40, 40);

    ListBox list;
    Label info;
    FlatBtn bPlay, bRefresh, bConnect, bLogout, bAdd, bRemove;
    List<string> ids = new List<string>();

    public PlaylistForm()
    {
        Text = Lang.T("pl_title");
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(420, 546);
        BackColor = BG;
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10f);
        try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

        info = new Label();
        info.Location = new Point(16, 12);
        info.Size = new Size(388, 90);
        info.ForeColor = Color.FromArgb(180, 180, 180);
        info.BackColor = BG;
        Controls.Add(info);

        list = new ListBox();
        list.Location = new Point(16, 108);
        list.Size = new Size(388, 310);
        list.BackColor = PANEL;
        list.ForeColor = Color.White;
        list.BorderStyle = BorderStyle.None;
        list.Font = new Font("Segoe UI", 10.5f);
        list.ItemHeight = 22;
        list.DoubleClick += delegate { PlaySelected(); };
        Controls.Add(list);

        bConnect = new FlatBtn(Lang.T("pl_connect"), Color.FromArgb(29, 185, 84), Color.Black);
        bConnect.Location = new Point(16, 490);
        bConnect.Size = new Size(388, 38);
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

        bAdd = new FlatBtn(Lang.T("pl_add"), PANEL, Color.White);
        bAdd.Location = new Point(146, 440);
        bAdd.Size = new Size(160, 36);
        bAdd.Click += delegate { AddFromClipboard(); };
        Controls.Add(bAdd);

        bRemove = new FlatBtn(Lang.T("pl_remove"), PANEL, Color.White);
        bRemove.Location = new Point(316, 440);
        bRemove.Size = new Size(88, 36);
        bRemove.Click += delegate { RemoveSelected(); };
        Controls.Add(bRemove);

        Setup();
    }

    void Say(string text)
    {
        try { BeginInvoke(new Action(delegate { info.Text = text; })); } catch { }
    }

    static bool LoggedIn()
    {
        bool hasId = !string.IsNullOrEmpty((Config.SpotifyId ?? "").Trim());
        return hasId && SpotifyApi.LoggedIn;
    }

    // zeigt je nach Stand die passenden Bedienelemente.
    // Angemeldet: die Playlists des Spotify-Kontos. Sonst (auch ohne Premium und ohne Client-ID):
    // die eigene Liste mit Playlist-Links, die in Spotify geoeffnet werden.
    void Setup()
    {
        bool hasId = !string.IsNullOrEmpty((Config.SpotifyId ?? "").Trim());
        bool logged = LoggedIn();
        list.Visible = true;
        bPlay.Visible = true;
        bRefresh.Visible = logged;
        bLogout.Visible = logged;
        bAdd.Visible = !logged;
        bRemove.Visible = !logged;
        bConnect.Visible = hasId && !logged;
        list.Items.Clear();
        ids.Clear();
        if (logged) { Load(); return; }
        foreach (KeyValuePair<string, string> p in MyLists.Get()) { list.Items.Add(p.Value); ids.Add(p.Key); }
        info.Text = Lang.T("pl_local") + (hasId ? "" : "\n\n" + Lang.T("pl_noid"));
    }

    // Link aus der Zwischenablage hinzufuegen (in Spotify: Playlist -> Teilen -> Link kopieren)
    void AddFromClipboard()
    {
        string txt = "";
        try { txt = Clipboard.GetText(); } catch { }
        string id = MyLists.ParseId(txt);
        if (id == null) { info.Text = Lang.T("pl_badlink"); return; }
        foreach (string x in ids)
            if (x == id) { info.Text = Lang.T("pl_have"); return; }
        info.Text = Lang.T("pl_adding");
        Thread t = new Thread(delegate()
        {
            string name = MyLists.FetchName(id);
            try
            {
                BeginInvoke(new Action(delegate
                {
                    List<KeyValuePair<string, string>> l = MyLists.Get();
                    l.Add(new KeyValuePair<string, string>(id, name));
                    MyLists.Save(l);
                    Setup();
                }));
            }
            catch { }
        });
        t.IsBackground = true;
        t.Start();
    }

    void RemoveSelected()
    {
        int i = list.SelectedIndex;
        if (i < 0 || i >= ids.Count) return;
        string id = ids[i];
        List<KeyValuePair<string, string>> l = MyLists.Get();
        l.RemoveAll(delegate(KeyValuePair<string, string> p) { return p.Key == id; });
        MyLists.Save(l);
        Setup();
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
        if (!LoggedIn())
        {
            try { Process.Start("spotify:playlist:" + id); Say(Lang.T("pl_opened")); }
            catch (Exception ex) { Say(Lang.T("pl_err", ex.Message)); }
            return;
        }
        Thread t = new Thread(delegate()
        {
            try
            {
                int r = SpotifyApi.Play(id);
                Say(r == 0 ? Lang.T("pl_started") : (r == 1 ? Lang.T("pl_free") : Lang.T("pl_opened")));
            }
            catch (Exception ex) { Say(Lang.T("pl_err", ex.Message)); }
        });
        t.IsBackground = true;
        t.Start();
    }
}

// Eigene Playlist-Links, gespeichert in der Registry als "ID<Tab>Name" pro Zeile
static class MyLists
{
    public static List<KeyValuePair<string, string>> Get()
    {
        List<KeyValuePair<string, string>> l = new List<KeyValuePair<string, string>>();
        foreach (string raw in (Config.MyPlaylists ?? "").Split('\n'))
        {
            string line = raw.TrimEnd('\r');
            int tab = line.IndexOf('\t');
            if (tab <= 0) continue;
            l.Add(new KeyValuePair<string, string>(line.Substring(0, tab), line.Substring(tab + 1)));
        }
        return l;
    }

    // Speichert nur diese Liste (nicht die uebrigen, evtl. noch nicht gespeicherten Einstellungen)
    public static void Save(List<KeyValuePair<string, string>> l)
    {
        StringBuilder sb = new StringBuilder();
        foreach (KeyValuePair<string, string> p in l)
            sb.Append(p.Key).Append('\t').Append(p.Value).Append('\n');
        Config.MyPlaylists = sb.ToString();
        try
        {
            using (RegistryKey k = Registry.CurrentUser.CreateSubKey(Config.Key))
                k.SetValue("MyPlaylists", Config.MyPlaylists);
        }
        catch { }
    }

    // Erkennt https://open.spotify.com/playlist/ID?si=... und spotify:playlist:ID
    public static string ParseId(string s)
    {
        if (string.IsNullOrEmpty(s)) return null;
        s = s.Trim();
        int i = s.IndexOf("playlist/");
        if (i < 0) i = s.IndexOf("playlist:");
        if (i < 0) return null;
        int st = i + 9;
        int e = st;
        while (e < s.Length && s[e] < 128 && char.IsLetterOrDigit(s[e])) e++;
        if (e - st < 10) return null;
        return s.Substring(st, e - st);
    }

    // Name der Playlist ueber die oeffentliche oEmbed-Adresse von Spotify (ohne Anmeldung)
    public static string FetchName(string id)
    {
        try
        {
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
            using (WebClient wc = new WebClient())
            {
                wc.Encoding = Encoding.UTF8;
                string js = wc.DownloadString("https://open.spotify.com/oembed?url=" + Uri.EscapeDataString("https://open.spotify.com/playlist/" + id));
                string n = Json.Str(Json.Parse(js), "title");
                if (n.Length > 0) return n.Replace('\t', ' ').Replace('\n', ' ').Replace('\r', ' ');
            }
        }
        catch { }
        return "Playlist " + id.Substring(0, 6);
    }
}
