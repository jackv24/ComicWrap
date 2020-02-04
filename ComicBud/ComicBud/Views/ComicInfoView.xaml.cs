using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using FreshMvvm;

using ComicBud.Systems;
using ComicBud.Pages;

namespace ComicBud.Views
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
            returnType: typeof(Comic),
            declaringType: typeof(ComicInfoView),
            defaultBindingMode: BindingMode.OneWay,
            propertyChanged: OnComicNamePropertyChanged
            );

        public Comic Comic
        {
            get { return (Comic)GetValue(ComicProperty); }
            set { SetValue(ComicProperty, value); }
        }

        private static void OnComicNamePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var comicInfoView = (ComicInfoView)bindable;
            var comic = (Comic)newValue;

            comicInfoView.labelComicName.Text = comic.Name;
        }

        private async Task OpenComic()
        {
            var navService = FreshIOC.Container.Resolve<IFreshNavigationService>(Constants.DefaultNavigationServiceName);
            var page = FreshPageModelResolver.ResolvePageModel<ComicDetailPageModel>(Comic);
            await navService.PushPage(page, null);
        }
    }
}