using System.Collections.ObjectModel;
using System.ComponentModel;

namespace PdfiumViewer.Core
{
    public class PdfBookmark
    {
        public string Title { get; set; }
        public int PageIndex { get; set; }

        public PdfBookmarkCollection Children { get; }

        public PdfBookmark()
        {
            Children = new PdfBookmarkCollection();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public override string ToString()
        {
            return Title;
        }
    }

    public class PdfBookmarkCollection : ObservableCollection<PdfBookmark>
    {
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
