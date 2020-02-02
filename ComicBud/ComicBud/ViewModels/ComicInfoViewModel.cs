using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using Acr.UserDialogs;
using FreshMvvm;

using ComicBud.Pages;

namespace ComicBud.ViewModels
{
    public class ComicInfoViewModel
    {
        public ComicInfoViewModel()
        {
            OpenComicCommand = new Command(async () => await OpenComic());
        }

        public Command OpenComicCommand { get; }

        private async Task OpenComic()
        {
            var navService = FreshIOC.Container.Resolve<IFreshNavigationService>(Constants.DefaultNavigationServiceName);
            var page = FreshPageModelResolver.ResolvePageModel<ComicDetailPageModel>();
            await navService.PushPage(page, null);
        }
    }
}
