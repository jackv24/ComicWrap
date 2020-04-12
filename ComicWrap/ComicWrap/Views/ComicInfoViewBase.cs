using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using FreshMvvm;

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

        private static void OnComicPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var comicInfoView = (ComicInfoViewBase)bindable;
            var comic = (ComicData)newValue;

            comicInfoView.OnComicChanged(comic);
        }

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

        protected abstract void OnComicChanged(ComicData newComic);

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