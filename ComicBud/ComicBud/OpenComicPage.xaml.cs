using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using AngleSharp;

namespace ComicBud
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class OpenComicPage : ContentPage
    {
        private string url;
        private string baseUrl;

        public OpenComicPage()
        {
            InitializeComponent();
        }

        private async void Entry_Completed(object sender, EventArgs e)
        {
            url = ((Entry)sender).Text;

            try
            {
                var uri = new Uri(url);
                baseUrl = uri.GetLeftPart(UriPartial.Authority);
            }
            catch (UriFormatException)
            {
                url = null;
                baseUrl = null;

                await DisplayAlert("Invalid URL", "The supplied URL was invalid.", "OK");
                return;
            }

            var config = Configuration.Default.WithDefaultLoader();
            var context = BrowsingContext.New(config);
            var document = await context.OpenAsync(url);

            var selectors = document.GetElementsByName("comic");
            if (selectors.Length == 0)
            {
                await DisplayAlert("Error", "Couldn't find archive list", "OK");
                return;
            }

            var options = selectors[0].Children;
            listPages.ItemsSource = options
                .Select(option => option.GetAttribute("value"))
                .ToArray();
        }

        private async void listPages_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            var urlEnd = (string)e.SelectedItem;

            string url = $"{baseUrl}/{urlEnd}";

            try
            {
                var uri = new Uri(url);
            }
            catch (UriFormatException)
            {
                await DisplayAlert("Invalid URL", "The clicked URL was invalid.", "OK");
                return;
            }

            var comicPage = new ComicWebPage(url);
            await Navigation.PushAsync(comicPage);
        }
    }
}