using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;

using FreshMvvm;

using ComicWrap.Pages;
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
            Settings.Init(this);
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
            Settings.Init(this);
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
    }
}
