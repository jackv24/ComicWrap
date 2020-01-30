using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Essentials;

using Xam.Plugin.WebView.Abstractions;

namespace ComicBud
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private void formsWebView_OnNavigationStarted(object sender, Xam.Plugin.WebView.Abstractions.Delegates.DecisionHandlerDelegate e)
        {
            MainThread.BeginInvokeOnMainThread(() => Title = "Loading...");
        }

        private void formsWebView_OnNavigationCompleted(object sender, string e)
        {
            MainThread.BeginInvokeOnMainThread(async () => Title = await ((FormsWebView)sender).InjectJavascriptAsync("(function(){return(document.title);})();"));
        }
    }
}
