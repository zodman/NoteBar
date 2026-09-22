using System;
using System.IO;
using System.Text.Json;

namespace NoteBar.Wpf.Config
{
    public class UserSettings
    {
        public double WindowLeft { get; set; } = double.NaN;
        public double WindowTop { get; set; } = double.NaN;
        public bool IsBarVisible { get; set; } = true;

        private static string GetConfigPath()
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NoteBar");
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            return Path.Combine(folder, "config.json");
        }

        public static UserSettings Load()
        {
            try
            {
                var file = GetConfigPath();
                if (File.Exists(file))
                {
                    var json = File.ReadAllText(file);
                    return JsonSerializer.Deserialize<UserSettings>(json) ?? new UserSettings();
                }
            }
            catch
            {
            }
            return new UserSettings();
        }

        public void Save()
        {
            try
            {
                var file = GetConfigPath();
                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(file, json);
            }
            catch
            {
            }
        }
    }
}
