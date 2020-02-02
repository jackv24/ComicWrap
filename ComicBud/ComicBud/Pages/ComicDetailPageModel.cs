using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using Acr.UserDialogs;
using FreshMvvm;

namespace ComicBud.Pages
{
    public class ComicDetailPageModel : FreshBasePageModel
    {
        public override void Init(object initData)
        {
            // TODO: Actually pass in newly created comic data object

            Chapters = new ObservableCollection<string>
            {
                "Chapter 0",
                "Chapter 0",
                "Chapter 0",
                "Chapter 0",
                "Chapter 0",
                "Chapter 0",
                "Chapter 0",
                "Chapter 0",
                "Chapter 0",
                "Chapter 0",
                "Chapter 0",
                "Chapter 0",
                "Chapter 0",
                "Chapter 0",
                "Chapter 0",
                "Chapter 0",
                "Chapter 0",
                "Chapter 0",
                "Chapter 0",
                "Chapter 0",
                "Chapter 0",
                "Chapter 0",
                "Chapter 0",
                "Chapter 0",
                "Chapter 0",
                "Chapter 0",
                "Chapter 0",
                "Chapter 0",
                "Chapter 0",
                "Chapter 0",
                "Chapter 0",
                "Chapter 0",
                "Chapter 0",
                "Chapter 0",
                "Chapter 0",
                "Chapter 0",
            };
        }

        public ObservableCollection<string> Chapters { get; set; }
    }
}
