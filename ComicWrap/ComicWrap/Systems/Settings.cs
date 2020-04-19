using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Xamarin.Essentials;

using ComicWrap.Themes;
using ComicWrap.Resources;
using ComicWrap.Systems;

namespace ComicWrap.Systems
{
    public static class Settings
    {
        public enum ThemeSetting
        {
            System,
            Light,
            Dark
        }

        public const string THEME_SETTING_KEY = "Setting_Theme";

        private static Application currentApplication;

        public static ThemeSetting Theme
        {
            get { return (ThemeSetting)Preferences.Get(THEME_SETTING_KEY, (int)ThemeSetting.System); }
            set
            {
                Preferences.Set(THEME_SETTING_KEY, (int)value);
                ApplyTheme(value);
            }
        }

        public static void Init(Application application)
        {
            currentApplication = application;

            ApplyTheme(Theme);
        }

        private static void ApplyTheme(ThemeSetting theme)
        {
            var environment = DependencyService.Get<IEnvironment>();
            SystemTheme systemTheme = environment.GetOperatingSystemTheme();

            var mergedDictionaries = currentApplication.Resources.MergedDictionaries;
            if (mergedDictionaries != null)
            {
                mergedDictionaries.Clear();

                ResourceDictionary themeDict;

                switch (theme)
                {
                    case ThemeSetting.System:
                        if (systemTheme == SystemTheme.Dark)
                            themeDict = new DarkTheme();
                        else
                            themeDict = new LightTheme();
                        break;

                    case ThemeSetting.Light:
                        themeDict = new LightTheme();
                        systemTheme = SystemTheme.Light;
                        break;

                    case ThemeSetting.Dark:
                        themeDict = new DarkTheme();
                        systemTheme = SystemTheme.Dark;
                        break;

                    default:
                        throw new NotImplementedException();
                }

                var styleDict = new ElementStyles();

                mergedDictionaries.Add(themeDict);

                // Need to merge theme into style dict before adding as style dict depends on theme
                styleDict.MergedDictionaries.Add(themeDict);
                mergedDictionaries.Add(styleDict);

                // Just add font dict since it doesn't depend on any others
                mergedDictionaries.Add(new AppFonts());
            }

            // Set system theme for platform-specific theming.
            // TODO: Fix android status bar colour only obeying system theme
            environment.ApplyTheme(systemTheme);
        }
    }
}
