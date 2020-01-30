using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace ComicBud
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class OpenComicPage : ContentPage
    {
        public OpenComicPage()
        {
            InitializeComponent();
        }

        private async void Entry_Completed(object sender, EventArgs e)
        {
            string url = ((Entry)sender).Text;

            try
            {
                var uri = new Uri(url);
            }
            catch (UriFormatException)
            {
                await DisplayAlert("Invalid URL", "The supplied url was invalid.", "OK");
                return;
            }

            var comicPage = new ComicWebPage(url);
            await Navigation.PushAsync(comicPage);
        }   
    }
}