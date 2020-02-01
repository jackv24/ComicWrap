using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using Acr.UserDialogs;

namespace ComicBud.ViewModels
{
    public class ComicInfoViewModel : ViewModelBase
    {
        public ComicInfoViewModel() : base()
        {
            OpenComicCommand = new Command(async () => await OpenComic());
        }

        public Command OpenComicCommand { get; }

        private async Task OpenComic()
        {
            await UserDialogs.Instance.AlertAsync("Opening comics not yet implemented!");
        }
    }
}
