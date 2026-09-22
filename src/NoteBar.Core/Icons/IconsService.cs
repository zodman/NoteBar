using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace NoteBar.Core.Icons
{
    public class IconsService
    {
        public static readonly string[] DefaultIconNames = new[]
        {
            "black", "blue", "cyan", "exclamation", "green",
            "orange", "purple", "question", "red", "white", "yellow"
        };

        public string FindIcon(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            var image = FindIconInAppData(name);
            if (image != null)
            {
                return image;
            }

            image = FindIconInResource(name);
            if (image != null)
            {
                return image;
            }

            return null;
        }

        public Stream GetIconStream(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            var appDataPath = FindIconInAppData(name);
            if (appDataPath != null && File.Exists(appDataPath))
            {
                return File.OpenRead(appDataPath);
            }

            var cleanName = name.ToLowerInvariant().Trim();
            if (DefaultIconNames.Contains(cleanName))
            {
                var assembly = typeof(IconsService).Assembly;
                var resName = $"NoteBar.Core.Icons.Resources.{cleanName}.png";
                var stream = assembly.GetManifestResourceStream(resName);
                if (stream != null)
                    return stream;
            }

            return null;
        }

        private string FindIconInAppData(string name)
        {
            var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NoteBar");
            var iconPath = Path.Combine(appData, $"{name}.png");

            return File.Exists(iconPath) ? iconPath : null;
        }

        private string FindIconInResource(string name)
        {
            var cleanName = name.ToLowerInvariant().Trim();
            if (DefaultIconNames.Contains(cleanName))
            {
                return $"pack://application:,,,/NoteBar.Core;component/Icons/Resources/{cleanName}.png";
            }

            return null;
        }
    }
}
