using System;
using System.Collections.Generic;
using System.Drawing;
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

        private ActionEngine _actionEngine;
        private ConfigManager _configManager;
        
        private TabControl _tabControl;
        private ListBox _lstLogs;
        
        // Settings UI
        private NumericUpDown _numNetPoll;
        private NumericUpDown _numProcPoll;
        private NumericUpDown _numCpuThresh;
        private TextBox _txtSuspicious;
        private TextBox _txtDiscordUrl;
        private TextBox _txtTelegramToken;
        private TextBox _txtTelegramChatId;

        // Graph UI
        private PictureBox _graphBox;
        private List<long> _latencyHistory = new List<long>();

        public DashboardForm(ActionEngine actionEngine, ConfigManager configManager)
        {
            _actionEngine = actionEngine;
            _configManager = configManager;
            InitializeComponent();
            LoadSettingsIntoUI();
            
            if (_actionEngine != null)
            {
                _actionEngine.OnAlertRequested += ActionEngine_OnAlertRequested;
                _actionEngine.OnMetricRecorded += ActionEngine_OnMetricRecorded;
            }
        }

        private void InitializeComponent()
        {
            this.Text = "Softcurse Pulse Dashboard";
            this.Size = new Size(560, 520);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Icon = SystemIcons.Application;
            this.BackColor = Color.FromArgb(2, 2, 2);
            this.FormBorderStyle = FormBorderStyle.None;
            // Draw a fake 1px cyan border
            this.Paint += (s, e) => { e.Graphics.DrawRectangle(new Pen(Color.FromArgb(0, 255, 255), 2), 0, 0, this.Width - 1, this.Height - 1); };

            var titleBar = new Panel { Dock = DockStyle.Top, Height = 35, BackColor = Color.FromArgb(5, 8, 16) };
            titleBar.MouseDown += TitleBar_MouseDown;

            var btnClose = new Button { Tag = "ignore", Text = "X", Dock = DockStyle.Right, FlatStyle = FlatStyle.Flat, Width = 35, ForeColor = Color.FromArgb(255, 107, 53), Cursor = Cursors.Hand, Font = new Font("Consolas", 10F, FontStyle.Bold) };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => this.Close();

            var btnMin = new Button { Tag = "ignore", Text = "_", Dock = DockStyle.Right, FlatStyle = FlatStyle.Flat, Width = 35, ForeColor = Color.FromArgb(0, 255, 255), Cursor = Cursors.Hand, Font = new Font("Consolas", 10F, FontStyle.Bold) };
            btnMin.FlatAppearance.BorderSize = 0;
            btnMin.Click += (s, e) => this.WindowState = FormWindowState.Minimized;

            var lblAppTitle = new Label { Text = "  " + this.Text, ForeColor = Color.FromArgb(0, 255, 255), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Bebas Neue", 11F, FontStyle.Regular) };
            lblAppTitle.MouseDown += TitleBar_MouseDown;

            titleBar.Controls.Add(lblAppTitle);
            titleBar.Controls.Add(btnMin);
            titleBar.Controls.Add(btnClose);

            _tabControl = new TabControl { Dock = DockStyle.Fill, DrawMode = TabDrawMode.OwnerDrawFixed, ItemSize = new Size(130, 25), Font = new Font("Space Mono", 9F) };
            _tabControl.Padding = new Point(15, 3);
            _tabControl.DrawItem += TabControl_DrawItem;
            
            // --- Tab 1: Logs & Graphs ---
            var tabLogs = new TabPage("Activity & Graphs") { BackColor = Color.FromArgb(2, 2, 2) };
            _lstLogs = new ListBox
            {
                Dock = DockStyle.Fill,
                IntegralHeight = false,
                BackColor = Color.FromArgb(5, 8, 16),
                ForeColor = Color.FromArgb(0, 255, 204), // Cyan Live
                Font = new Font("Space Mono", 10F, FontStyle.Regular),
                BorderStyle = BorderStyle.None
            };
            var lblTitle = new Label 
            { 
                Text = "◆ SOFTCURSE/SYS ALERTS", 
                Dock = DockStyle.Top, 
                Font = new Font("Bebas Neue", 12F, FontStyle.Regular),
                Padding = new Padding(5, 5, 5, 15),
                ForeColor = Color.FromArgb(0, 255, 255), // Cyan
                AutoSize = true
            };

            // Setup Graph Panel at the bottom
            var pnlGraph = new Panel { Dock = DockStyle.Bottom, Height = 120, BackColor = Color.FromArgb(2, 2, 2), Padding = new Padding(2) };
            _graphBox = new PictureBox { Dock = DockStyle.Fill };
            _graphBox.Paint += GraphBox_Paint;
            pnlGraph.Controls.Add(_graphBox);

            tabLogs.Controls.Add(_lstLogs);
            tabLogs.Controls.Add(lblTitle);
            tabLogs.Controls.Add(pnlGraph); // bottom layer

            // --- Tab 2: Settings ---
            var tabSettings = new TabPage("Settings") { Padding = new Padding(10), BackColor = Color.FromArgb(2, 2, 2), ForeColor = Color.FromArgb(232, 244, 248) };
            
            var table = new TableLayoutPanel 
            { 
                Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, RowCount = 9,
                Font = new Font("DM Sans", 9F)
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));

            table.Controls.Add(new Label { Text = "Network Polling (ms):", Anchor = AnchorStyles.Left }, 0, 0);
            _numNetPoll = new NumericUpDown { Minimum = 500, Maximum = 60000, Increment = 500, Width = 150 };
            table.Controls.Add(_numNetPoll, 1, 0);

            table.Controls.Add(new Label { Text = "Process Polling (ms):", Anchor = AnchorStyles.Left }, 0, 1);
            _numProcPoll = new NumericUpDown { Minimum = 1000, Maximum = 120000, Increment = 1000, Width = 150 };
            table.Controls.Add(_numProcPoll, 1, 1);

            table.Controls.Add(new Label { Text = "CPU Threshold (%):", Anchor = AnchorStyles.Left }, 0, 2);
            _numCpuThresh = new NumericUpDown { Minimum = 1, Maximum = 100, Width = 150 };
            table.Controls.Add(_numCpuThresh, 1, 2);

            table.Controls.Add(new Label { Text = "Suspicious Processes (csv):", Anchor = AnchorStyles.Left }, 0, 3);
            _txtSuspicious = new TextBox { Width = 250 };
            table.Controls.Add(_txtSuspicious, 1, 3);

            table.Controls.Add(new Label { Text = "Discord Webhook URL:", Anchor = AnchorStyles.Left }, 0, 4);
            _txtDiscordUrl = new TextBox { Width = 250 };
            table.Controls.Add(_txtDiscordUrl, 1, 4);

            table.Controls.Add(new Label { Text = "Telegram Bot Token:", Anchor = AnchorStyles.Left }, 0, 5);
            _txtTelegramToken = new TextBox { Width = 250 };
            table.Controls.Add(_txtTelegramToken, 1, 5);

            table.Controls.Add(new Label { Text = "Telegram Chat ID:", Anchor = AnchorStyles.Left }, 0, 6);
            _txtTelegramChatId = new TextBox { Width = 250 };
            table.Controls.Add(_txtTelegramChatId, 1, 6);

            var btnSave = new Button { Text = "Save Settings", Margin = new Padding(0, 10, 0, 0), Height = 30 };
            btnSave.Click += (s, e) => SaveSettingsFromUI();
            table.Controls.Add(btnSave, 1, 7);

            var btnOpenPlugins = new Button { Text = "Open Plugins Folder", Margin = new Padding(0, 10, 0, 0), Height = 30, Width = 150 };
            btnOpenPlugins.Click += (s, e) => OpenPluginsFolder();
            table.Controls.Add(btnOpenPlugins, 0, 8);

            tabSettings.Controls.Add(table);

            var tabHistory = new TabPage("Warning History") { BackColor = Color.FromArgb(2, 2, 2) };
            var lstHistory = new ListBox
            {
                Dock = DockStyle.Fill,
                IntegralHeight = false,
                BackColor = Color.FromArgb(5, 8, 16),
                ForeColor = Color.FromArgb(255, 107, 53),
                Font = new Font("Space Mono", 9F),
                BorderStyle = BorderStyle.None
            };
            foreach (var log in DatabaseManager.GetRecentAnomalies(100))
            {
                lstHistory.Items.Add(log);
            }
            tabHistory.Controls.Add(lstHistory);

            _tabControl.TabPages.Add(tabLogs);
            _tabControl.TabPages.Add(tabHistory);
            _tabControl.TabPages.Add(tabSettings);

            this.Controls.Add(_tabControl);
            this.Controls.Add(titleBar);
            titleBar.BringToFront();
            ApplyCyberpunkTheme(this);

            _lstLogs.Items.Add($"[{DateTime.Now:HH:mm:ss}] Softcurse Pulse Dashboard Online");
            _lstLogs.MouseDown += LstLogs_MouseDown;
        }

        private void TitleBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void TabControl_DrawItem(object sender, DrawItemEventArgs e)
        {
            var g = e.Graphics;
            var tabArea = _tabControl.GetTabRect(e.Index);
            var tabPage = _tabControl.TabPages[e.Index];

            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            
            using var backBrush = new SolidBrush(isSelected ? Color.FromArgb(5, 8, 16) : Color.FromArgb(2, 2, 2));
            g.FillRectangle(backBrush, tabArea);

            var textColor = isSelected ? Color.FromArgb(0, 255, 255) : Color.FromArgb(100, 100, 100);
            using var textBrush = new SolidBrush(textColor);
            
            var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(tabPage.Text, _tabControl.Font, textBrush, tabArea, format);
        }

        private void LstLogs_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                int index = _lstLogs.IndexFromPoint(e.Location);
                if (index != ListBox.NoMatches)
                {
                    _lstLogs.SelectedIndex = index;
                    string logText = _lstLogs.Items[index].ToString();
                    
                    var match = System.Text.RegularExpressions.Regex.Match(logText, @"detected:\s*([a-zA-Z0-9_\-\.]+)|([a-zA-Z0-9_\-\.]+)\s*is using");
                    if (match.Success)
                    {
                        string processName = match.Groups[1].Success ? match.Groups[1].Value.Trim() : match.Groups[2].Value.Trim();
                        if (!string.IsNullOrEmpty(processName))
                        {
                            var menu = new ContextMenuStrip();
                            menu.Items.Add($"Kill '{processName}'", null, (s, ev) => _actionEngine?.KillProcess(processName));
                            menu.Items.Add($"Quarantine '{processName}' (Firewall Block)", null, (s, ev) => _actionEngine?.QuarantineProcess(processName));
                            menu.Show(_lstLogs, e.Location);
                        }
                    }
                }
            }
        }

        private void GraphBox_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Color.FromArgb(2, 2, 2));
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            if (_latencyHistory.Count < 2) return;

            var w = _graphBox.Width;
            var h = _graphBox.Height;
            float stepX = (float)w / 100f; // Track last 100 ticks
            long maxRaw = _latencyHistory.Max();
            float maxLatency = (float)Math.Max(150.0, maxRaw * 1.2); 
            
            var points = new PointF[_latencyHistory.Count];
            for (int i = 0; i < _latencyHistory.Count; i++)
            {
                float x = i * stepX;
                float y = h - ((float)_latencyHistory[i] / maxLatency * h);
                points[i] = new PointF(x, y);
            }

            using var pen = new Pen(Color.FromArgb(255, 107, 53), 2f);
            g.DrawLines(pen, points);
            using var brush = new SolidBrush(Color.FromArgb(0, 255, 255));
            g.DrawString($"LIVE LATENCY (MAX {maxRaw}MS)", new Font("Space Mono", 8), brush, 5, 5);
        }

        private void ApplyCyberpunkTheme(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                if (c is TextBox || c is NumericUpDown)
                {
                    c.BackColor = Color.FromArgb(5, 8, 16);
                    c.ForeColor = Color.FromArgb(0, 255, 204);
                    if (c is TextBox txt) txt.BorderStyle = BorderStyle.FixedSingle;
                }
                else if (c is Button btn && btn.Tag?.ToString() != "ignore")
                {
                    btn.FlatStyle = FlatStyle.Flat;
                    btn.FlatAppearance.BorderColor = Color.FromArgb(0, 255, 255);
                    btn.FlatAppearance.BorderSize = 1;
                    btn.BackColor = Color.FromArgb(2, 2, 2);
                    btn.ForeColor = Color.FromArgb(0, 255, 255);
                }
                
                if (c.HasChildren)
                {
                    ApplyCyberpunkTheme(c);
                }
            }
        }

        private void LoadSettingsIntoUI()
        {
            if (_configManager?.CurrentConfig == null) return;
            var c = _configManager.CurrentConfig;
            _numNetPoll.Value = Math.Max(_numNetPoll.Minimum, Math.Min(_numNetPoll.Maximum, c.NetworkPollingIntervalMs));
            _numProcPoll.Value = Math.Max(_numProcPoll.Minimum, Math.Min(_numProcPoll.Maximum, c.ProcessPollingIntervalMs));
            _numCpuThresh.Value = (decimal)Math.Max((double)_numCpuThresh.Minimum, Math.Min((double)_numCpuThresh.Maximum, c.CpuThresholdPercent));
            _txtSuspicious.Text = string.Join(", ", c.SuspiciousProcesses);
            _txtDiscordUrl.Text = c.DiscordWebhookUrl;
            _txtTelegramToken.Text = c.TelegramBotToken;
            _txtTelegramChatId.Text = c.TelegramChatId;
        }

        private void OpenPluginsFolder()
        {
            try
            {
                var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SoftcursePulse");
                var pluginsDir = System.IO.Path.Combine(appData, "Plugins");
                if (!System.IO.Directory.Exists(pluginsDir)) System.IO.Directory.CreateDirectory(pluginsDir);
                System.Diagnostics.Process.Start("explorer.exe", pluginsDir);
                MessageBox.Show("Drop your .dll plugin files into this folder, then restart Pulse to load them automatically!", "Plugins", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open plugins folder: {ex.Message}", "Error");
            }
        }

        private void SaveSettingsFromUI()
        {
            if (_configManager == null) return;
            var c = _configManager.CurrentConfig;
            c.NetworkPollingIntervalMs = (int)_numNetPoll.Value;
            c.ProcessPollingIntervalMs = (int)_numProcPoll.Value;
            c.CpuThresholdPercent = (double)_numCpuThresh.Value;
            c.SuspiciousProcesses = _txtSuspicious.Text.Split(',')
                .Select(s => s.Trim().ToLowerInvariant())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
            c.DiscordWebhookUrl = _txtDiscordUrl.Text.Trim();
            c.TelegramBotToken = _txtTelegramToken.Text.Trim();
            c.TelegramChatId = _txtTelegramChatId.Text.Trim();

            _configManager.SaveConfig();
            
            _lstLogs.Items.Insert(0, $"[{DateTime.Now:HH:mm:ss}] [System] Config Updated! Network loop timing may require next cycle to catch new value.");
            _tabControl.SelectedIndex = 0;
        }

        private void ActionEngine_OnMetricRecorded(string name, long value)
        {
            if (name == "Latency")
            {
                if (this.InvokeRequired)
                {
                    this.BeginInvoke(new Action(() => ActionEngine_OnMetricRecorded(name, value)));
                    return;
                }
                _latencyHistory.Add(value);
                if (_latencyHistory.Count > 100) _latencyHistory.RemoveAt(0);
                _graphBox.Invalidate(); // trigger repaint
            }
        }

        private void ActionEngine_OnAlertRequested(string title, string message)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => ActionEngine_OnAlertRequested(title, message)));
                return;
            }

            _lstLogs.Items.Insert(0, $"[{DateTime.Now:HH:mm:ss}] [{title}] {message}");
            if (_lstLogs.Items.Count > 100)
            {
                _lstLogs.Items.RemoveAt(_lstLogs.Items.Count - 1);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_actionEngine != null)
            {
                _actionEngine.OnAlertRequested -= ActionEngine_OnAlertRequested;
                _actionEngine.OnMetricRecorded -= ActionEngine_OnMetricRecorded;
            }
            base.OnFormClosing(e);
        }
    }
}
