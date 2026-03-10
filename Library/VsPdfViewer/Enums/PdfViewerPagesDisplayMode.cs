using System;

namespace PdfiumViewer.Enums
{
    [Flags]
    public enum PdfiumViewerPagesDisplayMode
    {
        SinglePageMode = 1,
        BookMode = 2,
        ContinuousMode = 4
    }
}
