using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;

namespace ComicWrap
{
    public static class Extensions
    {
        public static string ToRelative(this Uri uri)
        {
            return uri.IsAbsoluteUri ? uri.PathAndQuery : uri.OriginalString;
        }

        public static void CancelAndDispose(this CancellationTokenSource tokenSource)
        {
            tokenSource.Cancel();
            tokenSource.Dispose();
        }

        public static void Sort<T>(this ObservableCollection<T> collection, Comparison<T> comparison)
        {
            var sortableList = new List<T>(collection);
            sortableList.Sort(comparison);

            for (int i = 0; i < sortableList.Count; i++)
            {
                collection.Move(collection.IndexOf(sortableList[i]), i);
            }
        }

        public static void MatchList<T>(this ObservableCollection<T> observableCollection, IList<T> matchList)
        {
            // Add new items to list (will be sorted after)
            foreach (T item in matchList)
            {
                if (!observableCollection.Contains(item))
                    observableCollection.Add(item);
            }

            // Remove items no longer in list, loop backwards since collection will be modified
            for (int i = observableCollection.Count - 1; i >= 0; i--)
            {
                T item = observableCollection[i];
                if (!matchList.Contains(item))
                    observableCollection.RemoveAt(i);
            }

            // Match collection positions (should be same length after add/remove above)
            for (int i = 0; i < matchList.Count; i++)
                observableCollection.Move(observableCollection.IndexOf(matchList[i]), i);
        }
    }
}
