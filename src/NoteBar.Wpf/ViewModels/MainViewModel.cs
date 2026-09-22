using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using NoteBar.Core.Icons;
using NoteBar.Core.Indicators;
using NoteBar.Core.Ipc;
using NoteBar.Core.Udp;
using NoteBar.Wpf.Config;
using NoteBar.Wpf.MVVM;
using Application = System.Windows.Application;

namespace NoteBar.Wpf.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged, IDisposable
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr handle);

        private readonly IconsService _iconsService;
        private readonly IndicatorsService _indicatorsService;
        private readonly NoteBarIpcServer _ipcServer;
        private NotifyIcon _notifyIcon;
        private IntPtr _currentHIcon = IntPtr.Zero;
        private readonly UserSettings _settings;

        public ObservableCollection<Indicator> Indicators => _indicatorsService.Indicators;

        public UserSettings Settings => _settings;

        public event Action RequestResetPosition;
        public event Action RequestCenterPosition;
        public event Action<bool> RequestSetVisibility;

        private ICommand _quitIndicatorCommand;
        public ICommand QuitIndicatorCommand => _quitIndicatorCommand ??= new RelayCommand(param =>
        {
            if (param is Indicator indicator)
            {
                RemoveIndicator(indicator);
            }
        });

        public MainViewModel()
        {
            App.Log("MainViewModel initializing...");
            Indicator.LogAction = App.Log;
            UdpServer.LogAction = App.Log;

            _iconsService = new IconsService();
            _indicatorsService = new IndicatorsService(_iconsService, RemoveIndicator);
            _settings = UserSettings.Load();

            NoteBarIpcServer.LogAction = App.Log;
            _ipcServer = new NoteBarIpcServer(HandleIpcCommand);
            _ipcServer.Start();
            App.Log("NoteBarIpcServer started");

            InitNotifyIcon();
        }

        private void InitNotifyIcon()
        {
            try
            {
                _notifyIcon = new NotifyIcon();
                UpdateNotifyIconImage("cyan");
                _notifyIcon.Text = "NoteBar - Taskbar Indicators";
                _notifyIcon.Visible = true;
                App.Log("NotifyIcon created and visible=true");

                var contextMenu = new ContextMenuStrip();
                var titleItem = new ToolStripMenuItem("NoteBar (Windows 11)") { Enabled = false };
                titleItem.Font = new Font(titleItem.Font, System.Drawing.FontStyle.Bold);
                contextMenu.Items.Add(titleItem);
                contextMenu.Items.Add(new ToolStripSeparator());

                var toggleItem = new ToolStripMenuItem("Show / Hide Floating Bar", null, (s, e) =>
                {
                    ToggleBarVisibility();
                });
                contextMenu.Items.Add(toggleItem);

                var centerItem = new ToolStripMenuItem("Center Bar on Screen", null, (s, e) =>
                {
                    RequestCenterPosition?.Invoke();
                });
                contextMenu.Items.Add(centerItem);

                var resetItem = new ToolStripMenuItem("Reset Bar to Taskbar Corner", null, (s, e) =>
                {
                    RequestResetPosition?.Invoke();
                });
                contextMenu.Items.Add(resetItem);

                contextMenu.Items.Add(new ToolStripSeparator());

                var exitItem = new ToolStripMenuItem("Exit NoteBar", null, (s, e) =>
                {
                    ExitApplication();
                });
                contextMenu.Items.Add(exitItem);

                _notifyIcon.ContextMenuStrip = contextMenu;
                _notifyIcon.DoubleClick += (s, e) => ToggleBarVisibility();

                _notifyIcon.ShowBalloonTip(3000, "NoteBar Active", "NoteBar is running! Look for the floating bar or click '^' in the system tray.", ToolTipIcon.Info);
            }
            catch (Exception ex)
            {
                App.Log($"InitNotifyIcon failed: {ex}");
            }
        }

        private void UpdateNotifyIconImage(string iconName)
        {
            try
            {
                using var stream = _iconsService.GetIconStream(iconName) ?? _iconsService.GetIconStream("white");
                if (stream != null)
                {
                    using var bmp = new Bitmap(stream);
                    var newHIcon = bmp.GetHicon();
                    var newIcon = Icon.FromHandle(newHIcon);

                    _notifyIcon.Icon = newIcon;

                    if (_currentHIcon != IntPtr.Zero)
                    {
                        DestroyIcon(_currentHIcon);
                    }
                    _currentHIcon = newHIcon;

                    App.Log($"NotifyIcon image updated to: {iconName} (HICON=0x{newHIcon:X})");
                }
            }
            catch (Exception ex)
            {
                App.Log($"UpdateNotifyIconImage exception: {ex}");
                if (_notifyIcon.Icon == null)
                {
                    _notifyIcon.Icon = SystemIcons.Application;
                }
            }
        }

        public string AddIndicator(uint port)
        {
            App.Log($"AddIndicator: port {port}");
            var result = _indicatorsService.Add(port);
            if (string.IsNullOrEmpty(result))
            {
                var ind = _indicatorsService.Indicators.FirstOrDefault(i => i.Port == port);
                if (ind != null)
                {
                    ind.ImageChanged += (i) =>
                    {
                        if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
                        {
                            Application.Current.Dispatcher.Invoke(UpdateTrayStatus);
                        }
                        else
                        {
                            UpdateTrayStatus();
                        }
                    };
                }
                UpdateTrayStatus();
                RequestSetVisibility?.Invoke(true);
            }
            App.Log($"AddIndicator result: {result ?? "OK"}");
            return result;
        }

        public void RemoveIndicator(Indicator indicator)
        {
            if (indicator == null) return;

            void RemoveAction()
            {
                App.Log($"Removing indicator: {indicator.Port}");
                _indicatorsService.Remove(indicator);
                UpdateTrayStatus();
            }

            if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(RemoveAction);
            }
            else
            {
                RemoveAction();
            }
        }

        private void UpdateTrayStatus()
        {
            try
            {
                var count = _indicatorsService.Indicators.Count;
                if (count > 0)
                {
                    var text = $"NoteBar ({count} indicator{(count > 1 ? "s" : "")})";
                    if (text.Length > 63) text = text.Substring(0, 63);
                    _notifyIcon.Text = text;

                    var last = _indicatorsService.Indicators.LastOrDefault();
                    if (last != null)
                    {
                        UpdateNotifyIconImage(last.CurrentIconName);
                    }
                }
                else
                {
                    _notifyIcon.Text = "NoteBar - Ready";
                    UpdateNotifyIconImage("cyan");
                }
            }
            catch (Exception ex)
            {
                App.Log($"UpdateTrayStatus failed: {ex}");
            }
        }

        private string HandleIpcCommand(string command)
        {
            App.Log($"HandleIpcCommand received: '{command}'");
            if (string.IsNullOrWhiteSpace(command))
                return "ERROR:Empty command";

            command = command.Trim();

            if (command.StartsWith("ADD:", StringComparison.OrdinalIgnoreCase))
            {
                var portStr = command.Substring(4).Trim();
                if (uint.TryParse(portStr, out var port))
                {
                    string result = null;
                    if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            result = AddIndicator(port);
                        });
                    }
                    else
                    {
                        result = AddIndicator(port);
                    }

                    return string.IsNullOrEmpty(result) ? "OK" : "ERROR:" + result;
                }
                return "ERROR:Invalid port";
            }

            if (command.StartsWith("REMOVE:", StringComparison.OrdinalIgnoreCase))
            {
                var portStr = command.Substring(7).Trim();
                if (uint.TryParse(portStr, out var port))
                {
                    void RemoveTarget()
                    {
                        var target = _indicatorsService.Indicators.FirstOrDefault(i => i.Port == port);
                        if (target != null)
                        {
                            RemoveIndicator(target);
                        }
                    }

                    if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
                    {
                        Application.Current.Dispatcher.Invoke(RemoveTarget);
                    }
                    else
                    {
                        RemoveTarget();
                    }
                    return "OK";
                }
                return "ERROR:Invalid port";
            }

            if (string.Equals(command, "QUIT", StringComparison.OrdinalIgnoreCase))
            {
                if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
                {
                    Application.Current.Dispatcher.Invoke(ExitApplication);
                }
                else
                {
                    ExitApplication();
                }
                return "OK";
            }

            return "ERROR:Unknown command";
        }

        public void ToggleBarVisibility()
        {
            _settings.IsBarVisible = !_settings.IsBarVisible;
            _settings.Save();
            RequestSetVisibility?.Invoke(_settings.IsBarVisible);
        }

        public void ExitApplication()
        {
            App.Log("ExitApplication called");
            _settings.Save();
            _indicatorsService.ShutDownAll();
            _ipcServer.Stop();
            _notifyIcon?.Dispose();
            if (_currentHIcon != IntPtr.Zero)
            {
                DestroyIcon(_currentHIcon);
                _currentHIcon = IntPtr.Zero;
            }
            Application.Current?.Shutdown();
        }

        public void Dispose()
        {
            _ipcServer?.Dispose();
            _notifyIcon?.Dispose();
            if (_currentHIcon != IntPtr.Zero)
            {
                DestroyIcon(_currentHIcon);
                _currentHIcon = IntPtr.Zero;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
