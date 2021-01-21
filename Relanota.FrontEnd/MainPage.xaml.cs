using Core.Objects.Entities;
using Core.Objects.Wrappers;
using Microsoft.Toolkit.Uwp.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UWP.FrontEnd.Views;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Core.Preview;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Core.SqlHelper;
using Core.StateHandler;
using System.ComponentModel;
using System.Threading;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace UWP.FrontEnd
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private static UISettings _uiSettings = new UISettings();

        public static bool IsDarkTheme { get; private set; }

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
        private FixedSizeObservableCollection<string> recentlyAccessed = new FixedSizeObservableCollection<string>(10);
        ApplicationViewTitleBar formattableTitleBar;


        public MainPage()
        {
            AppState.Set(State.Loading);
            Get = this;

            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            formattableTitleBar = ApplicationView.GetForCurrentView().TitleBar;
            this.InitializeComponent();
            DeserialiseRecentlyAccessed();
            formattableTitleBar.ButtonBackgroundColor = Colors.Transparent;
            SetTheme(localSettings.Values["theme"] as string);
            CoreApplicationViewTitleBar coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;
            using (Database context = new Database())
            {
                Acv = new AdvancedCollectionView(context.Notes.Select(n => n.Name).ToList(), false);
                Acv.SortDescriptions.Add(new SortDescription(SortDirection.Ascending));
                Acv.Filter = itm => (itm as string).Contains(SearchBox.Text, StringComparison.InvariantCultureIgnoreCase);
                SearchBox.ItemsSource = Acv;
            }

            RecentSpacerIndex = NavigationView.MenuItems.IndexOf(NavigationView.MenuItems.First(itm => (itm as NavigationViewItemBase).Content?.ToString() == "Recently Accessed Notes"));
            SystemNavigationManagerPreview.GetForCurrentView().CloseRequested += MainPage_CloseRequested;

            if (App.HasLegacyDB)
            {
                App.ShowDialog("Legacy database detected",
                    "A legacy database was found when Relanota was starting up. We have moved the content to a non-legacy Database.",
                    "Okay");
                App.HasLegacyDB = false;
            }
            AppState.Set(State.Ready);
        }

        private async void MainPage_CloseRequested(object sender, SystemNavigationCloseRequestedPreviewEventArgs e)
        {
            e.Handled = true;
            if (!NoteEditor.IsSaved)
            {
                await NoteEditor.Get.ShowUnsavedChangesDialog();
                if (!NoteEditor.IsSaved)
                {
                    return;
                }
            }
            App.Current.Exit();
        }

        public void SetTheme(string theme)
        {

            switch (theme)
            {
                case "Light":
                    this.RequestedTheme = ElementTheme.Light;
                    formattableTitleBar.ButtonInactiveBackgroundColor = Colors.White;
                    formattableTitleBar.ButtonForegroundColor = Colors.Black;
                    IsDarkTheme = false;
                    break;
                case "Dark":
                    this.RequestedTheme = ElementTheme.Dark;
                    formattableTitleBar.ButtonForegroundColor = Colors.WhiteSmoke;
                    formattableTitleBar.ButtonInactiveBackgroundColor = Colors.Black;
                    IsDarkTheme = true;
                    break;
                default:
                    UISettings DefaultTheme = new UISettings();
                    string uiTheme = DefaultTheme.GetColorValue(UIColorType.Background).ToString();
                    this.RequestedTheme = ElementTheme.Default;
                    if (uiTheme == "#FFFFFFFF")
                    {
                        formattableTitleBar.ButtonForegroundColor = Colors.Black;
                        formattableTitleBar.ButtonInactiveBackgroundColor = Colors.White;
                        IsDarkTheme = false;
                    }
                    else
                    {
                        formattableTitleBar.ButtonForegroundColor = Colors.WhiteSmoke;
                        formattableTitleBar.ButtonInactiveBackgroundColor = Colors.Black;
                        IsDarkTheme = true;
                    }
                    break;
            }
        }

        public void OnNoteSave(string newName)
        {
            if (NavigationView.MenuItems.Count - 1 > RecentSpacerIndex && !(NavigationView.MenuItems[RecentSpacerIndex + 1] as NavigationViewItemBase).Content.ToString().Equals(newName, StringComparison.InvariantCultureIgnoreCase))
            {
                (NavigationView.MenuItems[RecentSpacerIndex + 1] as NavigationViewItemBase).Content = newName;
            }
            using (Database context = new Database())
            {
                Acv = new AdvancedCollectionView(context.Notes.Select(n => n.Name).ToList(), false);
                Acv.SortDescriptions.Add(new SortDescription(SortDirection.Ascending));
                Acv.Filter = itm => (itm as string).Contains(SearchBox.Text, StringComparison.InvariantCultureIgnoreCase);
                SearchBox.ItemsSource = Acv;
            }

        }

        public void LogRecentAccess(Note note)
        {
            if (!recentlyAccessed.Any())
            {
                recentlyAccessed.Insert(note.Key.ToString(), true);
                return;
            }

            if (recentlyAccessed[0] != note.Key.ToString())
                UpdateRecentlyAccessedList(note);
        }


        private void RecentlyAccessed_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {

                using (Database context = new Database())
                {
                    foreach (string note in e.NewItems)
                    {
                        if (context.TryGetNote(int.Parse(note), out Note newNote))
                        {


                            for (int i = RecentSpacerIndex + 1; i < NavigationView.MenuItems.Count; i++)
                            {
                                if (((NavigationView.MenuItems[i] as NavigationViewItemBase).Tag as Note).Name
                                    .Equals(newNote.Name, StringComparison.InvariantCultureIgnoreCase))
                                    NavigationView.MenuItems.RemoveAt(i--);
                            }


                            NavigationViewItem navigationViewItem = new NavigationViewItem
                            {
                                Tag = newNote,
                                Content = new TextBlock
                                {
                                    Text = newNote.Name,
                                    TextTrimming = TextTrimming.CharacterEllipsis
                                },
                                Icon = new SymbolIcon(Symbol.Page2),
                            };
                            NavigationView.MenuItems.Insert(RecentSpacerIndex + 1, navigationViewItem);
                        }
                    }
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
            SerialiseRecentlyAdded(recentlyAccessed);
        }

        private async void SerialiseRecentlyAdded(FixedSizeObservableCollection<string> recentlyAccessed)
        {
            string path = $"{ApplicationData.Current.LocalFolder.Path}/AccessList";
            string content = "";
            foreach (string noteKey in recentlyAccessed)
            {
                content += $"{noteKey}{Environment.NewLine}";
            }
            content = content.Trim();

            using (FileStream fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    // discard the contents of the file by setting the length to 0
                    fs.SetLength(0);
                    sw.Write(content);
                }
            }
        }

        private async void DeserialiseRecentlyAccessed()
        {
            StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync("AccessList", CreationCollisionOption.OpenIfExists);
            IList<string> lines = await FileIO.ReadLinesAsync(file, Windows.Storage.Streams.UnicodeEncoding.Utf8);
            FixedSizeObservableCollection<string> tempCollection = new FixedSizeObservableCollection<string>(10);
            foreach (string line in lines)
            {
                if (int.TryParse(line, out int key))
                {
                    using (Database context = new Database())
                    {
                        if (context.TryGetNote(key, out Note note))
                        {
                            tempCollection.Add(note.Key.ToString());
                            NavigationViewItem navigationViewItem = new NavigationViewItem
                            {
                                Tag = note,
                                Content = new TextBlock
                                {
                                    Text = note.Name,
                                    TextTrimming = TextTrimming.CharacterEllipsis
                                },
                                Icon = new SymbolIcon(Symbol.Page2),

                            };
                            NavigationView.MenuItems.Add(navigationViewItem);
                        }
                    }

                }
            }
            tempCollection.CollectionChanged += RecentlyAccessed_CollectionChanged;
            recentlyAccessed = tempCollection;
        }

        private void SearchBox_FocusEngaged(Control sender, FocusEngagedEventArgs args)
        {

        }

        private readonly List<(string Tag, Type Page, int Index)> _pages = new List<(string Tag, Type Page, int Index)>
        {
            ("list", typeof(Home), 0),
            ("edit", typeof(NoteEditor), 3),
            ("tags", typeof(TagsEditor), 4),
            ("export", typeof(Export), 5),
            ("help", typeof(Help), 6),
            ("settings", typeof(Settings), -1),
            ("reset", typeof(Reset), -1),
        };

        private void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            AppState.Set(State.Navigation, () => {
                if (args.IsSettingsInvoked == true)
                {
                    NavView_Navigate("settings", args.RecommendedNavigationTransitionInfo);
                }
                else if (args.InvokedItemContainer != null)
                {
                    if (args.InvokedItemContainer.Tag.GetType() == typeof(Note))
                    {
                        HandleRecentlyAccessedNavigation((args.InvokedItemContainer.Tag as Note), args);
                        return;
                    }
                    else
                    {
                        NavView_Navigate(args.InvokedItemContainer.Tag.ToString(), null);
                    }

                }
            }, State.Ready);
            
        }

        private void HandleRecentlyAccessedNavigation(Note tagNote, NavigationViewItemInvokedEventArgs args)
        {
            AppState.Set(State.RecentNavigation, async () =>
            {
                // Navigate to the list view to create a transition (Reset view)
                if (tagNote.Name != (CurrentNote?.Name ?? ""))
                {
                    using (Database context = new Database())
                    {
                        if (CurrentNote != null && !NoteEditor.IsSaved && (CurrentNote?.IsInContext(context) ?? false))
                        {
                            await NoteEditor.Get.ShowUnsavedChangesDialog();

                        }
                        ContentFrame.Navigate(typeof(Reset));
                    }

                }
                SetNavigationIndex(3);
                using (Database context = new Database())
                {
                    // We only want to switch if the note is in the context
                    if (tagNote.IsInContext(context) && tagNote.TryGetFullNote(context, out Note note))
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
                        App.ShowDialog("We could not find that note.", $"The note '{tagNote.Name}' could note be found in the database.", "Okay");
                        recentlyAccessed.Remove(tagNote.Key.ToString());
                        NavigationView.MenuItems.Remove(args.InvokedItemContainer);
                        NavView_Navigate("list", null);

                    }
                }
            }, State.Ready);

        }

        private void UpdateRecentlyAccessedList(Note note)
        {
            // Calculate length of navitem lists. If it's greater than 0, add to the list
            int length = NavigationView.MenuItems.Count - 1 - RecentSpacerIndex;

            BackgroundWorker disableWorker = new BackgroundWorker();

            disableWorker.DoWork += (o, e) => Thread.Sleep(500);
            disableWorker.RunWorkerCompleted += (o, e) =>
            {
                for (int i = RecentSpacerIndex + 1; i < length; i++)
                    (NavigationView.MenuItems[i] as NavigationViewItem).IsHitTestVisible = true;
            };

            // We want to track the navigation state

            if (recentlyAccessed[0] == note.Key.ToString()) return;
            recentlyAccessed.Remove(note.Key.ToString());
            if (length > 0 && !(NavigationView.MenuItems[RecentSpacerIndex + 1] as NavigationViewItem).Content.ToString().Equals(note.Name, StringComparison.InvariantCultureIgnoreCase))
                recentlyAccessed.Insert(note.Key.ToString(), true);
            for (int i = RecentSpacerIndex + 1; i < length; i++)
                (NavigationView.MenuItems[i] as NavigationViewItem).IsHitTestVisible = false;
            if (!disableWorker.IsBusy)
                disableWorker.RunWorkerAsync();

        }

        public async void NavView_Navigate(string navItemTag, NavigationTransitionInfo transitionInfo)
        {
            if (((int)AppState.Current & 0b_0100_0000_0000_0001) == 0) return;
            if (navItemTag == "reset")
            {
                ContentFrame.Navigate(typeof(Reset));
                return;
            }

            (string tag, Type page, int index) = _pages.FirstOrDefault(p => p.Tag.Equals(navItemTag));
            if (index != -1) SetNavigationIndex(index);
            var preNavPageType = ContentFrame.CurrentSourcePageType;

            if (!(page is null) && preNavPageType != page)
            {
                if (!NoteEditor.IsSaved)
                {
                    await NoteEditor.Get.ShowUnsavedChangesDialog();
                    if (AppState.Current == State.NotSaved)
                    {
                        AppState.Set(State.Ready);
                        return;
                    }
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

                ContentFrame.Navigate(page, transitionInfo);
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
            await AppState.SetAsync(State.SearchNavigation, async () =>
            {
                using (Database context = new Database())
                {
                    if (!context.TryGetNote(sender.Text, out Note note))
                    {
                        await AppState.SetAsync(State.LoadError, async () =>
                        {
                            await App.ShowDialog("We could not find that note.", $"The note '{sender.Text}' could note be found in the database. Do you wish to create it?",
                               "Yes", () =>
                               {
                                   AppState.Set(State.NoteCreating);
                                   note = new Note()
                                   {
                                       Name = sender.Text
                                   };
                                   note.Save("", sender.Text, context);
                                   CurrentNote = note;
                                   AppState.Set(State.SearchNavigation);
                                   NavView_Navigate("edit", null);
                               },
                               "No", () =>
                               {
                                   SearchBox.Text = "";
                               });
                        });
                        
                    }
                    else
                    {
                        CurrentNote = note;
                        NavView_Navigate("reset", null);
                        NavView_Navigate("edit", null);
                    }
                }

            }, State.Ready);

        }
    }
}
