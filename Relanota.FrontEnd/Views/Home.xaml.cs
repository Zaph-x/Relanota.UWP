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

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace UWP.FrontEnd.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Home : Page
    {
        ObservableCollection<Note> NotesCollection { get; set; }
        ObservableCollection<Tag> TagsCollection { get; set; }

        public Home()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            using (Database context = new Database())
            {
                NotesCollection = new ObservableCollection<Note>(context.Notes.ToList().OrderBy(note => note.Name));
                TagsCollection = new ObservableCollection<Tag>(context.Tags.ToList().OrderBy(tag => tag.Name));
            }
            MainPage.Get.SetDividerNoteName("No Note Selected");
        }


        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            Note note = (sender as FrameworkElement).Tag as Note;
            using (Database context = new Database())
            {
                note = context.Notes.Include(n => n.NoteTags).ThenInclude(n => n.Tag).First(n => n.Key == note.Key);
            }
            NoteEditor.Get.State = NoteEditorState.ListNavigation;
            MainPage.CurrentNote = note;
            this.Frame.Navigate(typeof(NoteEditor), note);
        }

        private void NotesListView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            using (Database context = new Database())
            {
                if (NotesListView.SelectedIndex >= 0)
                {
                    NotesListView.SelectedItem = context.Notes.Include(n => n.NoteTags)
                        .ThenInclude(n => n.Tag)
                        .First(n => n.Key == (NotesListView.SelectedItem as Note).Key);
                    MainPage.CurrentNote = (NotesListView.SelectedItem as Note);
                    NoteEditor.Get.State = NoteEditorState.ListNavigation;
                    this.Frame.Navigate(typeof(NoteEditor), (NotesListView.SelectedItem as Note));
                }
            }

        }

        private void PreviewButton_Click(object sender, RoutedEventArgs e)
        {
            NotesListView.SelectedIndex = NotesListView.Items.IndexOf((sender as FrameworkElement).Tag as Note);
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
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
                using (Database context = new Database())
                {
                    NotesCollection.Remove((sender as FrameworkElement).Tag as Note);
                    ((sender as FrameworkElement).Tag as Note).Delete(context, App.ShowToastNotification);
                }
            }
        }

        private void EditTagButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (Database context = new Database())
                {
                    (sender as FrameworkElement).Tag = context.Tags.Include(t => t.NoteTags)
                        .ThenInclude(nt => nt.Note)
                        .First(t => t.Key == ((sender as FrameworkElement).Tag as Tag).Key);
                    MainPage.CurrentTag = ((sender as FrameworkElement).Tag as Tag);
                    this.Frame.Navigate(typeof(TagsEditor), ((sender as FrameworkElement).Tag as Tag));
                }

            }
            catch (Exception ex)
            {
                App.ShowToastNotification(ex.Message, "");
            }
        }

        private async void DeleteTagButton_Click(object sender, RoutedEventArgs e)
        {
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
                using (Database context = new Database())
                {
                    Tag tag = ((sender as FrameworkElement).Tag as Tag);
                    TagsCollection.Remove(tag);
                    tag.Delete(context, App.ShowToastNotification);
                }

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

        private void EntityList_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            foreach (Button button in ((sender as ListViewItem).ContentTemplateRoot as Grid)
                                            .Children.OfType<StackPanel>().Single().Children.OfType<Button>())
            {
                button.Foreground = new SolidColorBrush(Colors.Transparent);
            }
        }


        private void NoteListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
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

        private void AddNew_Click(object sender, RoutedEventArgs e)
        {
            MainPage.Get.NavView_Navigate("edit", null);
        }

        private void TagsListView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (TagsListView.SelectedIndex >= 0)
            {
                using (Database context = new Database())
                {
                    TagsListView.SelectedItem = context.Tags.Include(n => n.NoteTags)
                        .ThenInclude(n => n.Note)
                        .First(n => n.Key == (TagsListView.SelectedItem as Tag).Key);
                    MainPage.CurrentTag = (TagsListView.SelectedItem as Tag);
                    this.Frame.Navigate(typeof(TagsEditor), (TagsListView.SelectedItem as Tag));
                }

            }
        }
    }
}
