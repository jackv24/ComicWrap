using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using ComicBud.ViewModels;

namespace ComicBud.PageModels
{
    public class MainPageModel : ViewModelBase
    {
        public MainPageModel() : base()
        {
            MasterNavigationItems = new ObservableCollection<string>
            {
                "Library",
                "Discover",
            };
        }

        public ObservableCollection<string> MasterNavigationItems { get; }
    }
}
