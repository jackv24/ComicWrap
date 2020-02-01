using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using Acr.UserDialogs;

using ComicBud.ViewModels;

namespace ComicBud.PageModels
{
    public class HomePageModel : ViewModelBase
    {
        public HomePageModel() : base()
        {
            AddComicUrlCommand = new Command(async () => await AddComicUrl());
            RefreshCommand = new Command(async () => await Refresh());
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
                OnPropertyChanged();
            }
        }

        private async Task AddComicUrl()
        {
            await UserDialogs.Instance.AlertAsync("Adding comics not yet implemented");
        }

        private async Task Refresh()
        {
            await UserDialogs.Instance.AlertAsync("Refreshing comics not yet implemented");

            IsRefreshing = false;
        }
    }
}
