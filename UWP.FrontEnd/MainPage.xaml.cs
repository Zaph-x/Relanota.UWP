using Core.Objects;
using Core.SqlHelper;
using Microsoft.EntityFrameworkCore;
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
        public static Database context = new Database();
        public static Note CurrentNote = null;
        public static MainPage Get { get; private set; }

        public async void ConnectDB()
        {
            try
            {
                if (!File.Exists($@"{ApplicationData.Current.LocalFolder.Path}\notes.db"))
                    await ApplicationData.Current.LocalFolder.CreateFileAsync("notes.db");
            }
            catch
            {
                // File already exists
            }
            finally
            {
                Database.path = ApplicationData.Current.LocalFolder.Path;
                context.Database.EnsureCreated();
                context.Notes.Load();
                context.NoteTags.Load();
                context.Tags.Load();
            }

        }

        public MainPage()
        {
            Get = this;
            this.InitializeComponent();
            ApplicationViewTitleBar formattableTitleBar = ApplicationView.GetForCurrentView().TitleBar;
            formattableTitleBar.ButtonBackgroundColor = Colors.Transparent;
            formattableTitleBar.ButtonForegroundColor = Colors.Black;
            CoreApplicationViewTitleBar coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;
            ConnectDB();
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

        public void NavView_Navigate(string navItemTag, NavigationTransitionInfo transitionInfo)
        {
            Type _page = null;
            Note _note = null;
            if (navItemTag == "settings")
            {
                //_page = typeof(SettingsPage);
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
                if (navItemTag == "list") { CurrentNote = null; }
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
