using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace ComicWrap.Resources
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AppFonts : ResourceDictionary
    {
        public AppFonts()
        {
            InitializeComponent();
        }
    }
}