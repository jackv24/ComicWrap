using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Essentials;

namespace ComicBud
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class ComicWebPage : ContentPage
    {
        private string comicUrl;

        public ComicWebPage(string url)
        {
            InitializeComponent();

            comicUrl = url;
            webView.Source = new UrlWebViewSource { Url = url };
        }

        private async void WebView_Navigating(object sender, WebNavigatingEventArgs e)
        {
            string comicHostName = GetMainHostName(comicUrl);
            string nextHostName = GetMainHostName(e.Url);

            if (nextHostName != comicHostName)
            {
                e.Cancel = true;
                await Browser.OpenAsync(e.Url);
                return;
            }

            labelLoading.IsVisible = true;

            Title = await ((WebView)sender).EvaluateJavaScriptAsync("document.title");
        }

        private void WebView_Navigated(object sender, WebNavigatedEventArgs e)
        {
            labelLoading.IsVisible = false;
        }

        private string GetMainHostName(string url)
        {
            var uri = new Uri(url);
            string hostName = uri.Host;

            string[] splitHostName = hostName.Split('.');

            if (splitHostName.Length < 2)
                return hostName;

            string secondLevelHostName = splitHostName[splitHostName.Length - 2] + "." + splitHostName[splitHostName.Length - 1];
            return secondLevelHostName;
        }
    }
}
