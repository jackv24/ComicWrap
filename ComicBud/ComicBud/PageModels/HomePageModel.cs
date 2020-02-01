using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using Acr.UserDialogs;

namespace ComicBud.PageModels
{
    public class HomePageModel : PageModelBase
    {
        public HomePageModel() : base()
        {
            AddComicUrlCommand = new Command(async () => await AddComicUrl());
        }

        public Command AddComicUrlCommand { get; }

        private async Task AddComicUrl()
        {
            await UserDialogs.Instance.AlertAsync("This has not been implemented yet!");
        }
    }
}
