using Core.Objects;
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
    public sealed partial class TagsEditor : Page
    {
        public TagsEditor()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            MainPage.Get.SetNavigationIndex(4);
            base.OnNavigatedTo(e);
            FetchTags();
            if (MainPage.CurrentTag != null)
            {
                TagNameEditBox.Text = MainPage.CurrentTag.Name;
                TagDescriptionEditBox.Text = MainPage.CurrentTag.Description ?? "";
                GetRelatedNotesFromTag(MainPage.CurrentTag);
                TagsListView.ItemsSource = new ObservableCollection<Tag>();
            }
            //TagsListView.ItemsSource = MainPage.CurrentNote?.NoteTags.Select(nt => nt.Tag) ?? new List<Tag>();
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            MainPage.CurrentTag = null;
            base.OnNavigatingFrom(e);
        }

        private void FetchTags()
        {
            List<Tag> tags = new List<Tag>();
            if (MainPage.CurrentNote == null)
            {
                tags = MainPage.context.Tags.ToList();
            }
            else
            {
                tags = MainPage.context.NoteTags.Include(nt => nt.Tag).Where(nt => nt.NoteKey == MainPage.CurrentNote.Key).Select(nt => nt.Tag).ToList();
            }
            TagsListView.ItemsSource = new ObservableCollection<Tag>(tags);
        }

        private void GetRelatedNotesFromTag(Tag tag)
        {
            List<Note> Notes = new List<Note>();
            Notes = MainPage.context.Notes
                    .Include(note => note.NoteTags)
                    .ThenInclude(nt => nt.Tag)
                    .Where(note => note.NoteTags
                        .Select(nt => nt.Tag)
                        .Any(t => t.Name.ToLower() == tag.Name.ToLower()))
                    .ToList();
            RelatedNotesListView.ItemsSource = new ObservableCollection<Note>(Notes);
        }

        private void TagsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TagsListView.SelectedIndex >= 0)
            {
                Tag tag = TagsListView.SelectedItem as Tag;
                List<Note> Notes = new List<Note>();
                Notes = MainPage.context.Notes
                    .Include(note => note.NoteTags)
                    .ThenInclude(nt => nt.Tag)
                    .Where(note => note.NoteTags
                        .Select(nt => nt.Tag)
                        .Any(t => t.Name.ToLower() == tag.Name.ToLower()))
                    .ToList();
                RelatedNotesListView.ItemsSource = new ObservableCollection<Note>(Notes);
            }
            else
            {
                RelatedNotesListView.ItemsSource = new ObservableCollection<Note>();
            }
        }

        private void RelatedNotesGrid_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (RelatedNotesListView.SelectedIndex >= 0)
            {
                MainPage.CurrentNote = RelatedNotesListView.SelectedItem as Note;
                this.Frame.Navigate(typeof(NoteEditor), RelatedNotesListView.SelectedItem as Note);
            }
        }

        private void TagEditButton_Click(object sender, RoutedEventArgs e)
        {
            Tag tag = (sender as FrameworkElement).Tag as Tag;
            MainPage.CurrentTag = tag;
            TagNameEditBox.Text = tag.Name;
            TagDescriptionEditBox.Text = tag.Description ?? "";
        }

        private void TagSaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainPage.CurrentTag != null)
            {
                MainPage.CurrentTag.Update(TagDescriptionEditBox.Text.Trim(), TagNameEditBox.Text.Trim(), MainPage.context);
            }
            else
            {
                MainPage.CurrentTag.Save(TagDescriptionEditBox.Text.Trim(), TagNameEditBox.Text.Trim(), MainPage.context);
            }
            TagNameEditBox.Text = "";
            TagDescriptionEditBox.Text = "";
            MainPage.CurrentTag = null;
            FetchTags();

        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            Note note = (sender as FrameworkElement).Tag as Note;
            note = MainPage.context.Notes.Include(n => n.NoteTags).ThenInclude(n => n.Tag).First(n => n.Key == note.Key);
            MainPage.CurrentNote = note;
            this.Frame.Navigate(typeof(NoteEditor), note);
        }
    }
}
