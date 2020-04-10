using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;

using FreshMvvm;

using ComicWrap.Pages;
using ComicWrap.Themes;
using ComicWrap.Resources;
using ComicWrap.Systems;
using Secrets = ComicWrap.Helpers.Secrets;

namespace ComicWrap
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            var page = FreshPageModelResolver.ResolvePageModel<HomePageModel>();
            var navContainer = new FreshNavigationContainer(page);
            MainPage = navContainer;
        }

        protected override void OnStart()
        {
            AppCenterStart();
            UpdateTheme();
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
            UpdateTheme();
        }

        private void AppCenterStart()
        {
            string appSecretiOS = Secrets.AppSecret_iOS;
            string appSecretAndroid = Secrets.AppSecret_Android;

            // Only start app center for platforms that have their secret defined
            string appSecretString;
            if (!string.IsNullOrEmpty(appSecretiOS) && !string.IsNullOrEmpty(appSecretAndroid))
                appSecretString = $"ios={appSecretiOS};android={appSecretAndroid}";
            else if (!string.IsNullOrEmpty(appSecretiOS))
                appSecretString = $"ios={appSecretiOS}";
            else if (!string.IsNullOrEmpty(appSecretAndroid))
                appSecretString = $"android={appSecretAndroid}";
            else
                appSecretString = null;

            if (appSecretString != null)
                AppCenter.Start(appSecretString, typeof(Analytics), typeof(Crashes));
        }

        private void UpdateTheme()
        {
            var environment = DependencyService.Get<IEnvironment>();
            var theme = environment.GetOperatingSystemTheme();

            var mergedDictionaries = Current.Resources.MergedDictionaries;
            if (mergedDictionaries != null)
            {
                mergedDictionaries.Clear();

                ResourceDictionary themeDict;
                switch (theme)
                {
                    case Theme.Dark:
                        themeDict = new DarkTheme();
                        break;

                    case Theme.Light:
                    default:
                        themeDict = new LightTheme();
                    break;
                }

                var styleDict = new ElementStyles();

                mergedDictionaries.Add(themeDict);

                // Need to merge theme into style dict before adding as style dict depends on theme
                styleDict.MergedDictionaries.Add(themeDict);
                mergedDictionaries.Add(styleDict);

                // Just add font dict since it doesn't depend on any others
                mergedDictionaries.Add(new AppFonts());
            }

            environment.ApplyTheme(theme);
        }
    }
}
