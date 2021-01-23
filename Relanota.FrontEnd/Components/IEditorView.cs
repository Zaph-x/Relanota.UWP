using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UWP.FrontEnd.Views.Interfaces
{
    public interface IEditorView
    {
        NoteEditor ParentPage { get; set; }
        bool AreTextboxesEmpty();
        void Update(string content);
        void InsertImage(string image);
        void InsertList();
        void ApplyHeaderChange(int level);
        Page GetEditor();
        string GetContent();
        void SetContent(string content);
        void FormatBold();
        void FormatItalics();
        void FormatStrikethrough();
    }
}
