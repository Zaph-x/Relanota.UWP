using Core.Objects;
using Core.Objects.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Core.SqlHelper;
using Core.StateHandler;
using UWP.FrontEnd.Views.Interfaces;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace UWP.FrontEnd.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TagsEditor : Page, IContextInteractible<Tag>
    {
        ObservableCollection<Tag> Tags = new ObservableCollection<Tag>();

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
            using (Database context = new Database())
            {


                if (MainPage.CurrentNote == null)
                {
                    Tags.Clear();
                    foreach (Tag tag in context.Tags)
                        Tags.Add(tag);
                }
                else
                {
                    tags = context.NoteTags.Include(nt => nt.Tag)
                        .Where(nt => nt.NoteKey == MainPage.CurrentNote.Key).Select(nt => nt.Tag).ToList();
                }
            }

            TagsListView.ItemsSource = new ObservableCollection<Tag>(tags);
        }

        private void GetRelatedNotesFromTag(Tag tag)
        {
            List<Note> notes;
            using (Database context = new Database())
            {
                notes = context.Notes
                    .Include(note => note.NoteTags)
                    .ThenInclude(nt => nt.Tag)
                    .Where(note => note.NoteTags
                        .Select(nt => nt.Tag)
                        .Any(t => t.Name.ToLower() == tag.Name.ToLower()))
                    .ToList();
            }

            RelatedNotesListView.ItemsSource = new ObservableCollection<Note>(notes);

        }

        private void TagsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TagsListView.SelectedIndex >= 0)
            {
                Tag tag = TagsListView.SelectedItem as Tag;
                GetRelatedNotesFromTag(tag);
            }
            else
            {
                RelatedNotesListView.ItemsSource = new ObservableCollection<Note>();
            }
            foreach (var addedItem in e.AddedItems)
            {
                ((sender as ListView).ContainerFromItem(addedItem) as ListViewItem).PointerExited -= EntityList_PointerExited;


                if (MainPage.IsDarkTheme)
                {
                    foreach (Button button in (((sender as ListView).ContainerFromItem(addedItem) as ListViewItem).ContentTemplateRoot as Grid)
                        .Children.OfType<StackPanel>().Single().Children.OfType<Button>())
                    {

                        button.Foreground = new SolidColorBrush(Colors.Gainsboro);
                    }
                }
                else
                {
                    foreach (Button button in (((sender as ListView).ContainerFromItem(addedItem) as ListViewItem).ContentTemplateRoot as Grid)
                        .Children.OfType<StackPanel>().Single().Children.OfType<Button>())
                    {
                        button.Foreground = new SolidColorBrush(Colors.Black);
                    }
                }

            }

            foreach (var removedItem in e.RemovedItems)
            {
                ((sender as ListView).ContainerFromItem(removedItem) as ListViewItem).PointerExited += EntityList_PointerExited;
                foreach (Button button in (((sender as ListView).ContainerFromItem(removedItem) as ListViewItem).ContentTemplateRoot as Grid)
                    .Children.OfType<StackPanel>().Single().Children.OfType<Button>())
                {
                    button.Foreground = new SolidColorBrush(Colors.Transparent);
                }
            }
        }

        private void RelatedNotesGrid_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (RelatedNotesListView.SelectedIndex >= 0)
            {
                AppState.Set(State.Navigation);
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
            Save();
            FetchTags();
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            Note note = (sender as FrameworkElement).Tag as Note;
            using (Database context = new Database())
            {
                note = context.Notes.Include(n => n.NoteTags).ThenInclude(n => n.Tag).First(n => n.Key == note.Key);
                MainPage.CurrentNote = note;
                AppState.Set(State.Navigation);
                this.Frame.Navigate(typeof(NoteEditor), note);
            }
        }
        private void EntityList_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (MainPage.IsDarkTheme)
            {
                foreach (Button button in ((sender as ListViewItem).ContentTemplateRoot as Grid)
                    .Children.OfType<StackPanel>().Single().Children.OfType<Button>())
                {

                    button.Foreground = new SolidColorBrush(Colors.Gainsboro);
                }
            }
            else
            {
                foreach (Button button in ((sender as ListViewItem).ContentTemplateRoot as Grid)
                    .Children.OfType<StackPanel>().Single().Children.OfType<Button>())
                {
                    button.Foreground = new SolidColorBrush(Colors.Black);
                }
            }
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (var addedItem in e.AddedItems)
            {
                ((sender as ListView).ContainerFromItem(addedItem) as ListViewItem).PointerExited -= EntityList_PointerExited;


                if (MainPage.IsDarkTheme)
                {
                    foreach (Button button in (((sender as ListView).ContainerFromItem(addedItem) as ListViewItem).ContentTemplateRoot as Grid)
                        .Children.OfType<StackPanel>().Single().Children.OfType<Button>())
                    {

                        button.Foreground = new SolidColorBrush(Colors.Gainsboro);
                    }
                }
                else
                {
                    foreach (Button button in (((sender as ListView).ContainerFromItem(addedItem) as ListViewItem).ContentTemplateRoot as Grid)
                        .Children.OfType<StackPanel>().Single().Children.OfType<Button>())
                    {
                        button.Foreground = new SolidColorBrush(Colors.Black);
                    }
                }

            }

            foreach (var removedItem in e.RemovedItems)
            {
                ((sender as ListView).ContainerFromItem(removedItem) as ListViewItem).PointerExited += EntityList_PointerExited;
                foreach (Button button in (((sender as ListView).ContainerFromItem(removedItem) as ListViewItem).ContentTemplateRoot as Grid)
                    .Children.OfType<StackPanel>().Single().Children.OfType<Button>())
                {
                    button.Foreground = new SolidColorBrush(Colors.Transparent);
                }
            }

        }

        private void EntityList_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            foreach (Button button in ((sender as ListViewItem).ContentTemplateRoot as Grid)
                .Children.OfType<StackPanel>().Single().Children.OfType<Button>())
            {
                button.Foreground = new SolidColorBrush(Colors.Transparent);
            }
        }

        public void Save()
        {
            using (Database context = new Database())
            {
                if (MainPage.CurrentTag != null)
                {
                    MainPage.CurrentTag.Update(TagDescriptionEditBox.Text.Trim(), TagNameEditBox.Text.Trim(),
                        context);
                }
                else
                {
                    MainPage.CurrentTag = new Tag();
                    MainPage.CurrentTag.Save(TagDescriptionEditBox.Text.Trim(), TagNameEditBox.Text.Trim(),
                        context);
                }
            }

            TagNameEditBox.Text = "";
            TagDescriptionEditBox.Text = "";
            MainPage.CurrentTag = null;
        }

        public Tag Load(int key)
        {
            Tag tag = null;
            using (Database context = new Database())
            {

            }
            return tag;
        }
    }
}
