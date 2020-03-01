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

namespace ComicWrap.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ComicInfoView : ContentView
    {
        public ComicInfoView()
        {
            InitializeComponent();

            var tapGesture = new TapGestureRecognizer();
            tapGesture.Command = new Command(async () => await OpenComic());
            GestureRecognizers.Add(tapGesture);
        }

        public static BindableProperty ComicProperty = BindableProperty.Create(
            propertyName: "Comic",
            returnType: typeof(ComicData),
            declaringType: typeof(ComicInfoView),
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
            var comicInfoView = (ComicInfoView)bindable;
            var comic = (ComicData)newValue;

            comicInfoView.labelComicName.Text = comic.Name;
            comicInfoView.labelLastComicPageName.Text = comic.LastReadPage?.Name ?? string.Empty;
            comicInfoView.progressBarReadProgress.Progress = comic.ReadProgress;
            comicInfoView.labelLastUpdated.Text = $"Updated {comic.DaysSinceLastUpdated} days ago";
        }

        private async Task OpenComic()
        {
            var navService = FreshIOC.Container.Resolve<IFreshNavigationService>(Constants.DefaultNavigationServiceName);
            var page = FreshPageModelResolver.ResolvePageModel<ComicDetailPageModel>(Comic);
            await navService.PushPage(page, null);
        }
    }
}