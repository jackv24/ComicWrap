using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using FreshMvvm;
using Rg.Plugins.Popup.Services;
using Rg.Plugins.Popup.Pages;

using ComicWrap.Systems;

namespace ComicWrap.Pages
{
    public class HomePageModel : FreshBasePageModel
    {
        public HomePageModel() : base()
        {
            AddComicCommand = new Command(async () => await OpenAddComicPopup());
            RefreshCommand = new Command(Refresh);

            Comics = new ObservableCollection<ComicData>();

            Refresh();
        }

        private bool _isRefreshing;
        private bool isRefreshTaskRunning;

        public Command AddComicCommand { get; }
        public Command RefreshCommand { get; }

        public bool IsRefreshing
        {
            get { return _isRefreshing; }
            set
            {
                _isRefreshing = value;
                RaisePropertyChanged();
            }
        }

        public ObservableCollection<ComicData> Comics { get; }

        public bool IsAnyComics
        {
            get { return Comics.Count > 0; }
        }

        protected override void ViewIsAppearing(object sender, EventArgs e)
        {
            Refresh();
        }

        private async Task OpenAddComicPopup()
        {
            var page = (PopupPage)FreshPageModelResolver.ResolvePageModel<AddComicPageModel>();
            await PopupNavigation.Instance.PushAsync(page);
        }

        private async void Refresh()
        {
            // Method is "fire and forget" so make sure it's not already running
            if (isRefreshTaskRunning)
                return;

            isRefreshTaskRunning = true;
            IsRefreshing = true;

            // Load comics from local database
            var loadedComics = await ComicDatabase.Instance.GetComics();

            // Update UI
            Comics.Clear();
            foreach (var comic in loadedComics)
                Comics.Add(comic);

            // Update comics from internet after loading from database so UI is filled ASAP
            foreach (var comic in loadedComics)
                await ComicUpdater.UpdateComic(comic);

            RaisePropertyChanged(nameof(IsAnyComics));

            isRefreshTaskRunning = false;
            IsRefreshing = false;

            // TODO: Get comic updates in the background
        }
    }
}
