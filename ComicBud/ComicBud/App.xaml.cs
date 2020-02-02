using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using FreshMvvm;

using ComicBud.Pages;

namespace ComicBud
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
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
