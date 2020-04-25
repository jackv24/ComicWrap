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
            tapGesture.Command = new Command(async () => await OpenComic(), () => IsEnabled);
            GestureRecognizers.Add(tapGesture);
        }

        private CancellationTokenSource coverImageDownloadCancel;
        private ComicData previousComic;

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
            if (newComic != previousComic)
            {
                if (previousComic != null)
                    previousComic.Updated -= RefreshComic;

                if (newComic != null)
                    newComic.Updated += RefreshComic;

                previousComic = newComic;
            }

            if (coverImageDownloadCancel != null)
                coverImageDownloadCancel.Cancel();

            if (newComic == null)
            {
                CoverImage.Source = null;
                return;
            }

            // Load cover image
            string coverImagePath = LocalImageService.GetImagePath(newComic.Id);

            // Cover image hasn't been download yet
            if (string.IsNullOrEmpty(coverImagePath))
            {
                coverImageDownloadCancel = new CancellationTokenSource();

                // Download image, and then set image source
                DownloadCoverImage(coverImageDownloadCancel.Token)
                    .SafeFireAndForget();
            }
            else
            {
                // Set image source immediately
                CoverImage.Source = new FileImageSource { File = coverImagePath };
            }

            OnComicChanged(newComic);
        }

        private void RefreshComic()
        {
            ComicChanged(Comic);
        }

        private async Task DownloadCoverImage(CancellationToken cancelToken = default)
        {
            if (Comic == null || Comic.Pages.Count() == 0)
                return;

            string url = await ComicUpdater.Instance.GetComicImageUrl(Comic.Pages.ElementAt(0), cancelToken);
            if (string.IsNullOrEmpty(url))
                return;

            string filePath = await LocalImageService.DownloadImage(new Uri(url), Comic.Id, cancelToken);
            if (string.IsNullOrEmpty(filePath))
                return;

            CoverImage.Source = new FileImageSource { File = filePath };
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
    }
}