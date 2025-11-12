using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace GlobalAudio.Services
{
    public static class ThemeManager
    {
        private const string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        private const string RegistryValueName = "AppsUseLightTheme";

        public static bool IsSystemDarkMode()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath);
                var value = key?.GetValue(RegistryValueName);
                if (value is int intValue)
                {
                    return intValue == 0; // 0 = Dark, 1 = Light
                }
            }
            catch
            {
                // If we can't read the registry, default to light mode
            }
            return false;
        }

        public static void ApplyTheme(Window window)
        {
            bool isDark = IsSystemDarkMode();

            if (isDark)
            {
                ApplyDarkTheme(window);
            }
            else
            {
                ApplyLightTheme(window);
            }
        }

        private static void ApplyDarkTheme(Window window)
        {
            window.Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));

            var resources = window.Resources;
            resources["BackgroundColor"] = new SolidColorBrush(Color.FromRgb(30, 30, 30));
            resources["SurfaceColor"] = new SolidColorBrush(Color.FromRgb(45, 45, 48));
            resources["BorderColor"] = new SolidColorBrush(Color.FromRgb(60, 60, 60));
            resources["TextColor"] = new SolidColorBrush(Color.FromRgb(240, 240, 240));
            resources["SecondaryTextColor"] = new SolidColorBrush(Color.FromRgb(180, 180, 180));
            resources["ItemBackgroundColor"] = new SolidColorBrush(Color.FromRgb(40, 40, 43));
        }

        private static void ApplyLightTheme(Window window)
        {
            window.Background = new SolidColorBrush(Color.FromRgb(245, 245, 245));

            var resources = window.Resources;
            resources["BackgroundColor"] = new SolidColorBrush(Color.FromRgb(245, 245, 245));
            resources["SurfaceColor"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            resources["BorderColor"] = new SolidColorBrush(Color.FromRgb(224, 224, 224));
            resources["TextColor"] = new SolidColorBrush(Color.FromRgb(0, 0, 0));
            resources["SecondaryTextColor"] = new SolidColorBrush(Color.FromRgb(136, 136, 136));
            resources["ItemBackgroundColor"] = new SolidColorBrush(Color.FromRgb(250, 250, 250));
        }
    }
}
