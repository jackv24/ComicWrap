using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace ComicBud.PageModels
{
    public class MainPageModel : PageModelBase
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
