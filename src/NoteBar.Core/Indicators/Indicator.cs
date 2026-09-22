using System;
using System.Diagnostics;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NoteBar.Core.Icons;
using NoteBar.Core.Udp;

namespace NoteBar.Core.Indicators
{
    public class Indicator : INotifyPropertyChanged
    {
        public static Action<string> LogAction { get; set; }

        private UdpServer UdpServer { get; }
        private IconsService IconsService { get; }
        private Action<Indicator> QuitFnc { get; }

        public uint Port { get; }

        public string CurrentIconName { get; private set; } = "white";

        public event Action<Indicator> ImageChanged;

        private ImageSource _iconImage;
        public ImageSource IconImage
        {
            get => _iconImage;
            private set
            {
                if (Equals(value, _iconImage))
                    return;

                _iconImage = value;
                OnPropertyChanged();
            }
        }

        private string _imagePath;
        public string ImagePath
        {
            get => _imagePath;
            private set
            {
                if (value == _imagePath)
                    return;

                _imagePath = value;
                OnPropertyChanged();
            }
        }

        public Indicator(IconsService iconsService, uint port, Action<Indicator> quitFnc)
        {
            IconsService = iconsService ?? throw new ArgumentNullException(nameof(iconsService));
            Port = port;
            QuitFnc = quitFnc;

            UdpServer = new UdpServer(port, OnGetMessage);
            ImagePath = iconsService.FindIcon("white");
            IconImage = LoadIconImage("white");
        }

        public static Indicator Run(IconsService iconsService, uint port, Action<Indicator> quitFnc)
        {
            var indicator = new Indicator(iconsService, port, quitFnc);
            indicator.UdpServer.Start();
            return indicator;
        }

        public Stream GetCurrentIconStream()
        {
            return IconsService.GetIconStream(CurrentIconName) ?? IconsService.GetIconStream("question");
        }

        private ImageSource LoadIconImage(string iconName)
        {
            try
            {
                using var stream = IconsService.GetIconStream(iconName) ?? IconsService.GetIconStream("question");
                if (stream != null)
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    return bitmap;
                }
            }
            catch (Exception ex)
            {
                LogAction?.Invoke($"[Indicator:{Port}] Failed to load IconImage '{iconName}': {ex.Message}");
            }
            return null;
        }

        private void OnGetMessage(string message)
        {
            message = message?.Trim();
            LogAction?.Invoke($"[Indicator:{Port}] Processing message: '{message}'");

            if (string.Equals(message, "quit", StringComparison.OrdinalIgnoreCase))
            {
                QuitFnc?.Invoke(this);
                return;
            }

            var iconPath = IconsService.FindIcon(message);
            if (iconPath == null)
            {
                Trace.TraceWarning($"NoteBar: Cannot find '{message}' icon");
                LogAction?.Invoke($"[Indicator:{Port}] Unknown icon '{message}', falling back to 'question'");
                message = "question";
                iconPath = IconsService.FindIcon("question");
            }

            CurrentIconName = message;
            var newImageSource = LoadIconImage(message);

            void UpdateProperties()
            {
                ImagePath = iconPath;
                IconImage = newImageSource;
                ImageChanged?.Invoke(this);
                LogAction?.Invoke($"[Indicator:{Port}] Indicator updated to '{CurrentIconName}'");
            }

            if (Application.Current != null && Application.Current.Dispatcher != null &&
                !Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(UpdateProperties);
            }
            else
            {
                UpdateProperties();
            }
        }

        public void Quit()
        {
            UdpServer.ShutDown();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
