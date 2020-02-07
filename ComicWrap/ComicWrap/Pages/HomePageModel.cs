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
            AddComicUrlCommand = new Command(async () => await AddComicUrl());
            RefreshCommand = new Command(Refresh);

            Comics = new ObservableCollection<ComicData>();

            ReloadComics();
        }

        private bool _isRefreshing;

        public Command AddComicUrlCommand { get; }
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

        private async Task AddComicUrl()
        {
            var page = (PopupPage)FreshPageModelResolver.ResolvePageModel<AddComicPageModel>();
            await PopupNavigation.Instance.PushAsync(page);

            ReloadComics();
        }

        private void Refresh()
        {
            IsRefreshing = true;
            ReloadComics();
            IsRefreshing = false;
        }

        private void ReloadComics()
        {
            Comics.Clear();
            var loadedComics = ComicDatabase.Instance.GetData<ComicData>();
            foreach (var comic in loadedComics)
                Comics.Add(comic);

            RaisePropertyChanged(nameof(IsAnyComics));
        }
    }
}
