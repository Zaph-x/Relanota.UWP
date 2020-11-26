using Core.Objects;
using Core.SqlHelper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using UWP.FrontEnd.Views;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Notifications;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

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

        public static Note CurrentNote = null;
        public static Tag CurrentTag = null;
        public static MainPage Get { get; private set; }

        

        public MainPage()
        {
            Get = this;
            this.InitializeComponent();
            ApplicationViewTitleBar formattableTitleBar = ApplicationView.GetForCurrentView().TitleBar;
            formattableTitleBar.ButtonBackgroundColor = Colors.Transparent;
            if (!IsDarkTheme)
                formattableTitleBar.ButtonForegroundColor = Colors.Black;
            else {
                formattableTitleBar.ButtonForegroundColor = Colors.WhiteSmoke;
            }
            CoreApplicationViewTitleBar coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;
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
                var navItemTag = args.InvokedItemContainer.Tag.ToString();
                NavView_Navigate(navItemTag, args.RecommendedNavigationTransitionInfo);
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
    }
}
