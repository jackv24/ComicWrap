using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using FreshMvvm;

namespace ComicWrap.Pages
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class HomePage : ContentPage
    {
        public HomePage()
        {
            InitializeComponent();

            SizeChanged += OnSizeChanged;
        }

        private void OnSizeChanged(object sender, EventArgs e)
        {
            const double baseWidth = 360;

            double width = Width;
            int columns = width > baseWidth
                ? (int)Math.Floor(width / baseWidth)
                : 1;

            libraryGridLayout.Span = columns;
        }
    }
}