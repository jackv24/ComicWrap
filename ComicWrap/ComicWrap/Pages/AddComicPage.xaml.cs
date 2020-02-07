using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using Rg.Plugins.Popup.Pages;

namespace ComicWrap.Pages
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AddComicPage : PopupPage
    {
        public AddComicPage()
        {
            InitializeComponent();
        }
    }
}