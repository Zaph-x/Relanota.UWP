using Core.Objects;
using Core.SqlHelper;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
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
            NotesListView.ItemsSource = MainPage.context.Notes.Local.ToObservableCollection();
            TagsListView.ItemsSource = MainPage.context.Tags.Local.ToObservableCollection();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            MainPage.context.Notes.Load();
            MainPage.context.Tags.Load();
            NotesListView.ItemsSource = MainPage.context.Notes.Local.ToObservableCollection();
            TagsListView.ItemsSource = MainPage.context.Tags.Local.ToObservableCollection();
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            Note note = (sender as FrameworkElement).Tag as Note;
            note = MainPage.context.Notes.Include(n => n.NoteTags).ThenInclude(n => n.Tag).First(n => n.Key == note.Key);
            this.Frame.Navigate(typeof(NoteEditor), note);
        }
    }
}
