using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using Acr.UserDialogs;
using FreshMvvm;

using ComicBud.Systems;

namespace ComicBud.Pages
{
    public class ComicDetailPageModel : FreshBasePageModel
    {
        public ComicDetailPageModel()
        {
            OpenOptionsCommand = new Command(async () => await OpenOptions());
        }

        public Command OpenOptionsCommand { get; }

        public Comic Comic { get; private set; }
        public ObservableCollection<string> Chapters { get; set; }

        public override void Init(object initData)
        {
            Comic = initData as Comic;

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

        private async Task OpenOptions()
        {
            string buttonPressed = await UserDialogs.Instance.ActionSheetAsync(
                title: "Comic Options",
                cancel: "Cancel",
                destructive: "Delete"
                );

            switch (buttonPressed)
            {
                case "Cancel":
                    return;

                case "Delete":
                    // TODO: Uncomment when Comic isn't null
                    //ComicDatabase.Instance.DeleteComic(Comic);
                    break;

                default:
                    throw new NotImplementedException();
            }

            await CoreMethods.PopPageModel();
            UserDialogs.Instance.Toast("Deleted Comic: {0}");
        }
    }
}
