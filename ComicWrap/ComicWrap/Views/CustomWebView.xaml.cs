using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace ComicWrap.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CustomWebView : WebView
    {
        public struct WebViewNavigatingArgs
        {
            public object Sender { get; set; }
            public WebNavigatingEventArgs EventArgs { get; set; }
        }

        public struct WebViewNavigatedArgs
        {
            public object Sender { get; set; }
            public WebNavigatedEventArgs EventArgs { get; set; }
        }

        public static readonly BindableProperty NavigatingCommandProperty =
            BindableProperty.Create(nameof(NavigatingCommand), typeof(ICommand), typeof(CustomWebView));
        
        public static readonly BindableProperty NavigatedCommandProperty =
            BindableProperty.Create(nameof(NavigatedCommand), typeof(ICommand), typeof(CustomWebView));

        public CustomWebView()
        {
            InitializeComponent();

            Navigating += OnNavigating;
            Navigated += OnNavigated;
        }

        public ICommand NavigatingCommand
        {
            get { return (ICommand)GetValue(NavigatingCommandProperty); }
            set { SetValue(NavigatingCommandProperty, value); }
        }

        public ICommand NavigatedCommand
        {
            get { return (ICommand)GetValue(NavigatedCommandProperty); }
            set { SetValue(NavigatedCommandProperty, value); }
        }

        private void OnNavigating(object sender, WebNavigatingEventArgs e)
        {
            var args = new WebViewNavigatingArgs
            {
                Sender = sender,
                EventArgs = e
            };

            if (NavigatingCommand?.CanExecute(args) ?? false)
                NavigatingCommand.Execute(args);
        }

        private void OnNavigated(object sender, WebNavigatedEventArgs e)
        {
            var args = new WebViewNavigatedArgs
            {
                Sender = sender,
                EventArgs = e
            };

            if (NavigatedCommand?.CanExecute(args) ?? false)
                NavigatedCommand.Execute(args);
        }
    }
}