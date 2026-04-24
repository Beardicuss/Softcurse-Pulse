using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Pulse.Core;

namespace Pulse.App
{
    public class TrayApplication : ApplicationContext
    {
        private NotifyIcon _notifyIcon;
        private List<IModule> _modules;
        
        private Icon _iconGreen;
        private Icon _iconRed;
        
        private SynchronizationContext _syncContext;
        private DashboardForm _dashboardForm;
        private ConfigManager _configManager;

        public TrayApplication(List<IModule> modules, ConfigManager configManager)
        {
            _modules = modules;
            _configManager = configManager;
            _syncContext = SynchronizationContext.Current;
            
            InitializeIcons();
            InitializeContext();
            HookEvents();
        }

        private void InitializeIcons()
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            _iconGreen = new Icon(System.IO.Path.Combine(basePath, "pulse.ico"));
            _iconRed = new Icon(System.IO.Path.Combine(basePath, "red_pulse.ico"));
        }

        private void InitializeContext()
        {
            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Pulse Status: Running", null, (s, e) => { });
            contextMenu.Items.Add("-");
            contextMenu.Items.Add("Open Dashboard", null, (s, e) => ShowDashboard());
            contextMenu.Items.Add("-");
            
            foreach (var module in _modules)
            {
                var item = new ToolStripMenuItem($"{module.Name}: {(module.IsEnabled ? "Enabled" : "Disabled")}");
                item.Click += (s, e) => {
                    module.IsEnabled = !module.IsEnabled;
                    item.Text = $"{module.Name}: {(module.IsEnabled ? "Enabled" : "Disabled")}";
                    Logger.Info($"{module.Name} is now {(module.IsEnabled ? "Enabled" : "Disabled")}");
                };
                contextMenu.Items.Add(item);
            }

            contextMenu.Items.Add("-");
            contextMenu.Items.Add("Exit", null, (s, e) => Exit());

            _notifyIcon = new NotifyIcon()
            {
                Icon = _iconGreen,
                ContextMenuStrip = contextMenu,
                Text = "Pulse - Network & Process Monitor",
                Visible = true
            };

            _notifyIcon.DoubleClick += (s, e) => ShowDashboard();

            _notifyIcon.BalloonTipTitle = "Pulse Started";
            _notifyIcon.BalloonTipText = "Pulse is now monitoring your system in the background.";
            _notifyIcon.ShowBalloonTip(3000);
        }

        private void ShowDashboard()
        {
            if (_dashboardForm == null || _dashboardForm.IsDisposed)
            {
                var actionEngine = _modules.OfType<ActionEngine>().FirstOrDefault();
                _dashboardForm = new DashboardForm(actionEngine, _configManager);
                _dashboardForm.Show();
            }
            else
            {
                if (_dashboardForm.WindowState == FormWindowState.Minimized)
                    _dashboardForm.WindowState = FormWindowState.Normal;
                _dashboardForm.Activate();
            }
        }

        private void HookEvents()
        {
            var actionEngine = _modules.OfType<ActionEngine>().FirstOrDefault();
            if (actionEngine != null)
            {
                actionEngine.OnAlertRequested += (title, message) =>
                {
                    if (_syncContext != null)
                    {
                        _syncContext.Post(_ => ShowAlert(title, message), null);
                    }
                    else
                    {
                        ShowAlert(title, message);
                    }
                };
            }
        }
        
        private void ShowAlert(string title, string message)
        {
            try 
            {
                _notifyIcon.Icon = _iconRed;
                _notifyIcon.BalloonTipTitle = title;
                _notifyIcon.BalloonTipText = message;
                _notifyIcon.ShowBalloonTip(5000);
                
                // Revert icon back to green after 5 seconds
                Task.Delay(5000).ContinueWith(_ => 
                {
                    if (_syncContext != null)
                    {
                        _syncContext.Post(state => 
                        {
                            if (_notifyIcon != null && _notifyIcon.Visible) 
                                _notifyIcon.Icon = _iconGreen; 
                        }, null);
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.Error($"UI Alert Error: {ex.Message}");
            }
        }

        private void Exit()
        {
            _notifyIcon.Visible = false;
            Application.Exit();
        }
    }
}
