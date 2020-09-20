using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Core.Objects;
using Core.SqlHelper;
using Microsoft.EntityFrameworkCore;

namespace FrontEnd
{
    /// <summary>
    /// Interaction logic for RelatedItemsWindow.xaml
    /// </summary>
    public partial class RelatedItemsWindow : Window
    {
        public Note SelectedNote { get; set; }
        private List<Note> Notes { get; set; }
        MainWindow mainWindow { get; set; }
        public RelatedItemsWindow(Tag tag, Database context, MainWindow mainWindow) {
            this.Owner = mainWindow;
            this.mainWindow = mainWindow;
            Notes = context.Notes
                .Include(note => note.NoteTags)
                .ThenInclude(nt => nt.Tag)
                .Where(note => note.NoteTags
                    .Select(nt => nt.Tag)
                    .Any(t => t.Name.ToLower() == tag.Name.ToLower()))
                .ToList();
            InitializeComponent();
            this.Title = $"Related notes";
            this.InfoText.Text = $"Notes related to '{tag.Name}' 📜";
            this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            NoteList.ItemsSource = new ObservableCollection<Note>(Notes);
            CollectionViewSource.GetDefaultView(NoteList.ItemsSource).Refresh();
        }
            
        private void NoteList_DoubleClickNote(object sender, MouseButtonEventArgs e)
        {
            if (NoteList.SelectedIndex == -1)
            {
                return;
            }

            SelectedNote = NoteList.SelectedItem as Note;
            mainWindow.LoadNote(SelectedNote);
            mainWindow.NoteList.SelectedIndex = mainWindow.NoteList.Items.IndexOf(SelectedNote);
            mainWindow.TagList.SelectedIndex = -1;
            Close();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (NoteList.SelectedIndex == -1)
            {
                return;
            }

            SelectedNote = NoteList.SelectedItem as Note;
            mainWindow.LoadNote(SelectedNote);
            mainWindow.NoteList.SelectedIndex = mainWindow.NoteList.Items.IndexOf(SelectedNote);
            mainWindow.TagList.SelectedIndex = -1;
            Close();
        }
    }
}
