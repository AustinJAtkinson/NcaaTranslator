using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace NcaaTranslator.Wpf
{
    public static class ThemeManager
    {
        public static void ApplyLightTheme()
        {
            Application.Current.Resources["PrimaryBrush"] = new SolidColorBrush(Color.FromRgb(0, 120, 212));
            Application.Current.Resources["SecondaryBrush"] = new SolidColorBrush(Color.FromRgb(16, 110, 190));
            Application.Current.Resources["AccentBrush"] = new SolidColorBrush(Color.FromRgb(0, 90, 158));
            Application.Current.Resources["SelectedBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0, 120, 212));
            Application.Current.Resources["BackgroundBrush"] = new SolidColorBrush(Colors.White);
            Application.Current.Resources["SurfaceBrush"] = new SolidColorBrush(Colors.White);
            Application.Current.Resources["TextPrimaryBrush"] = new SolidColorBrush(Colors.Black);
            Application.Current.Resources["TextSecondaryBrush"] = new SolidColorBrush(Color.FromRgb(51, 51, 51));
            Application.Current.Resources["BorderBrush"] = new SolidColorBrush(Color.FromRgb(225, 225, 225));
            Application.Current.Resources["HoverBrush"] = new SolidColorBrush(Color.FromRgb(229, 243, 255));
            Application.Current.Resources["PressedBrush"] = new SolidColorBrush(Color.FromRgb(199, 228, 247));
            Application.Current.Resources["AlternatingRowBrush"] = new SolidColorBrush(Color.FromRgb(248, 248, 248));
            Application.Current.Resources[SystemColors.InactiveSelectionHighlightBrushKey] = new SolidColorBrush(Color.FromRgb(0, 120, 212));
        }

        public static void ApplyDarkTheme()
        {
            Application.Current.Resources["PrimaryBrush"] = new SolidColorBrush(Color.FromRgb(0, 120, 212));
            Application.Current.Resources["SecondaryBrush"] = new SolidColorBrush(Color.FromRgb(16, 110, 190));
            Application.Current.Resources["AccentBrush"] = new SolidColorBrush(Color.FromRgb(0, 90, 158));
            Application.Current.Resources["SelectedBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(80, 80, 80));
            Application.Current.Resources["BackgroundBrush"] = new SolidColorBrush(Color.FromRgb(30, 30, 30));
            Application.Current.Resources["SurfaceBrush"] = new SolidColorBrush(Color.FromRgb(45, 45, 45));
            Application.Current.Resources["TextPrimaryBrush"] = new SolidColorBrush(Colors.White);
            Application.Current.Resources["TextSecondaryBrush"] = new SolidColorBrush(Color.FromRgb(200, 200, 200));
            Application.Current.Resources["BorderBrush"] = new SolidColorBrush(Color.FromRgb(100, 100, 100));
            Application.Current.Resources["HoverBrush"] = new SolidColorBrush(Color.FromRgb(60, 60, 60));
            Application.Current.Resources["PressedBrush"] = new SolidColorBrush(Color.FromRgb(80, 80, 80));
            Application.Current.Resources["AlternatingRowBrush"] = new SolidColorBrush(Color.FromRgb(74, 74, 74));
            Application.Current.Resources[SystemColors.InactiveSelectionHighlightBrushKey] = new SolidColorBrush(Color.FromRgb(80, 80, 80));
        }

        public static bool IsLightTheme()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    if (key != null)
                    {
                        var value = key.GetValue("AppsUseLightTheme");
                        if (value is int intValue)
                        {
                            return intValue == 1;
                        }
                    }
                }
            }
            catch
            {
                // If registry access fails, default to light theme
            }
            return true;
        }

        public static void ApplySystemTheme()
        {
            if (IsLightTheme())
            {
                ApplyLightTheme();
            }
            else
            {
                ApplyDarkTheme();
            }
        }
    }
}