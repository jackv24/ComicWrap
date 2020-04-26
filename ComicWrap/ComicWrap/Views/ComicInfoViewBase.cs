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
using AsyncAwaitBestPractices.MVVM;

using ComicWrap.Systems;
using ComicWrap.Pages;
using Res = ComicWrap.Resources.AppResources;

namespace ComicWrap.Views
{
    public abstract class ComicInfoViewBase : ContentView
    {
        protected ComicInfoViewBase()
        {
            GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new AsyncCommand(OpenComic, _ => IsEnabled)
            });
        }

        private CancellationTokenSource coverImageDownloadCancel;
        private ComicData previousComic;

        public static BindableProperty ComicProperty = BindableProperty.Create(
            propertyName: nameof(Comic),
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
                DownloadCoverImage(newComic, coverImageDownloadCancel.Token)
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

        private async Task DownloadCoverImage(ComicData comic, CancellationToken cancelToken = default)
        {
            if (comic == null || comic.Pages.Count() == 0)
                return;

            string url = await ComicUpdater.Instance.GetComicImageUrl(comic.Pages.ElementAt(0), cancelToken);
            if (string.IsNullOrEmpty(url))
                return;

            string filePath = await LocalImageService.DownloadImage(new Uri(url), comic.Id, cancelToken);
            if (string.IsNullOrEmpty(filePath))
                return;

            CoverImage.Source = new FileImageSource { File = filePath };
        }

        protected abstract void OnComicChanged(ComicData newComic);

        private Task OpenComic()
        {
            // Needed to use page navigation from a non-page
            var navService = FreshIOC.Container.Resolve<IFreshNavigationService>(Constants.DefaultNavigationServiceName);

            // Manually create PageModel to inject data into the constructor
            var pageModel = new ComicDetailPageModel(PageTarget);

            // Use already created PageModel to push Page
            var page = FreshPageModelResolver.ResolvePageModel(Comic, pageModel);
            return navService.PushPage(page, null);
        }
    }
}