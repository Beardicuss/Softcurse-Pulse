using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Pulse.Core;

namespace Pulse.App
{
    public class DashboardForm : Form
    {
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        // ── Palette ───────────────────────────────────────────────────────────────
        private static readonly Color BG       = Color.FromArgb(5,   8,  16);
        private static readonly Color BG2      = Color.FromArgb(2,   2,   2);
        private static readonly Color CYAN     = Color.FromArgb(0,  255, 255);
        private static readonly Color ORANGE   = Color.FromArgb(255, 107,  53);
        private static readonly Color GREEN    = Color.FromArgb(0,  255, 140);
        private static readonly Color DIM      = Color.FromArgb(22,  32,  48);
        private static readonly Color DIMTEXT  = Color.FromArgb(70,  95, 120);

        // ── Fields ────────────────────────────────────────────────────────────────
        private ActionEngine  _actionEngine;
        private ConfigManager _configManager;

        private Panel   _contentPanel;
        private Panel   _pnlLogsView;
        private Panel   _pnlHistoryView;
        private Panel   _pnlSettingsView;

        private ListBox _lstLogs;
        private ListBox _lstHistory;

        private Button  _btnTabLogs;
        private Button  _btnTabHistory;
        private Button  _btnTabSettings;

        private NumericUpDown _numNetPoll;
        private NumericUpDown _numProcPoll;
        private NumericUpDown _numCpuThresh;
        private TextBox       _txtSuspicious;
        private TextBox       _txtDiscordUrl;
        private TextBox       _txtTelegramToken;
        private TextBox       _txtTelegramChatId;

        private PictureBox         _graphBox;
        private List<long>         _latencyHistory = new List<long>();
        private TableLayoutPanel   _settingsTable;   // field so OnLoad can set initial width

        private System.Windows.Forms.Timer _blinkTimer;
        private Label  _lblStatus;
        private bool   _blinkOn;

        // ─────────────────────────────────────────────────────────────────────────
        public DashboardForm(ActionEngine actionEngine, ConfigManager configManager)
        {
            _actionEngine  = actionEngine;
            _configManager = configManager;
            InitializeComponent();
            LoadSettingsIntoUI();

            if (_actionEngine != null)
            {
                _actionEngine.OnAlertRequested += ActionEngine_OnAlertRequested;
                _actionEngine.OnMetricRecorded  += ActionEngine_OnMetricRecorded;
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        private void InitializeComponent()
        {
            this.Text            = "SOFTCURSE PULSE";
            this.Size            = new Size(620, 560);
            this.MinimumSize     = new Size(500, 400);
            this.StartPosition   = FormStartPosition.CenterScreen;
            this.Icon            = SystemIcons.Application;
            this.BackColor       = CYAN;           // 1-px neon border via Padding(1)
            this.Padding         = new Padding(1);
            this.FormBorderStyle = FormBorderStyle.None;

            // ── Title Bar ────────────────────────────────────────────────────────
            var titleBar = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 38,
                BackColor = BG
            };
            titleBar.MouseDown += TitleBar_MouseDown;
            titleBar.Paint     += (s, e) =>
            {
                // Neon bottom-edge accent line
                using var p = new Pen(CYAN, 1);
                e.Graphics.DrawLine(p, 0, titleBar.Height - 1, titleBar.Width, titleBar.Height - 1);
            };

            var btnClose = MakeTitleButton("■", ORANGE);
            btnClose.Click += (s, e) => this.Close();

            var btnMin = MakeTitleButton("▬", CYAN);
            btnMin.Click += (s, e) => this.WindowState = FormWindowState.Minimized;

            var pulseDot = new Label
            {
                Tag       = "ignore",
                Text      = "●",
                Width     = 22,
                Dock      = DockStyle.Left,
                ForeColor = GREEN,
                TextAlign = ContentAlignment.MiddleCenter,
                Font      = new Font("Consolas", 10F),
                BackColor = BG
            };

            var lblAppTitle = new Label
            {
                Text      = "  SOFTCURSE / PULSE  ·  SYSTEM MONITOR",
                ForeColor = CYAN,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font      = new Font("Consolas", 9F, FontStyle.Bold),
                BackColor = BG
            };
            lblAppTitle.MouseDown += TitleBar_MouseDown;

            titleBar.Controls.Add(lblAppTitle);
            titleBar.Controls.Add(pulseDot);
            titleBar.Controls.Add(btnMin);
            titleBar.Controls.Add(btnClose);

            // ── Tab Selector ─────────────────────────────────────────────────────
            var tabBar = new Panel { Dock = DockStyle.Top, Height = 34, BackColor = BG2 };
            tabBar.Paint += (s, e) =>
            {
                using var p = new Pen(DIM, 1);
                e.Graphics.DrawLine(p, 0, tabBar.Height - 1, tabBar.Width, tabBar.Height - 1);
            };

            _btnTabLogs     = MakeTabButton("⬡  ACTIVITY & GRAPHS");
            _btnTabHistory  = MakeTabButton("⚠  WARNING HISTORY");
            _btnTabSettings = MakeTabButton("⚙  SYSTEM SETTINGS");

            _btnTabLogs.Click     += (s, e) => SwitchTab(0);
            _btnTabHistory.Click  += (s, e) => { SwitchTab(1); RefreshHistory(); };
            _btnTabSettings.Click += (s, e) => SwitchTab(2);

            tabBar.Controls.Add(_btnTabLogs);
            tabBar.Controls.Add(_btnTabHistory);
            tabBar.Controls.Add(_btnTabSettings);
            tabBar.Resize += (s, e) => LayoutTabButtons(tabBar);

            // ── Status Bar ───────────────────────────────────────────────────────
            var statusBar = new Panel { Dock = DockStyle.Bottom, Height = 22, BackColor = BG };
            statusBar.Paint += (s, e) =>
            {
                using var p = new Pen(DIM, 1);
                e.Graphics.DrawLine(p, 0, 0, statusBar.Width, 0);
            };

            _lblStatus = new Label
            {
                Text      = "●  ONLINE  ·  MONITORING ACTIVE",
                ForeColor = GREEN,
                Dock      = DockStyle.Left,
                Width     = 300,
                TextAlign = ContentAlignment.MiddleLeft,
                Font      = new Font("Consolas", 8F),
                Padding   = new Padding(6, 0, 0, 0),
                BackColor = BG
            };
            var lblClock = new Label
            {
                Text      = DateTime.Now.ToString("HH:mm:ss"),
                ForeColor = DIMTEXT,
                Dock      = DockStyle.Right,
                Width     = 80,
                TextAlign = ContentAlignment.MiddleRight,
                Font      = new Font("Consolas", 8F),
                Padding   = new Padding(0, 0, 6, 0),
                BackColor = BG
            };
            statusBar.Controls.Add(_lblStatus);
            statusBar.Controls.Add(lblClock);

            _blinkTimer = new System.Windows.Forms.Timer { Interval = 900 };
            _blinkTimer.Tick += (s, e) =>
            {
                _blinkOn       = !_blinkOn;
                _lblStatus.ForeColor = _blinkOn ? GREEN : Color.FromArgb(0, 100, 70);
                lblClock.Text  = DateTime.Now.ToString("HH:mm:ss");
            };
            _blinkTimer.Start();

            // ── Content container ─────────────────────────────────────────────────
            // BUG 1 FIX: Padding(0,4) prevents the first listbox item from being
            // flush against the top edge and getting visually clipped.
            _contentPanel = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = BG,
                Padding   = new Padding(0, 4, 0, 0)
            };

            // ── View 1: Activity & Graphs ─────────────────────────────────────────
            _pnlLogsView = new Panel { Dock = DockStyle.Fill, BackColor = BG, Visible = true };

            var lblLogsTitle = MakeSectionLabel("⬡  LIVE SYSTEM ALERTS", CYAN);

            var pnlGraph = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 130,
                BackColor = BG,
                Padding   = new Padding(4)
            };
            _graphBox = new PictureBox { Dock = DockStyle.Fill, BackColor = BG };
            _graphBox.Paint += GraphBox_Paint;
            pnlGraph.Controls.Add(_graphBox);

            _lstLogs = MakeAlertList(CYAN);
            _lstLogs.MouseDown += LstLogs_MouseDown;

            // Dock add order: Top → Bottom → Fill (Fill must be last)
            _pnlLogsView.Controls.Add(lblLogsTitle); // Top
            _pnlLogsView.Controls.Add(pnlGraph);     // Bottom
            _pnlLogsView.Controls.Add(_lstLogs);     // Fill (last)

            // ── View 2: Warning History ───────────────────────────────────────────
            _pnlHistoryView = new Panel { Dock = DockStyle.Fill, BackColor = BG, Visible = false };

            var lblHistTitle = MakeSectionLabel("⚠  WARNING HISTORY LOG", ORANGE);

            _lstHistory = MakeAlertList(ORANGE);

            _pnlHistoryView.Controls.Add(lblHistTitle); // Top
            _pnlHistoryView.Controls.Add(_lstHistory);  // Fill (last)

            // ── View 3: Settings ─────────────────────────────────────────────────
            // Approach: _pnlSettingsView itself is the AutoScroll container.
            // _settingsTable is a field (not a local) so OnLoad can set its initial
            // width — the Resize handler alone isn't enough because it doesn't fire
            // during InitializeComponent when the panel hasn't been measured yet.
            _pnlSettingsView = new Panel
            {
                Dock       = DockStyle.Fill,
                BackColor  = BG,
                AutoScroll = true,
                Padding    = new Padding(12, 8, 12, 12),
                Visible    = false
            };

            _settingsTable = new TableLayoutPanel
            {
                Dock            = DockStyle.None,
                AutoSize        = true,
                AutoSizeMode    = AutoSizeMode.GrowAndShrink,
                ColumnCount     = 2,
                Location        = new Point(0, 0),
                BackColor       = BG,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            _settingsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 210));
            _settingsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            // Row 0: section header spanning both columns
            _settingsTable.RowCount = 1;
            _settingsTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            var hdrLabel = new Label
            {
                Text = "⚙  SYSTEM CONFIGURATION", Dock = DockStyle.Fill,
                Font = new Font("Consolas", 9F, FontStyle.Bold), ForeColor = CYAN,
                BackColor = DIM, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(6, 0, 0, 0), Margin = new Padding(0, 0, 0, 8)
            };
            _settingsTable.Controls.Add(hdrLabel, 0, 0);
            _settingsTable.SetColumnSpan(hdrLabel, 2);

            // Data rows (all 7 config fields)
            int r = 1;
            AddSettingsRow(_settingsTable, r++, "◈  Network Polling (ms)",
                _numNetPoll    = MakeNumeric(500, 60000, 500));
            AddSettingsRow(_settingsTable, r++, "◈  Process Polling (ms)",
                _numProcPoll   = MakeNumeric(1000, 120000, 1000));
            AddSettingsRow(_settingsTable, r++, "◈  CPU Alert Threshold (%)",
                _numCpuThresh  = MakeNumeric(1, 100, 1));
            AddSettingsRow(_settingsTable, r++, "◈  Suspicious Procs (csv)",
                _txtSuspicious = MakeTextInput());
            AddSettingsRow(_settingsTable, r++, "◈  Discord Webhook URL",
                _txtDiscordUrl = MakeTextInput());
            AddSettingsRow(_settingsTable, r++, "◈  Telegram Bot Token",
                _txtTelegramToken = MakeTextInput());
            AddSettingsRow(_settingsTable, r++, "◈  Telegram Chat ID",
                _txtTelegramChatId = MakeTextInput());

            // Spacer row
            _settingsTable.RowCount = r + 2;
            _settingsTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 16));
            _settingsTable.Controls.Add(new Label { BackColor = BG }, 0, r++);

            // Buttons row
            var pnlBtns = new FlowLayoutPanel
            {
                AutoSize = true, BackColor = BG,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false, Margin = new Padding(0)
            };
            var btnSave    = MakeActionButton("[ SAVE CONFIG ]",   CYAN);
            var btnPlugins = MakeActionButton("[ OPEN PLUGINS ]",  ORANGE);
            btnSave.Click    += (s, e) => SaveSettingsFromUI();
            btnPlugins.Click += (s, e) => OpenPluginsFolder();
            pnlBtns.Controls.Add(btnSave);
            pnlBtns.Controls.Add(btnPlugins);
            _settingsTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            _settingsTable.Controls.Add(pnlBtns, 0, r);
            _settingsTable.SetColumnSpan(pnlBtns, 2);

            // Resize handler keeps table full-width during window resizes
            _pnlSettingsView.Resize += (s, e) =>
            {
                var p = _pnlSettingsView;
                _settingsTable.Width = Math.Max(1, p.ClientSize.Width - p.Padding.Horizontal);
            };

            _pnlSettingsView.Controls.Add(_settingsTable);

            // ── Assemble ──────────────────────────────────────────────────────────
            _contentPanel.Controls.Add(_pnlLogsView);
            _contentPanel.Controls.Add(_pnlHistoryView);
            _contentPanel.Controls.Add(_pnlSettingsView);

            // BUG 2 FIX: In WinForms, the LAST Dock=Top control added claims y=0.
            // Old order: titleBar first → tabBar second → tabBar ends up at y=0.
            // Fix: add tabBar first, titleBar LAST → titleBar correctly wins y=0.
            this.Controls.Add(statusBar);       // Bottom
            this.Controls.Add(tabBar);          // Top → added 1st → sits at y=38
            this.Controls.Add(titleBar);        // Top → added LAST → wins y=0  ←
            this.Controls.Add(_contentPanel);   // Fill → takes remaining space

            SwitchTab(0);

            // Preload recent DB history into the activity list
            var recent = DatabaseManager.GetRecentAnomalies(50);
            recent.Reverse();
            foreach (var e in recent)
                _lstLogs.Items.Insert(0, e);
            _lstLogs.Items.Insert(0, $"[{DateTime.Now:HH:mm:ss}] ▶ SOFTCURSE PULSE ONLINE");
        }

        // ── Control factories ─────────────────────────────────────────────────────

        private Button MakeTitleButton(string text, Color color)
        {
            var btn = new Button
            {
                Tag       = "ignore",
                Text      = text,
                Dock      = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                Width     = 38,
                ForeColor = color,
                BackColor = BG,
                Cursor    = Cursors.Hand,
                Font      = new Font("Consolas", 10F, FontStyle.Bold)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.MouseEnter += (s, e) => btn.BackColor = DIM;
            btn.MouseLeave += (s, e) => btn.BackColor = BG;
            return btn;
        }

        private Button MakeTabButton(string text)
        {
            var btn = new Button
            {
                Text      = text,
                FlatStyle = FlatStyle.Flat,
                ForeColor = DIMTEXT,
                BackColor = BG2,
                Cursor    = Cursors.Hand,
                Font      = new Font("Consolas", 8F, FontStyle.Bold),
                Height    = 34,
                TextAlign = ContentAlignment.MiddleCenter
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private Label MakeSectionLabel(string text, Color color) => new Label
        {
            Text      = text,
            Dock      = DockStyle.Top,
            Height    = 28,
            AutoSize  = false,
            Font      = new Font("Consolas", 9F, FontStyle.Bold),
            ForeColor = color,
            BackColor = DIM,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(8, 0, 0, 0)
        };

        private ListBox MakeAlertList(Color fore) => new ListBox
        {
            Dock          = DockStyle.Fill,
            IntegralHeight= false,
            BackColor     = BG,
            ForeColor     = fore,
            Font          = new Font("Consolas", 9F),
            BorderStyle   = BorderStyle.None,
            DrawMode      = DrawMode.OwnerDrawFixed,
            ItemHeight    = 19
        };

        private NumericUpDown MakeNumeric(int min, int max, int inc) => new NumericUpDown
        {
            Minimum     = min, Maximum = max, Increment = inc,
            BackColor   = DIM, ForeColor = CYAN,
            Font        = new Font("Consolas", 9F),
            BorderStyle = BorderStyle.None,
            Margin      = new Padding(0, 4, 0, 4),
            Height      = 26
        };

        private TextBox MakeTextInput() => new TextBox
        {
            BackColor   = DIM, ForeColor = CYAN,
            BorderStyle = BorderStyle.None,
            Font        = new Font("Consolas", 9F),
            Margin      = new Padding(0, 4, 0, 4),
            Height      = 26
        };

        private Button MakeActionButton(string text, Color accent)
        {
            var btn = new Button
            {
                Text      = text, FlatStyle = FlatStyle.Flat,
                BackColor = BG,   ForeColor = accent,
                Font      = new Font("Consolas", 9F, FontStyle.Bold),
                Cursor    = Cursors.Hand, Height = 30,
                AutoSize  = true, Margin = new Padding(0, 0, 8, 0)
            };
            btn.FlatAppearance.BorderColor = accent;
            btn.FlatAppearance.BorderSize  = 1;
            btn.MouseEnter += (s, e) => btn.BackColor = DIM;
            btn.MouseLeave += (s, e) => btn.BackColor = BG;
            return btn;
        }

        private void AddSettingsRow(TableLayoutPanel table, int row, string labelText, Control input)
        {
            table.RowCount = Math.Max(table.RowCount, row + 1);
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));

            var lbl = new Label
            {
                Text = labelText, AutoSize = false, Dock = DockStyle.Fill,
                ForeColor = DIMTEXT, BackColor = BG, TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Consolas", 8F), Margin = new Padding(0, 4, 8, 4)
            };
            input.Dock = DockStyle.Fill;

            table.Controls.Add(lbl,   0, row);
            table.Controls.Add(input, 1, row);
        }

        // ── Tab switching ─────────────────────────────────────────────────────────

        private void LayoutTabButtons(Panel bar)
        {
            int w = bar.ClientSize.Width / 3;
            _btnTabLogs.SetBounds(0,       0, w, 34);
            _btnTabHistory.SetBounds(w,    0, w, 34);
            _btnTabSettings.SetBounds(w*2, 0, bar.ClientSize.Width - w*2, 34);
        }

        private void SwitchTab(int index)
        {
            _pnlLogsView.Visible     = index == 0;
            _pnlHistoryView.Visible  = index == 1;
            _pnlSettingsView.Visible = index == 2;

            SetTabActive(_btnTabLogs,     index == 0, CYAN);
            SetTabActive(_btnTabHistory,  index == 1, ORANGE);
            SetTabActive(_btnTabSettings, index == 2, CYAN);
        }

        private void SetTabActive(Button btn, bool active, Color accent)
        {
            btn.BackColor = active ? BG : BG2;
            btn.ForeColor = active ? accent : DIMTEXT;
        }

        // ── Owner-draw listbox ────────────────────────────────────────────────────

        // Wire this up for BOTH listboxes after construction
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            _lstLogs.DrawItem    += ListBox_DrawItem;
            _lstHistory.DrawItem += ListBox_DrawItem;
            LayoutTabButtons((Panel)_btnTabLogs.Parent);
        }

        private void ListBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            var lb   = (ListBox)sender;
            string   text = lb.Items[e.Index].ToString();
            bool     sel  = (e.State & DrawItemState.Selected) != 0;

            e.Graphics.FillRectangle(new SolidBrush(sel ? DIM : BG), e.Bounds);

            Color accent = GetAlertAccent(text);
            e.Graphics.FillRectangle(new SolidBrush(accent),
                new Rectangle(e.Bounds.X, e.Bounds.Y + 2, 2, e.Bounds.Height - 4));

            using var brush = new SolidBrush(GetAlertTextColor(text));
            var textRect = new Rectangle(e.Bounds.X + 8, e.Bounds.Y, e.Bounds.Width - 8, e.Bounds.Height);
            e.Graphics.DrawString(text, lb.Font, brush, textRect,
                new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap });
        }

        private static Color GetAlertAccent(string t)
        {
            if (t.Contains("Security") || t.Contains("Suspicious")) return ORANGE;
            if (t.Contains("CPU") || t.Contains("Memory") || t.Contains("Performance")) return Color.FromArgb(255, 210, 0);
            if (t.Contains("Network") || t.Contains("Latency")) return CYAN;
            if (t.Contains("ONLINE") || t.Contains("System Action")) return GREEN;
            return DIMTEXT;
        }

        private static Color GetAlertTextColor(string t)
        {
            if (t.Contains("Security") || t.Contains("Suspicious")) return ORANGE;
            if (t.Contains("CPU") || t.Contains("Memory") || t.Contains("Performance")) return Color.FromArgb(255, 230, 80);
            if (t.Contains("Network") || t.Contains("Latency")) return CYAN;
            if (t.Contains("ONLINE") || t.Contains("System Action")) return GREEN;
            return Color.FromArgb(170, 190, 210);
        }

        // ── Graph ─────────────────────────────────────────────────────────────────

        private void GraphBox_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(BG);

            int w = _graphBox.Width, h = _graphBox.Height;

            // Scan-line grid
            using var grid = new Pen(Color.FromArgb(12, 0, 255, 255), 1);
            for (int gx = 0; gx < w; gx += 30) g.DrawLine(grid, gx, 0, gx, h);
            for (int gy = 0; gy < h; gy += 20) g.DrawLine(grid, 0, gy, w, gy);

            if (_latencyHistory.Count < 2)
            {
                using var nb = new SolidBrush(DIMTEXT);
                g.DrawString("  AWAITING LATENCY DATA...", new Font("Consolas", 8F), nb, 6, 6);
                return;
            }

            float stepX      = (float)w / 100f;
            long  maxRaw     = _latencyHistory.Max();
            float maxLatency = (float)Math.Max(150.0, maxRaw * 1.2);

            var pts = new PointF[_latencyHistory.Count];
            for (int i = 0; i < _latencyHistory.Count; i++)
            {
                pts[i] = new PointF(
                    i * stepX,
                    h - ((float)_latencyHistory[i] / maxLatency * (h - 18)) - 4);
            }

            // Fill
            var fill = new List<PointF>(pts)
            {
                new PointF(pts.Last().X, h),
                new PointF(pts.First().X, h)
            };
            using var fb = new LinearGradientBrush(
                new Rectangle(0, 0, w, h),
                Color.FromArgb(50, 255, 107, 53),
                Color.FromArgb(0,  5,   8,  16),
                LinearGradientMode.Vertical);
            g.FillPolygon(fb, fill.ToArray());

            // Line
            using var lp = new Pen(ORANGE, 2f);
            g.DrawLines(lp, pts);

            // Label
            using var lb2 = new SolidBrush(CYAN);
            g.DrawString($"  ▶ LIVE LATENCY   MAX: {maxRaw}ms   SAMPLES: {_latencyHistory.Count}",
                new Font("Consolas", 8F, FontStyle.Bold), lb2, 4, 4);
        }

        // ── Window chrome ─────────────────────────────────────────────────────────

        private void TitleBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_NCHITTEST = 0x0084;
            base.WndProc(ref m);
            if (m.Msg == WM_NCHITTEST && m.Result.ToInt32() == 1)
            {
                Point p = PointToClient(Cursor.Position);
                int b = 10;
                if      (p.X <= b && p.Y <= b)                                                  m.Result = (IntPtr)13;
                else if (p.X <= b && p.Y >= ClientSize.Height - b)                              m.Result = (IntPtr)16;
                else if (p.X <= b)                                                               m.Result = (IntPtr)10;
                else if (p.X >= ClientSize.Width - b && p.Y <= b)                               m.Result = (IntPtr)14;
                else if (p.X >= ClientSize.Width - b && p.Y >= ClientSize.Height - b)           m.Result = (IntPtr)17;
                else if (p.X >= ClientSize.Width - b)                                            m.Result = (IntPtr)11;
                else if (p.Y <= b)                                                               m.Result = (IntPtr)12;
                else if (p.Y >= ClientSize.Height - b)                                           m.Result = (IntPtr)15;
            }
        }

        // ── Right-click context menu ──────────────────────────────────────────────

        private void LstLogs_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;
            int idx = _lstLogs.IndexFromPoint(e.Location);
            if (idx == ListBox.NoMatches) return;

            _lstLogs.SelectedIndex = idx;
            string logText = _lstLogs.Items[idx].ToString();
            var match = System.Text.RegularExpressions.Regex.Match(
                logText, @"detected:\s*([a-zA-Z0-9_\-\.]+)|([a-zA-Z0-9_\-\.]+)\s*is using");
            if (!match.Success) return;

            string proc = match.Groups[1].Success
                ? match.Groups[1].Value.Trim()
                : match.Groups[2].Value.Trim();
            if (string.IsNullOrEmpty(proc)) return;

            var menu = new ContextMenuStrip();
            menu.BackColor = BG; menu.ForeColor = CYAN;
            menu.Items.Add($"▶  Kill '{proc}'", null,
                (s, ev) => _actionEngine?.KillProcess(proc));
            menu.Items.Add($"⬡  Quarantine '{proc}' (Firewall Block)", null,
                (s, ev) => _actionEngine?.QuarantineProcess(proc));
            menu.Show(_lstLogs, e.Location);
        }

        // ── Settings ──────────────────────────────────────────────────────────────

        private void RefreshHistory()
        {
            _lstHistory.Items.Clear();
            foreach (var log in DatabaseManager.GetRecentAnomalies(100))
                _lstHistory.Items.Add(log);
        }

        private void LoadSettingsIntoUI()
        {
            if (_configManager?.CurrentConfig == null) return;
            var c = _configManager.CurrentConfig;
            _numNetPoll.Value    = Clamp((decimal)c.NetworkPollingIntervalMs, _numNetPoll);
            _numProcPoll.Value   = Clamp((decimal)c.ProcessPollingIntervalMs, _numProcPoll);
            _numCpuThresh.Value  = Clamp((decimal)c.CpuThresholdPercent,      _numCpuThresh);
            _txtSuspicious.Text     = string.Join(", ", c.SuspiciousProcesses);
            _txtDiscordUrl.Text     = c.DiscordWebhookUrl;
            _txtTelegramToken.Text  = c.TelegramBotToken;
            _txtTelegramChatId.Text = c.TelegramChatId;
        }

        private decimal Clamp(decimal v, NumericUpDown n) =>
            Math.Max(n.Minimum, Math.Min(n.Maximum, v));

        private void SaveSettingsFromUI()
        {
            if (_configManager == null) return;
            var c = _configManager.CurrentConfig;
            c.NetworkPollingIntervalMs = (int)_numNetPoll.Value;
            c.ProcessPollingIntervalMs = (int)_numProcPoll.Value;
            c.CpuThresholdPercent      = (double)_numCpuThresh.Value;
            c.SuspiciousProcesses = _txtSuspicious.Text
                .Split(',').Select(s => s.Trim().ToLowerInvariant())
                .Where(s => !string.IsNullOrEmpty(s)).ToList();
            c.DiscordWebhookUrl  = _txtDiscordUrl.Text.Trim();
            c.TelegramBotToken   = _txtTelegramToken.Text.Trim();
            c.TelegramChatId     = _txtTelegramChatId.Text.Trim();

            _configManager.SaveConfig();
            _lstLogs.Items.Insert(0, $"[{DateTime.Now:HH:mm:ss}] ▶ Config saved successfully.");
            SwitchTab(0);
        }

        private void OpenPluginsFolder()
        {
            try
            {
                var dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "SoftcursePulse", "Plugins");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                System.Diagnostics.Process.Start("explorer.exe", dir);
                MessageBox.Show("Drop .dll plugin files here, then restart Pulse to load them.",
                    "Plugins", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}"); }
        }

        // ── Engine event handlers ─────────────────────────────────────────────────

        private void ActionEngine_OnMetricRecorded(string name, long value)
        {
            if (name != "Latency") return;
            if (InvokeRequired) { BeginInvoke(new Action(() => ActionEngine_OnMetricRecorded(name, value))); return; }
            _latencyHistory.Add(value);
            if (_latencyHistory.Count > 100) _latencyHistory.RemoveAt(0);
            _graphBox.Invalidate();
        }

        private void ActionEngine_OnAlertRequested(string title, string message)
        {
            if (InvokeRequired) { BeginInvoke(new Action(() => ActionEngine_OnAlertRequested(title, message))); return; }
            string entry = $"[{DateTime.Now:HH:mm:ss}] [{title}] {message}";

            _lstLogs.Items.Insert(0, entry);
            if (_lstLogs.Items.Count > 100) _lstLogs.Items.RemoveAt(_lstLogs.Items.Count - 1);

            _lstHistory.Items.Insert(0, entry);
            if (_lstHistory.Items.Count > 100) _lstHistory.Items.RemoveAt(_lstHistory.Items.Count - 1);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _blinkTimer?.Stop();
            if (_actionEngine != null)
            {
                _actionEngine.OnAlertRequested -= ActionEngine_OnAlertRequested;
                _actionEngine.OnMetricRecorded  -= ActionEngine_OnMetricRecorded;
            }
            base.OnFormClosing(e);
        }
    }
}