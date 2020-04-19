using System;
using System.Linq;
using System.Threading.Tasks;
using UIKit;
using Xamarin.Forms;
using ComicWrap.Systems;
using ComicWrap.iOS.Systems;

[assembly: Dependency(typeof(Environment_iOS))]
namespace ComicWrap.iOS.Systems
{
    // Original Source: https://codetraveler.io/2019/09/11/check-for-dark-mode-in-xamarin-forms/

    public class Environment_iOS : IEnvironment
    {
        public SystemTheme GetOperatingSystemTheme()
        {
            // `TraitCollection.UserInterfaceStyle` was introduced in iOS 12.0
            if (UIDevice.CurrentDevice.CheckSystemVersion(12, 0))
            {
                var currentViewController = GetVisibleViewController();

                var uiStyle = currentViewController.TraitCollection.UserInterfaceStyle;
                switch (uiStyle)
                {
                    case UIUserInterfaceStyle.Light:
                        return SystemTheme.Light;

                    case UIUserInterfaceStyle.Dark:
                        return SystemTheme.Dark;

                    default:
                        throw new NotSupportedException($"UIUserInterfaceStyle {uiStyle} not supported");
                }
            }
            else
            {
                return SystemTheme.Light;
            }
        }

        public Task<SystemTheme> GetOperatingSystemThemeAsync()
            => Device.InvokeOnMainThreadAsync(GetOperatingSystemTheme);

        public void ApplyTheme(SystemTheme theme)
        {

        }

        private static UIViewController GetVisibleViewController()
        {
            UIViewController viewController = null;

            var window = UIApplication.SharedApplication.KeyWindow;

            if (viewController == null)
            {
                window = UIApplication.SharedApplication
                    .Windows
                    .OrderByDescending(w => w.WindowLevel)
                    .FirstOrDefault(w => w.RootViewController != null && w.WindowLevel == UIWindowLevel.Normal);

                viewController = window?.RootViewController
                    ?? throw new InvalidOperationException("Could not find current view controller.");
            }

            while (viewController.PresentedViewController != null)
                viewController = viewController.PresentedViewController;

            return viewController;
        }
    }
}