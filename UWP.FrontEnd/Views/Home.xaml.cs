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
using Windows.UI;
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
        ObservableCollection<Note> NotesCollection = new ObservableCollection<Note>();
        ObservableCollection<Tag> TagsCollection = new ObservableCollection<Tag>();

        public Home()
        {
            this.InitializeComponent();
            App.Context.Notes.Load();
            App.Context.Tags.Load();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            App.Context.Notes.Load();
            App.Context.Tags.Load();
            NotesCollection = new ObservableCollection<Note>(App.Context.Notes.Local.OrderBy(note => note.Name));
            TagsCollection = new ObservableCollection<Tag>(App.Context.Tags.Local.OrderBy(tag => tag.Name));
            MainPage.Get.SetDividerNoteName("No Note Selected");
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            Note note = (sender as FrameworkElement).Tag as Note;
            note = App.Context.Notes.Include(n => n.NoteTags).ThenInclude(n => n.Tag).First(n => n.Key == note.Key);
            MainPage.CurrentNote = note;
            this.Frame.Navigate(typeof(NoteEditor), note);
        }

        private void NotesListView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (NotesListView.SelectedIndex >= 0)
            {
                Note note = NotesListView.SelectedItem as Note;
                note = App.Context.Notes.Include(n => n.NoteTags).ThenInclude(n => n.Tag).First(n => n.Key == note.Key);
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
                NotesCollection.Remove(note);
                note.Delete(App.Context, App.ShowMessageBox);
            }
        }

        private void EditTagButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Tag tag = (sender as FrameworkElement).Tag as Tag;
                tag = App.Context.Tags.Include(t => t.NoteTags).ThenInclude(nt => nt.Note).First(t => t.Key == tag.Key);
                MainPage.CurrentTag = tag;
                this.Frame.Navigate(typeof(TagsEditor), tag);
            }
            catch (Exception ex)
            {
                App.ShowMessageBox(ex.Message, "");
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
                tag.Delete(App.Context, App.ShowMessageBox);
                TagsListView.Items.Remove(tag);
            }
        }

        private void EntityList_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            ListViewItem item = sender as ListViewItem;
            Grid grid = item.ContentTemplateRoot as Grid;


            StackPanel panel = grid.Children.OfType<StackPanel>().Single();
            if (MainPage.IsDarkTheme)
            {
                foreach (Button button in panel.Children.OfType<Button>())
                {

                    button.Foreground = new SolidColorBrush(Colors.Gainsboro);
                }
            }
            else
            {
                foreach (Button button in panel.Children.OfType<Button>())
                {

                    button.Foreground = new SolidColorBrush(Colors.Black);
                }
            }
        }

        private void EntityList_PointerExited(object sender, PointerRoutedEventArgs e)
        {

            ListViewItem item = sender as ListViewItem;
            Grid grid = item.ContentTemplateRoot as Grid;
            //grid.Data

            StackPanel panel = grid.Children.OfType<StackPanel>().Single();
            foreach (Button button in panel.Children.OfType<Button>())
            {
                button.Foreground = new SolidColorBrush(Colors.Transparent);
            }
        }


        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (var item in NotesListView.Items)
            {
                ListViewItem listItem = NotesListView.ContainerFromItem(item) as ListViewItem;
                listItem.PointerEntered += EntityList_PointerEntered;
                listItem.PointerExited += EntityList_PointerExited;

            }

            foreach (var item in TagsListView.Items)
            {
                ListViewItem listItem = TagsListView.ContainerFromItem(item) as ListViewItem;
                listItem.PointerEntered += EntityList_PointerEntered;
                listItem.PointerExited += EntityList_PointerExited;
            }
        }

        private void NoteListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (var addedItem in e.AddedItems)
            {
                ListView lv = (sender as ListView);
                ListViewItem item = lv.ContainerFromItem(addedItem) as ListViewItem;
                Grid grid = item.ContentTemplateRoot as Grid;
                //grid.Data
                item.PointerExited -= EntityList_PointerExited;
                StackPanel panel = grid.Children.OfType<StackPanel>().Single();

                if (MainPage.IsDarkTheme)
                {
                    foreach (Button button in panel.Children.OfType<Button>())
                    {

                        button.Foreground = new SolidColorBrush(Colors.Gainsboro);
                    }
                }
                else {
                    foreach (Button button in panel.Children.OfType<Button>())
                    {

                        button.Foreground = new SolidColorBrush(Colors.Black);
                    }
                }

            }

            foreach (var removedItem in e.RemovedItems)
            {
                ListView lv = (sender as ListView);
                ListViewItem item = lv.ContainerFromItem(removedItem) as ListViewItem;
                Grid grid = item.ContentTemplateRoot as Grid;
                //grid.Data
                item.PointerExited += EntityList_PointerExited;
                StackPanel panel = grid.Children.OfType<StackPanel>().Single();
                foreach (Button button in panel.Children.OfType<Button>())
                {
                    button.Foreground = new SolidColorBrush(Colors.Transparent);
                }
            }

        }
    }
}
