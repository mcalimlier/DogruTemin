using System.Windows;
using System.Windows.Documents;

namespace DogruTemin.Wpf;

public partial class DocumentPreviewWindow : Window
{
    public DocumentPreviewWindow(FixedDocument document)
    {
        InitializeComponent();
        TextSharpnessService.Apply(this);
        Viewer.Document = document;
    }
}
