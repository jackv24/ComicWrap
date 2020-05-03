using System;
using Xamarin.UITest;
using Xamarin.UITest.Queries;

namespace ComicWrap.UITest
{
    public class AppInitializer
    {
        public static IApp StartApp(Platform platform)
        {
            if (platform == Platform.Android)
            {
                return ConfigureApp.Android.InstalledApp("com.jackvine.comicwrap").StartApp();
            }

            return ConfigureApp.iOS.InstalledApp("com.jackvine.ComicWrap").StartApp();
        }
    }
}