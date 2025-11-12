using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GlobalAudioSoundboard.Models;
using Newtonsoft.Json;

namespace GlobalAudioSoundboard.Services
{
    public class ConfigManager
    {
        private static readonly string AppDataFolder = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "Data");
        private static readonly string SoundsFolder = Path.Combine(AppDataFolder, "Sounds");
        private static readonly string ConfigFile = Path.Combine(AppDataFolder, "config.json");
        private static readonly string SettingsFile = Path.Combine(AppDataFolder, "settings.json");

        public ConfigManager()
        {
            EnsureDirectoriesExist();
        }

        private void EnsureDirectoriesExist()
        {
            Directory.CreateDirectory(AppDataFolder);
            Directory.CreateDirectory(SoundsFolder);
        }

        public List<SoundItem> LoadSounds()
        {
            if (!File.Exists(ConfigFile))
                return new List<SoundItem>();

            try
            {
                var json = File.ReadAllText(ConfigFile);
                return JsonConvert.DeserializeObject<List<SoundItem>>(json) ?? new List<SoundItem>();
            }
            catch
            {
                return new List<SoundItem>();
            }
        }

        public void SaveSounds(List<SoundItem> sounds)
        {
            try
            {
                var json = JsonConvert.SerializeObject(sounds, Formatting.Indented);
                File.WriteAllText(ConfigFile, json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to save configuration: {ex.Message}");
            }
        }

        public string ImportSound(string sourceFilePath)
        {
            var fileName = Path.GetFileName(sourceFilePath);
            var destPath = Path.Combine(SoundsFolder, fileName);

            // Handle duplicate names
            int counter = 1;
            while (File.Exists(destPath))
            {
                var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                var ext = Path.GetExtension(fileName);
                destPath = Path.Combine(SoundsFolder, $"{nameWithoutExt}_{counter}{ext}");
                counter++;
            }

            File.Copy(sourceFilePath, destPath, false);
            return destPath;
        }

        public void DeleteSound(string filePath)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        public string GetSoundsFolder() => SoundsFolder;

        public AppSettings LoadSettings()
        {
            if (!File.Exists(SettingsFile))
            {
                var defaultSettings = new AppSettings();
                defaultSettings.OutputDeviceNumbers = new List<int> { -1 };
                defaultSettings.DeviceVolumes[-1] = 1.0f;
                return defaultSettings;
            }

            try
            {
                var json = File.ReadAllText(SettingsFile);
                var settings = JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();

                // Ensure DeviceVolumes is not null
                if (settings.DeviceVolumes == null)
                {
                    settings.DeviceVolumes = new Dictionary<int, float>();
                }

                // If no devices are specified, default to device -1
                if (settings.OutputDeviceNumbers == null || settings.OutputDeviceNumbers.Count == 0)
                {
                    settings.OutputDeviceNumbers = new List<int> { -1 };
                    settings.DeviceVolumes[-1] = 1.0f;
                }
                else
                {
                    // Ensure all selected devices have a volume setting
                    foreach (var deviceNum in settings.OutputDeviceNumbers)
                    {
                        if (!settings.DeviceVolumes.ContainsKey(deviceNum))
                        {
                            settings.DeviceVolumes[deviceNum] = 1.0f;
                        }
                    }
                }

                return settings;
            }
            catch (Exception)
            {
                var defaultSettings = new AppSettings();
                defaultSettings.OutputDeviceNumbers = new List<int> { -1 };
                defaultSettings.DeviceVolumes[-1] = 1.0f;
                return defaultSettings;
            }
        }

        public void SaveSettings(AppSettings settings)
        {
            try
            {
                // Ensure the settings object is valid
                if (settings.DeviceVolumes == null)
                {
                    settings.DeviceVolumes = new Dictionary<int, float>();
                }

                var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(SettingsFile, json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to save settings: {ex.Message}");
            }
        }
    }

    public class AppSettings
    {
        public List<int> OutputDeviceNumbers { get; set; } = new List<int>();
        public Dictionary<int, float> DeviceVolumes { get; set; } = new Dictionary<int, float>();
    }
}
