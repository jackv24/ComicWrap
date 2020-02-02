using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using Acr.UserDialogs;
using FreshMvvm;

namespace ComicBud.Pages
{
    public class HomePageModel : FreshBasePageModel
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
                RaisePropertyChanged();
            }
        }

        private async Task AddComicUrl()
        {
            PromptResult result = await UserDialogs.Instance.PromptAsync(
                "Enter webcomic archive page URL",
                title: "Add Webcomic",
                okText: "Add",
                cancelText: "Cancel",
                inputType: InputType.Url);

            if (string.IsNullOrEmpty(result.Text))
                return;

            // TODO: Create new comic data

            await CoreMethods.PushPageModel<ComicDetailPageModel>();
        }

        private async Task Refresh()
        {
            await UserDialogs.Instance.AlertAsync("Refreshing comics not yet implemented");

            IsRefreshing = false;
        }
    }
}
