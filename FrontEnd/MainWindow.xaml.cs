using System.Text.RegularExpressions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Core.Objects;
using Core.SqlHelper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;

namespace FrontEnd
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Database context = new Database();

        private Note currentSelectedNote { get; set; }

        private ICollectionView _noteListView;
        private ICollectionView _tagListView;

        public MainWindow()
        {
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            context.Database.EnsureCreated();
            context.Notes.Load();
            NoteList.ItemsSource = context.Notes.Local.ToObservableCollection();
            NoteList.Items.SortDescriptions.Add(
                new System.ComponentModel.SortDescription("Name",
                    System.ComponentModel.ListSortDirection.Ascending));

            _noteListView = CollectionViewSource.GetDefaultView(NoteList.ItemsSource);
            _tagListView = CollectionViewSource.GetDefaultView(TagList.ItemsSource);
        }

        private void NoteContentBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if ((sender as TextBox)?.Text == "Write Something... ✏💭")
            {
                (sender as TextBox).Text = "";
                (sender as TextBox).Foreground = Brushes.Black;
            }
        }

        private void NoteContentBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace((sender as TextBox).Text))
            {
                (sender as TextBox).Text = "Write Something... ✏💭";
                (sender as TextBox).Foreground = Brushes.DarkGray;
            }
            else
            {
                SaveNote();
            }
        }

        public void LoadNote(Note note)
        {
            currentSelectedNote = note;

            NoteNameBox.Text = note.Name;
            NoteContentBox.Text = note.Content;
            TagList.ItemsSource = new ObservableCollection<Tag>(note.NoteTags.Select(nt => nt.Tag));
            currentSelectedNote.HasChanges = false;
        }

        private void SaveNote()
        {
            if (currentSelectedNote == null)
            {
                if (!string.IsNullOrWhiteSpace(NoteNameBox.Text) && (NoteContentBox.Text != "Write Something... ✏💭" && !string.IsNullOrWhiteSpace(NoteContentBox.Text)))
                {
                    if (context.Notes.Any(note => note.Name.ToLower() == NoteNameBox.Text.ToLower()))
                    {
                        string contentToAppend = NoteContentBox.Text;
                        LoadNote(context.Notes
                            .Include(note => note.NoteTags)
                            .ThenInclude(nt => nt.Tag).First(note => note.Name.ToLower() == NoteNameBox.Text.ToLower()));
                        currentSelectedNote.Content += $"\n\n{contentToAppend}";
                        context.TryUpdateManyToMany(currentSelectedNote.NoteTags, currentSelectedNote.NoteTags, x => x.TagKey);
                        context.SaveChanges();
                        NoteContentBox.Text = currentSelectedNote.Content;
                    }
                    else
                    {
                        currentSelectedNote = new Note
                        {
                            Name = NoteNameBox.Text.Trim(),
                            Content = NoteContentBox.Text.Trim()
                        };
                        context.Notes.Add(currentSelectedNote);
                        context.SaveChanges();
                    }

                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(NoteNameBox.Text) && (NoteContentBox.Text != "Write Something... ✏💭" && !string.IsNullOrWhiteSpace(NoteContentBox.Text)))
                {
                    currentSelectedNote.Name = NoteNameBox.Text.Trim();
                    currentSelectedNote.Content = NoteContentBox.Text.Trim();
                    currentSelectedNote.NoteTags = context.NoteTags.Where(nt => nt.NoteKey == currentSelectedNote.Key).Include(nt => nt.Tag).ToList();
                    context.TryUpdateManyToMany(currentSelectedNote.NoteTags, currentSelectedNote.NoteTags, x => x.TagKey);
                    context.SaveChanges();
                }
            }

            if (currentSelectedNote != null)
            {
                currentSelectedNote.HasChanges = false;
            }
            CollectionViewSource.GetDefaultView(NoteList.ItemsSource).Refresh();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveNote();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentSelectedNote == null)
            {
                NoteNameBox.Text = "";
                NoteContentBox.Foreground = Brushes.DarkGray;
                NoteContentBox.Text = "Write Something... ✏💭";
            }
            else if (MessageBox.Show(this, "Once completed, this action can not be undone.", $"Do you wish to delete '{currentSelectedNote.Name}'?", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) ==
              MessageBoxResult.Yes)
            {
                context.Notes.Remove(currentSelectedNote);
                context.SaveChanges();
                currentSelectedNote = null;
                NoteNameBox.Text = "";
                NoteContentBox.Foreground = Brushes.DarkGray;
                NoteContentBox.Text = "Write Something... ✏💭";
                TagList.ItemsSource = new ObservableCollection<Tag>();
            }

        }

        private void NoteList_DoubleClickNote(object sender, MouseButtonEventArgs e)
        {
            if (NoteList.SelectedIndex != -1)
            {
                Note note = NoteList.SelectedItems[0] as Note;
                note = context.Notes.Include(n => n.NoteTags).ThenInclude(nt => nt.Tag).First(n => n.Key == note.Key);
                LoadNote(note);
                NoteContentBox.Foreground = Brushes.Black;
                TagList.ItemsSource = new ObservableCollection<Tag>(note.NoteTags.Select(nt => nt.Tag));
                CollectionViewSource.GetDefaultView(TagList.ItemsSource).Refresh();
            }
        }

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if ((sender as TextBox)?.Text == "Search...")
            {
                (sender as TextBox).Text = "";
                (sender as TextBox).Foreground = Brushes.Black;
            }
        }

        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace((sender as TextBox)?.Text))
            {
                (sender as TextBox).Text = "Search...";
                (sender as TextBox).Foreground = Brushes.DarkGray;
            }
        }

        private void NewButton_Click(object sender, RoutedEventArgs e)
        {
            if (context.Notes.Local.Any(note => note.Name.ToLower() == NoteNameBox.Text.ToLower()))
            {
                if (NoteContentBox.Text != context.Notes.First(note => note.Name.ToLower() == NoteNameBox.Text.ToLower())?.Content)
                {
                    if (MessageBox.Show(this, "Do you wish to save this note?\n\nIf you pick no, it can not be recovered", "You have unsaved changes",
                            MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        SaveNote();
                    }
                }
                else
                {
                    if (!(NoteContentBox.Text == "Write Something... ✏💭" || string.IsNullOrWhiteSpace(NoteContentBox.Text)) && string.IsNullOrWhiteSpace(NoteNameBox.Text))
                    {
                        if (MessageBox.Show(this, "Do you wish to finish this note?", "You have an unfinished note",
                                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                        {
                            return;
                        }
                    }
                    else if (!string.IsNullOrWhiteSpace(NoteNameBox.Text) &&
                             (NoteContentBox.Text != "Write Something... ✏💭" &&
                              !string.IsNullOrWhiteSpace(NoteContentBox.Text)) && currentSelectedNote.HasChanges)
                    {
                        if (MessageBox.Show(this, "Do you wish to save this note?\n\nIf you pick no, it can not be recovered", "You have an unsaved note",
                                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                        {
                            SaveNote();
                        }
                    }
                }
            }
            else
            {
                if (!(NoteContentBox.Text == "Write Something... ✏💭" || string.IsNullOrWhiteSpace(NoteContentBox.Text)) && string.IsNullOrWhiteSpace(NoteNameBox.Text))
                {
                    if (MessageBox.Show(this, "Do you wish to finish this note?", "You have an unfinished note",
                            MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        return;
                    }
                }
                else if (!string.IsNullOrWhiteSpace(NoteNameBox.Text) &&
                         (NoteContentBox.Text != "Write Something... ✏💭" &&
                          !string.IsNullOrWhiteSpace(NoteContentBox.Text)))
                {
                    if (MessageBox.Show(this, "Do you wish to save this note?\n\nIf you pick no, it can not be recovered", "You have an unsaved note",
                            MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        SaveNote();
                    }
                }
            }

            currentSelectedNote = null;
            NoteNameBox.Text = "";
            NoteContentBox.Foreground = Brushes.DarkGray;
            NoteContentBox.Text = "Write Something... ✏💭";
            TagList.ItemsSource = new ObservableCollection<Tag>();
            NoteList.SelectedIndex = -1;
            CollectionViewSource.GetDefaultView(TagList.ItemsSource).Refresh();
        }

        private void AddTagButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NewTagNameBox.Text)) return;

            if (currentSelectedNote != null)
            {
                if (!(string.IsNullOrWhiteSpace(NoteNameBox.Text) || string.IsNullOrWhiteSpace(NoteContentBox.Text))
                    && context.Notes.Any(note => note.Name.ToLower() == currentSelectedNote.Name.ToLower()))
                {
                    SaveNote();
                }
                else
                {
                    MessageBox.Show(this, "Please add a name and some notes to this entry before adding tags.",
                        "Could not save note", MessageBoxButton.OK);
                    NewTagNameBox.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(NewTagNameBox.Text))
                {
                    MessageBox.Show(this, "Please provide a tag name when adding tags to notes");
                    NewTagNameBox.Focus();
                    return;
                }

                Tag tag = context.Tags.Any(t => t.Name.ToLower() == NewTagNameBox.Text.ToLower()) ?
                    context.Tags.First(t => t.Name.ToLower() == NewTagNameBox.Text.ToLower()) :
                    new Tag() { Name = NewTagNameBox.Text };

                NoteTag noteTag = new NoteTag() { Note = currentSelectedNote, Tag = tag };
                try
                {
                    context.NoteTags.Add(noteTag);
                }
                catch
                {
                    MessageBox.Show(this, "This note is already connected to that tag", "Duplicate tag detected",
                        MessageBoxButton.OK);
                    NewTagNameBox.Focus();
                    return;
                }
                context.SaveChanges();

                TagList.ItemsSource = currentSelectedNote.NoteTags.Select(nt => nt.Tag);
                CollectionViewSource.GetDefaultView(TagList.ItemsSource).Refresh();
                NewTagNameBox.Text = "";
            }
            else
            {
                MessageBox.Show(this, "Please name your note before adding tags", "Could not add tag",
                    MessageBoxButton.OK);
            }
            NewTagNameBox.Focus();
        }

        private void NewTagNameBox_EnterPressed(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddTagButton_Click(sender, null);
            }
        }

        private void RemoveTagButton_Click(object sender, RoutedEventArgs e)
        {
            NoteTag noteTag = null;
            if (!string.IsNullOrWhiteSpace(NewTagNameBox.Text))
            {
                noteTag = currentSelectedNote.NoteTags.FirstOrDefault(nt =>
                        nt.Tag.Name.ToLower() == NewTagNameBox.Text.ToLower());
                currentSelectedNote.NoteTags.Remove(noteTag);
                context.TryUpdateManyToMany(currentSelectedNote.NoteTags, currentSelectedNote.NoteTags, x => x.TagKey);
                context.SaveChanges();
                TagList.ItemsSource = currentSelectedNote.NoteTags.Select(nt => nt.Tag);
                CollectionViewSource.GetDefaultView(TagList.ItemsSource).Refresh();
                NewTagNameBox.Text = "";
                if (!context.NoteTags.Any(nt => nt.Tag == noteTag.Tag))
                {
                    context.Remove(noteTag.Tag);
                    context.SaveChanges();
                }
                NewTagNameBox.Focus();
                return;
            }
            if (TagList.SelectedIndex == -1)
            {
                NewTagNameBox.Focus();
                return;
            }

            noteTag = currentSelectedNote.NoteTags.FirstOrDefault(nt => nt.Tag == (TagList.SelectedItem as Tag));
            currentSelectedNote.NoteTags.Remove(noteTag);
            context.TryUpdateManyToMany(currentSelectedNote.NoteTags, currentSelectedNote.NoteTags, x => x.TagKey);
            context.SaveChanges();
            TagList.ItemsSource = currentSelectedNote.NoteTags.Select(nt => nt.Tag);
            CollectionViewSource.GetDefaultView(TagList.ItemsSource).Refresh();
            NewTagNameBox.Text = "";
            if (!context.NoteTags.Any(nt => nt.Tag == noteTag.Tag))
            {
                context.Remove(noteTag.Tag);
                context.SaveChanges();
            }
            NewTagNameBox.Focus();
        }

        private void TagList_DoubleClickNote(object sender, MouseButtonEventArgs e)
        {
            if (TagList.SelectedIndex == -1) return;

            Tag tag = TagList.SelectedItems[0] as Tag;

            RelatedItemsView riv = new RelatedItemsView(tag, context, this);
            riv.ShowDialog();
        }

        private void NoteContentBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (currentSelectedNote != null)
                currentSelectedNote.HasChanges = true;
                
            // Macro handling
            Match match = Regex.Match(NoteContentBox.Text, @"\\grid\{\s*(\d+)\s*,\s*(\d+)\s*(,\s*(\d+)\s*)?\}");
            if (match.Success)
            {
                Core.Macros.Grid grid = null;
                if (!string.IsNullOrEmpty(match.Groups[3].Value))
                    grid = new Core.Macros.Grid(int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value), int.Parse(match.Groups[4].Value));
                else
                    grid = new Core.Macros.Grid(int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value), 3);
                
                NoteContentBox.Text = NoteContentBox.Text.Replace(match.Value, "\n" + grid.WriteComponent());
                NoteContentBox.CaretIndex = NoteContentBox.Text.Length;
                return;
            }
            match = Regex.Match(NoteContentBox.Text, @"\\pagebreak");
            if (match.Success)
            {
                NoteContentBox.Text = NoteContentBox.Text.Replace(match.Value ,"\n" + "-".Repeat(60) + "\n");
                NoteContentBox.CaretIndex = NoteContentBox.Text.Length;
                return;
            }

        }

        private void TagSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if ((e.Source as TextBox).Text.ToLower() == "search...") return;
            TagList.ItemsSource ??= new ObservableCollection<Tag>();
            _tagListView = CollectionViewSource.GetDefaultView(TagList.ItemsSource);

            _tagListView.Filter = (item) =>
            {
                Tag _ = item as Tag;
                string query = (e.Source as TextBox).Text;
                return AreAlike(query, _.Name);
            };
        }

        private bool AreAlike(string query, string item) => item.ToLower().Contains(query.ToLower());

        private void NoteListSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if ((e.Source as TextBox).Text.ToLower() == "search...") return;
            NoteList.ItemsSource ??= new ObservableCollection<Note>();
            _noteListView = CollectionViewSource.GetDefaultView(NoteList.ItemsSource);
            _noteListView.Filter = (item) =>
            {
                Note _ = item as Note;
                string query = (e.Source as TextBox).Text;
                if (query.Length < 2) return AreAlike(query, _.Name);

                return (query.Substring(0, 2).ToLower()) switch
                {
                    "t:" => _.NoteTags.Select(nt => nt.Tag).Any(t => AreAlike(query.Replace("t:", ""), t.Name)),
                    "n:" => AreAlike(query.Replace("n:", ""), _.Name),
                    "c:" => AreAlike(query.Replace("c:", ""), _.Content),
                    _ => AreAlike(query, _.Name)
                };
            };
        }
    }

    public static class Extensions
    {
        public static string Repeat(this string str, int times)
        {
            string newString = str;
            for (int i = 0; i < times; i++)
            {
                newString += str;
            }
            return newString;
        }
    }
}
