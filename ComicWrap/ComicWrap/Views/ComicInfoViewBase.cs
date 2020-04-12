using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using FreshMvvm;
using AsyncAwaitBestPractices;

using ComicWrap.Systems;
using ComicWrap.Pages;
using Res = ComicWrap.Resources.AppResources;

namespace ComicWrap.Views
{
    public abstract class ComicInfoViewBase : ContentView
    {
        public ComicInfoViewBase()
        {
            var tapGesture = new TapGestureRecognizer();
            tapGesture.Command = new Command(async () => await OpenComic());
            GestureRecognizers.Add(tapGesture);
        }

        private CancellationTokenSource coverImageDownloadCancel;

        public static BindableProperty ComicProperty = BindableProperty.Create(
            propertyName: "Comic",
            returnType: typeof(ComicData),
            declaringType: typeof(ComicInfoViewBase),
            defaultBindingMode: BindingMode.OneWay,
            propertyChanged: OnComicPropertyChanged
            );

        public ComicData Comic
        {
            get { return (ComicData)GetValue(ComicProperty); }
            set { SetValue(ComicProperty, value); }
        }

        public abstract ComicPageTargetType PageTarget { get; }
        public abstract Image CoverImage { get; }

        private static void OnComicPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var comicInfoView = (ComicInfoViewBase)bindable;
            var comic = (ComicData)newValue;

            comicInfoView.ComicChanged(comic);
        }

        private void ComicChanged(ComicData newComic)
        {
            if (coverImageDownloadCancel != null)
            {
                try
                {
                    coverImageDownloadCancel.Cancel(true);
                }
                catch (OperationCanceledException)
                {
                    // Cancel silently
                }
            }

            coverImageDownloadCancel = new CancellationTokenSource();

            // Load cover image
            string coverImagePath = LocalImageService.GetImagePath(Comic.Id);
            
            // Cover image hasn't been download yet
            if (string.IsNullOrEmpty(coverImagePath))
            {
                // Download image, and then set image source
                LocalImageService.DownloadImage(new Uri("https://i.ytimg.com/vi/UNlCL2x_W8M/hqdefault.jpg"), Comic.Id, coverImageDownloadCancel.Token)
                    .ContinueWith((t) => CoverImage.Source = new FileImageSource { File = t.Result }, coverImageDownloadCancel.Token)
                    .SafeFireAndForget();
            }
            else
            {
                // Set image source immediately
                CoverImage.Source = new FileImageSource { File = coverImagePath };
            }

            OnComicChanged(newComic);
        }

        protected abstract void OnComicChanged(ComicData newComic);

        private async Task OpenComic()
        {
            // Needed to use page navigation from a non-page
            var navService = FreshIOC.Container.Resolve<IFreshNavigationService>(Constants.DefaultNavigationServiceName);

            // Manually create PageModel to inject data into the constructor
            var pageModel = new ComicDetailPageModel(PageTarget);

            // Use already created PageModel to push Page
            var page = FreshPageModelResolver.ResolvePageModel(Comic, pageModel);
            await navService.PushPage(page, null);
        }

        protected static string GetFormattedComicName(ComicData comic)
        {
            return string.IsNullOrEmpty(comic.Name) ? "$COMIC_NAME$" : comic.Name;
        }

        protected static string GetFormattedLastReadPage(ComicData comic)
        {
            return comic.LastReadPage?.Name ?? "$LAST_READ_PAGE$";
        }
    }
}