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
using ComicBud.Systems;

namespace ComicBud.Views
{
    public class ComicInfoViewModel : INotifyPropertyChanged
    {
        public ComicInfoViewModel()
        {
            OpenComicCommand = new Command(async () => await OpenComic());
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private Comic _comic;

        public Comic Comic
        {
            get { return _comic; }
            set
            {
                _comic = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(ComicName));
            }
        }

        public string ComicName
        {
            get { return Comic != null ? Comic.Name : "TODO: Comics"; }
        }

        public Command OpenComicCommand { get; }

        private async Task OpenComic()
        {
            var navService = FreshIOC.Container.Resolve<IFreshNavigationService>(Constants.DefaultNavigationServiceName);
            var page = FreshPageModelResolver.ResolvePageModel<ComicDetailPageModel>();
            await navService.PushPage(page, null);
        }

        private void OnPropertyChanged([CallerMemberName]string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
