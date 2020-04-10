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

        private static void OnComicPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var comicInfoView = (ComicInfoViewBase)bindable;
            var comic = (ComicData)newValue;

            comicInfoView.OnComicChanged(comic);
        }

        private async Task OpenComic()
        {
            var navService = FreshIOC.Container.Resolve<IFreshNavigationService>(Constants.DefaultNavigationServiceName);
            var page = FreshPageModelResolver.ResolvePageModel<ComicDetailPageModel>(Comic);
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