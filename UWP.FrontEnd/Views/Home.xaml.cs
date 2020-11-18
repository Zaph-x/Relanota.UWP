using Core.Objects;
using Core.SqlHelper;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace UWP.FrontEnd.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Home : Page
    {
        public Home()
        {
            this.InitializeComponent();
            MainPage.context.Notes.Load();
            MainPage.context.Tags.Load();
            ObservableCollection<Note> notesCollection = MainPage.context.Notes.Local.ToObservableCollection();
            NotesListView.ItemsSource = notesCollection.OrderBy(note => note.Name);
            ObservableCollection<Tag> tagsCollection = MainPage.context.Tags.Local.ToObservableCollection();
            TagsListView.ItemsSource = tagsCollection.OrderBy(tag => tag.Name);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            MainPage.context.Notes.Load();
            MainPage.context.Tags.Load();
            ObservableCollection<Note> notesCollection = MainPage.context.Notes.Local.ToObservableCollection(); ;
            NotesListView.ItemsSource = notesCollection;
            ObservableCollection < Tag > tagsCollection = MainPage.context.Tags.Local.ToObservableCollection();
            TagsListView.ItemsSource = tagsCollection;
            MainPage.Get.SetDividerNoteName("No Note Selected");
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            Note note = (sender as FrameworkElement).Tag as Note;
            note = MainPage.context.Notes.Include(n => n.NoteTags).ThenInclude(n => n.Tag).First(n => n.Key == note.Key);
            MainPage.CurrentNote = note;
            this.Frame.Navigate(typeof(NoteEditor), note);
        }

        private void NotesListView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (NotesListView.SelectedIndex >= 0)
            {
                Note note = NotesListView.SelectedItem as Note;
                note = MainPage.context.Notes.Include(n => n.NoteTags).ThenInclude(n => n.Tag).First(n => n.Key == note.Key);
                MainPage.CurrentNote = note;
                this.Frame.Navigate(typeof(NoteEditor), note);
            }
        }

        private void PreviewButton_Click(object sender, RoutedEventArgs e)
        {
            NotesListView.SelectedIndex = NotesListView.Items.IndexOf((sender as FrameworkElement).Tag as Note);
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            Note note = (sender as FrameworkElement).Tag as Note;
            ContentDialog deleteFileDialog = new ContentDialog
            {
                Title = "Delete note permanently?",
                Content = "If you delete this note, you won't be able to recover it. Do you want to delete it?",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel"
            };

            ContentDialogResult result = await deleteFileDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                note.Delete(MainPage.context, MainPage.Get.ShowMessageBox);
                NotesListView.Items.Remove(note);
            }
        }

        private void EditTagButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Tag tag = (sender as FrameworkElement).Tag as Tag;
                tag = MainPage.context.Tags.Include(t => t.NoteTags).ThenInclude(nt => nt.Note).First(t => t.Key == tag.Key);
                MainPage.CurrentTag = tag;
                this.Frame.Navigate(typeof(TagsEditor), tag);
            }
            catch (Exception ex)
            {
                MainPage.Get.ShowMessageBox(ex.Message, "");
            }
        }

        private async void DeleteTagButton_Click(object sender, RoutedEventArgs e)
        {
            Tag tag = (sender as FrameworkElement).Tag as Tag;
            ContentDialog deleteFileDialog = new ContentDialog
            {
                Title = "Delete tag permanently?",
                Content = "If you delete this tag, you will remove any relations to the tag. Tou won't be able to recover it. Do you want to delete it?",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel"
            };

            ContentDialogResult result = await deleteFileDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                tag.Delete(MainPage.context, MainPage.Get.ShowMessageBox);
                TagsListView.Items.Remove(tag);
            }
        }
    }
}
