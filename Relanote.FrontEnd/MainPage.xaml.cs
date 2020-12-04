using Core.Objects.Entities;
using Core.Objects.Wrappers;
using Microsoft.Toolkit.Uwp.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UWP.FrontEnd.Views;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Core.Preview;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace UWP.FrontEnd
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private static UISettings _uiSettings = new UISettings();

        public static bool IsDarkTheme => _uiSettings.GetColorValue(UIColorType.Background) == Colors.Black;

        private static Note _note { get; set; }
        public static Note CurrentNote {
            get => _note;
            set {
                Get.SetDividerNoteName(value?.Name ?? "No Note Selected");
                _note = value;
            }
        }
        public static Tag CurrentTag = null;
        public static MainPage Get { get; private set; }
        private AdvancedCollectionView Acv { get; set; }
        private int RecentSpacerIndex { get; set; }
        private FixedSizeObservableCollection<Note> recentlyAccessed = new FixedSizeObservableCollection<Note>(10);


        public MainPage()
        {
            Get = this;
            this.InitializeComponent();
            DeserialiseRecentlyAccessed();
            ApplicationViewTitleBar formattableTitleBar = ApplicationView.GetForCurrentView().TitleBar;
            formattableTitleBar.ButtonBackgroundColor = Colors.Transparent;
            if (!IsDarkTheme)
                formattableTitleBar.ButtonForegroundColor = Colors.Black;
            else
            {
                formattableTitleBar.ButtonForegroundColor = Colors.WhiteSmoke;
            }
            CoreApplicationViewTitleBar coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;

            Acv = new AdvancedCollectionView(App.Context.Notes.Select(n => n.Name).ToList(), false);
            Acv.SortDescriptions.Add(new SortDescription(SortDirection.Ascending));
            Acv.Filter = itm => (itm as string).Contains(SearchBox.Text, StringComparison.InvariantCultureIgnoreCase);
            SearchBox.ItemsSource = Acv;
            RecentSpacerIndex = NavigationView.MenuItems.IndexOf(NavigationView.MenuItems.First(itm => (itm as NavigationViewItemBase).Content?.ToString() == "Recently Accessed Notes"));
            SystemNavigationManagerPreview.GetForCurrentView().CloseRequested += MainPage_CloseRequested;
        }

        private async void MainPage_CloseRequested(object sender, SystemNavigationCloseRequestedPreviewEventArgs e)
        {
            e.Handled = true;
            if (!NoteEditor.IsSaved)
            {
                await NoteEditor.ShowUnsavedChangesDialog();
                if (!NoteEditor.IsSaved)
                {
                    return;
                }
            }
            App.Current.Exit();
        }

        public void OnNoteSave(string newName)
        {
            if (!(NavigationView.MenuItems[RecentSpacerIndex + 1] as NavigationViewItemBase).Content.ToString().Equals(newName, StringComparison.InvariantCultureIgnoreCase))
            {
                (NavigationView.MenuItems[RecentSpacerIndex + 1] as NavigationViewItemBase).Content = newName;
            }
            
            Acv = new AdvancedCollectionView(App.Context.Notes.Select(n => n.Name).ToList(), false);
            Acv.SortDescriptions.Add(new SortDescription(SortDirection.Ascending));
            Acv.Filter = itm => (itm as string).Contains(SearchBox.Text, StringComparison.InvariantCultureIgnoreCase);
            SearchBox.ItemsSource = Acv;
        }

        public void LogRecentAccess(Note note)
        {
            if (!recentlyAccessed.Any())
            {
                recentlyAccessed.Insert(note);
                return;
            }

            if (recentlyAccessed[0].Name != note.Name)
                recentlyAccessed.Insert(note);
        }


        private async void RecentlyAccessed_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (Note note in e.NewItems)
                {
                    for (int i = RecentSpacerIndex + 1; i < NavigationView.MenuItems.Count; i++)
                    {
                        if ((NavigationView.MenuItems[i] as NavigationViewItemBase).Content.ToString().Equals(note.Name, StringComparison.InvariantCultureIgnoreCase)) NavigationView.MenuItems.RemoveAt(i--);
                    }
                    NavigationViewItem navigationViewItem = new NavigationViewItem
                    {
                        Tag = note,
                        Content = note.Name,
                        Icon = new SymbolIcon(Symbol.Page2),
                    };
                    NavigationView.MenuItems.Insert(RecentSpacerIndex + 1, navigationViewItem);
                }
            }

            if (e.NewItems?.Count > 0)
            {
                // Recalculate the lengt of of the navview
                int length = NavigationView.MenuItems.Count - 1 - RecentSpacerIndex;
                if (length > recentlyAccessed.Size)
                {
                    NavigationView.MenuItems.RemoveAt(NavigationView.MenuItems.Count - 1);
                }
            }
            await SerialiseRecentlyAdded(recentlyAccessed);
        }

        private async Task SerialiseRecentlyAdded(FixedSizeObservableCollection<Note> recentlyAccessed)
        {
            StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync("AccessList", CreationCollisionOption.OpenIfExists);
            string content = "";
            foreach (Note note in recentlyAccessed)
            {
                content += $"{note.Key}{Environment.NewLine}";
            }
            content = content.Trim();
            while (!file.IsAvailable)
            {

            }

            await FileIO.WriteTextAsync(file, content, Windows.Storage.Streams.UnicodeEncoding.Utf8);
        }

        private async void DeserialiseRecentlyAccessed()
        {
            StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync("AccessList", CreationCollisionOption.OpenIfExists);
            IList<string> lines = await FileIO.ReadLinesAsync(file, Windows.Storage.Streams.UnicodeEncoding.Utf8);
            FixedSizeObservableCollection<Note> tempCollection = new FixedSizeObservableCollection<Note>(10);
            foreach (string line in lines)
            {
                if (int.TryParse(line, out int key))
                {
                    if (App.Context.TryGetNote(key, true, out Note note))
                    {
                        tempCollection.Add(note);
                        NavigationViewItem navigationViewItem = new NavigationViewItem
                        {
                            Tag = note,
                            Content = note.Name,
                            Icon = new SymbolIcon(Symbol.Page2),
                        };
                        NavigationView.MenuItems.Add(navigationViewItem);
                    }
                }
            }
            tempCollection.CollectionChanged += RecentlyAccessed_CollectionChanged;
            recentlyAccessed = tempCollection;
        }

        private void SearchBox_FocusEngaged(Control sender, FocusEngagedEventArgs args)
        {

        }

        private readonly List<(string Tag, Type Page, object obj)> _pages = new List<(string Tag, Type Page, object obj)>
        {
            ("list", typeof(Home), null),
            ("edit", typeof(NoteEditor), CurrentNote),
            ("tags", typeof(TagsEditor), CurrentNote),
            ("export", typeof(Export), null),
            ("help", typeof(Help), null),
        };

        private void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked == true)
            {
                NavView_Navigate("settings", args.RecommendedNavigationTransitionInfo);
            }
            else if (args.InvokedItemContainer != null)
            {
                var navItemTag = args.InvokedItemContainer.Tag;
                if (navItemTag.GetType() == typeof(Note))
                {
                    HandleRecentlyAccessedNavigation((navItemTag as Note), args);
                }
                NavView_Navigate(navItemTag.ToString(), args.RecommendedNavigationTransitionInfo);
            }
        }

        private async void HandleRecentlyAccessedNavigation(Note tagNote, NavigationViewItemInvokedEventArgs args)
        {
            // Navigate to the list view to create a transition (Reset view)
            if (tagNote.Name != (CurrentNote?.Name ?? "")) NavView_Navigate("list", args.RecommendedNavigationTransitionInfo);
            SetNavigationIndex(3);

            // We only want to switch if the note is in the context
            if (tagNote.IsInContext(App.Context) && tagNote.TryGetFullNote(App.Context, out Note note))
            {
                CurrentNote = note;
                // Update and navigate
                UpdateRecentlyAccessedList(note);
                NavView_Navigate("edit", args.RecommendedNavigationTransitionInfo);
                return;
            }
            else
            {
                // If the note no longer exists in the context, we want to let the user know, and remove it from the list.
                await App.ShowDialog("We could not find that note.", $"The note '{tagNote.Name}' could note be found in the database.", "Okay");
                recentlyAccessed.Remove(tagNote);
                NavigationView.MenuItems.Remove(args.InvokedItemContainer);

            }
        }

        private void UpdateRecentlyAccessedList(Note note)
        {
            // Calculate length of navitem lists. If it's greater than 0, add to the list
            int length = NavigationView.MenuItems.Count - 1 - RecentSpacerIndex;
            if (length > 0 && !(NavigationView.MenuItems[RecentSpacerIndex + 1] as NavigationViewItem).Content.ToString().Equals(note.Name, StringComparison.InvariantCultureIgnoreCase))
                recentlyAccessed.Insert(note);
            // We want to track the navigation state
            NoteEditor.Get.State = NoteEditorState.RecentNavigation;
        }

        public async void NavView_Navigate(string navItemTag, NavigationTransitionInfo transitionInfo)
        {
            Type _page = null;
            Note _note = null;
            if (navItemTag == "settings")
            {
                _page = typeof(Settings);
            }
            else
            {
                var item = _pages.FirstOrDefault(p => p.Tag.Equals(navItemTag));
                _page = item.Page;
                _note = item.obj as Note;
            }
            var preNavPageType = ContentFrame.CurrentSourcePageType;

            if (!(_page is null) && !Equals(preNavPageType, _page))
            {
                if (!NoteEditor.IsSaved && !NoteEditor.Get.AreTextboxesEmpty())
                {
                    await NoteEditor.ShowUnsavedChangesDialog();
                    if (!NoteEditor.IsSaved)
                    {
                        // The user chose save, but was unable to. We don't wish to delete the note
                        return;
                    }
                }
                if (navItemTag == "list")
                {
                    CurrentNote = null;
                    SearchBox.Text = "";
                }
                ContentFrame.Navigate(_page, _note, transitionInfo);
            }
        }

        public void SetDividerNoteName(string name)
        {
            NoteName.Content = name;
        }

        public void SetNavigationIndex(int index)
        {
            NavigationView.SelectedItem = NavigationView.MenuItems[index];
        }

        private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            // Make sure the event is fired because of the user
            if (args.CheckCurrent() && args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                Acv.RefreshFilter();
            }
        }

        private void SearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
        }

        private async void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            Note note = App.Context.Notes.AsEnumerable().FirstOrDefault(n => n.Name.Equals(sender.Text, StringComparison.InvariantCultureIgnoreCase));
            if (note == null)
            {
                await App.ShowDialog("We could not find that note.", $"The note '{sender.Text}' could note be found in the database. Do you wish to create it?",
                    "Yes", () =>
                    {
                        note = new Note()
                        {
                            Name = sender.Text
                        };
                        note.Save("", sender.Text, App.Context);
                        CurrentNote = note;
                        NoteEditor.SetState(NoteEditorState.SearchNavigation);
                        NavView_Navigate("edit", null);
                    },
                    "No", () =>
                    {
                        SearchBox.Text = "";
                    });
            } else
            {
                CurrentNote = note;
                NavView_Navigate("tags", null);
                NoteEditor.SetState(NoteEditorState.SearchNavigation);
                NavView_Navigate("edit", null);
            }

        }
    }
}
