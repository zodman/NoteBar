using System;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Input;
using NoteBar.Core.Indicators;
using NoteBar.Wpf.ViewModels;

namespace NoteBar.Wpf
{
    public partial class MainWindow : Window
    {
        private MainViewModel ViewModel { get; }

        public MainWindow(uint? initialPort = null)
        {
            InitializeComponent();

            ViewModel = new MainViewModel();
            DataContext = ViewModel;

            ViewModel.RequestResetPosition += ResetPosition;
            ViewModel.RequestCenterPosition += CenterOnScreen;
            ViewModel.RequestSetVisibility += SetBarVisibility;

            ViewModel.Indicators.CollectionChanged += Indicators_CollectionChanged;
            UpdateEmptyPanelVisibility();

            Loaded += MainWindow_Loaded;

            if (initialPort.HasValue)
            {
                ViewModel.AddIndicator(initialPort.Value);
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ApplySavedOrInitialPosition();

            if (!ViewModel.Settings.IsBarVisible)
            {
                Visibility = Visibility.Collapsed;
            }
            App.Log($"MainWindow Loaded. Position: Left={Left}, Top={Top}, Visibility={Visibility}");
        }

        private void Indicators_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateEmptyPanelVisibility();
        }

        private void UpdateEmptyPanelVisibility()
        {
            if (EmptyPanel != null)
            {
                EmptyPanel.Visibility = ViewModel.Indicators.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void ApplySavedOrInitialPosition()
        {
            var settings = ViewModel.Settings;
            if (!double.IsNaN(settings.WindowLeft) && !double.IsNaN(settings.WindowTop))
            {
                var vLeft = SystemParameters.VirtualScreenLeft;
                var vTop = SystemParameters.VirtualScreenTop;
                var vWidth = SystemParameters.VirtualScreenWidth;
                var vHeight = SystemParameters.VirtualScreenHeight;

                if (settings.WindowLeft >= vLeft && settings.WindowLeft < (vLeft + vWidth - 50) &&
                    settings.WindowTop >= vTop && settings.WindowTop < (vTop + vHeight - 30))
                {
                    Left = settings.WindowLeft;
                    Top = settings.WindowTop;
                    return;
                }
            }

            ResetPosition();
        }

        public void ResetPosition()
        {
            // Position near bottom-right on primary screen work area (above taskbar)
            Left = Math.Max(20, SystemParameters.WorkArea.Right - 280);
            Top = Math.Max(20, SystemParameters.WorkArea.Bottom - 65);

            SavePosition();
            App.Log($"Position reset to: Left={Left}, Top={Top}");
        }

        public void CenterOnScreen()
        {
            Left = Math.Max(20, (SystemParameters.PrimaryScreenWidth - (ActualWidth > 0 ? ActualWidth : 200)) / 2);
            Top = Math.Max(20, (SystemParameters.PrimaryScreenHeight - (ActualHeight > 0 ? ActualHeight : 45)) / 2);

            SavePosition();
            App.Log($"Position centered to: Left={Left}, Top={Top}");
        }

        private void SavePosition()
        {
            if (IsLoaded && WindowState == WindowState.Normal)
            {
                ViewModel.Settings.WindowLeft = Left;
                ViewModel.Settings.WindowTop = Top;
                ViewModel.Settings.Save();
            }
        }

        private void SetBarVisibility(bool visible)
        {
            if (visible)
            {
                Show();
                WindowState = WindowState.Normal;
                Activate();
            }
            else
            {
                Hide();
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
                SavePosition();
            }
        }

        private void MenuItemQuit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is Indicator indicator)
            {
                ViewModel.RemoveIndicator(indicator);
            }
        }

        private void MenuItemHideBar_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ToggleBarVisibility();
        }

        private void MenuItemResetPos_Click(object sender, RoutedEventArgs e)
        {
            ResetPosition();
        }

        private void MenuItemCenterPos_Click(object sender, RoutedEventArgs e)
        {
            CenterOnScreen();
        }

        private void MenuItemExit_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ExitApplication();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            ViewModel.Dispose();
        }
    }
}
