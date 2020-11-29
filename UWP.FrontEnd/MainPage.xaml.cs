using Core.Objects.Entities;
using Core.Objects.Wrappers;
using Microsoft.Toolkit.Uwp.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using UWP.FrontEnd.Views;
using Windows.ApplicationModel.Core;
using Windows.UI;
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
        private FixedSizeObservableCollection<Note> recentlyAccessed = new FixedSizeObservableCollection<Note>(10);


        public MainPage()
        {
            Get = this;
            recentlyAccessed.CollectionChanged += RecentlyAccessed_CollectionChanged;
            this.InitializeComponent();
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


        private void RecentlyAccessed_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            int indexOfSpacer = 7; // TODO make this dynamic
            int length = NavigationView.MenuItems.Count - indexOfSpacer;

            if (e.NewItems != null)
            {
                foreach (Note note in e.NewItems)
                {
                    for (int i = indexOfSpacer; i < NavigationView.MenuItems.Count; i++)
                    {
                        if ((NavigationView.MenuItems[i] as NavigationViewItemBase).Content == note.Name) NavigationView.MenuItems.RemoveAt(i--);
                    }
                    NavigationViewItem navigationViewItem = new NavigationViewItem
                    {
                        Tag = note,
                        Content = note.Name,
                        Icon = new SymbolIcon(Symbol.Page2),
                    };
                    NavigationView.MenuItems.Insert(indexOfSpacer + 1, navigationViewItem);
                }
            }

            if (e.NewItems?.Count > 0)
            {
                // Recalculate the lengt of of the navview
                length = NavigationView.MenuItems.Count-1 - indexOfSpacer;
                if (length > recentlyAccessed.Size)
                {
                    NavigationView.MenuItems.RemoveAt(NavigationView.MenuItems.Count - 1);
                }
            }
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

        private async void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
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
                    Note tagNote = (navItemTag as Note);
                    if (tagNote.Name != (CurrentNote?.Name ?? "")) NavView_Navigate("list", args.RecommendedNavigationTransitionInfo);
                    SetNavigationIndex(3);
                    if (tagNote.IsInContext(App.Context) && (navItemTag as Note).TryGetFullNote(App.Context, out Note note))
                    {
                        CurrentNote = note;
                        NoteEditor.Get.State = NoteEditorState.RecentNavigation;
                        NavView_Navigate("edit", args.RecommendedNavigationTransitionInfo);
                        return;
                    }
                    else
                    {
                        ContentDialog errorDialog = new ContentDialog
                        {
                            Title = "We could not find that note.",
                            Content = $"The note '{(navItemTag as Note).Name}' could note be found in the database.",
                            PrimaryButtonText = "Okay"

                        };
                        await errorDialog.ShowAsync();

                        recentlyAccessed.Remove((navItemTag as Note));
                        NavigationView.MenuItems.Remove(args.InvokedItemContainer);

                    }
                }
                NavView_Navigate(navItemTag.ToString(), args.RecommendedNavigationTransitionInfo);
            }
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

            if (!(_page is null) && !Type.Equals(preNavPageType, _page))
            {
                if (!NoteEditor.IsSaved)
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
                ContentDialog errorDialog = new ContentDialog
                {
                    Title = "We could not find that note.",
                    Content = $"The note '{sender.Text}' could note be found in the database. Do you wish to create it?",
                    PrimaryButtonText = "Yes",
                    CloseButtonText = "No"
                };
                ContentDialogResult result = await errorDialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    note = new Note()
                    {
                        Name = sender.Text
                    };
                    note.Save("", sender.Text, App.Context);
                } else
                {
                    SearchBox.Text = "";
                    return;
                }

            }
            CurrentNote = note;
            NavView_Navigate("edit", null);
        }
    }
}
