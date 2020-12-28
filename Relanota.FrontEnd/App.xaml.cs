using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.Toolkit.Uwp.Helpers;
using UWP.FrontEnd.Views;
using Windows.Storage;
using Microsoft.EntityFrameworkCore;
using Core.Objects;
using Microsoft.Toolkit.Uwp.Notifications;
using Windows.UI.Notifications;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Streams;
using Core;
using Core.SqlHelper;
using Core.Objects.Entities;
using Microsoft.Data.Sqlite;

namespace UWP.FrontEnd
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        private Frame rootFrame = null;
        public static bool HasLegacyDB = false;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            Constants.DATABASE_PATH = ApplicationData.Current.LocalFolder.Path;
            Constants.DATABASE_NAME = "notes.db";
            if (File.Exists($"{Constants.DATABASE_PATH}/{Constants.DATABASE_NAME}"))
            {
                using (SqliteConnection connection =
                    new SqliteConnection($"Data Source={Constants.DATABASE_PATH}/{Constants.DATABASE_NAME}"))
                {
                    connection.Open();
                    using (SqliteCommand command = connection.CreateCommand())
                    {
                        command.CommandText = @"SELECT 
    name
FROM 
    sqlite_master 
WHERE 
    type ='table' AND 
    name NOT LIKE 'sqlite_%';";
                        using (SqliteDataReader reader = command.ExecuteReader())
                        {
                            string tables = "";
                            while (reader.Read())
                            {
                                tables += $"{reader.GetString(0)};";
                            }

                            HasLegacyDB = !tables.Contains("__EFMigrationsHistory");
                        }
                    }
                }
                if (HasLegacyDB)
                {
                    MoveDatabase();
                }
            }

            this.Suspending += OnSuspending;
        }

        private void MoveDatabase()
        {
            if (File.Exists($"{Constants.DATABASE_PATH}/{Constants.DATABASE_NAME}_LEGACY")) File.Delete($"{Constants.DATABASE_PATH}/{Constants.DATABASE_NAME}_LEGACY");
            File.Move($"{Constants.DATABASE_PATH}/{Constants.DATABASE_NAME}", $"{Constants.DATABASE_PATH}/{Constants.DATABASE_NAME}_LEGACY");
            List<Note> notes = new List<Note>();
            List<Tag> tags = new List<Tag>();
            List<NoteTag> noteTags = new List<NoteTag>();
            using (SqliteConnection connection =
                new SqliteConnection($"Data Source={Constants.DATABASE_PATH}/{Constants.DATABASE_NAME}_LEGACY"))
            {
                connection.Open();
                using (SqliteCommand command = connection.CreateCommand())
                {
                    command.CommandText = @"
SELECT
    *
FROM
    Notes;";
                    using (SqliteDataReader reader = command.ExecuteReader())
                    {
                        var ordinal = new
                        {
                            Key = reader.GetOrdinal("Key"),
                            Name = reader.GetOrdinal("Name"),
                            Content = reader.GetOrdinal("Content"),
                        };
                        while (reader.Read())
                        {
                            notes.Add(new Note { Key = reader.GetInt32(ordinal.Key) ,Name = reader.GetString(ordinal.Name), Content = reader.GetString(ordinal.Content) });
                        }
                    }
                }
                using (SqliteCommand command = connection.CreateCommand())
                {
                    command.CommandText = @"
SELECT
    *
FROM
    NoteTags;";
                    using (SqliteDataReader reader = command.ExecuteReader())
                    {
                        var ordinal = new
                        {
                            NoteKey = reader.GetOrdinal("NoteKey"),
                            TagKey = reader.GetOrdinal("TagKey"),
                        };
                        while (reader.Read())
                        {
                            noteTags.Add(new NoteTag { NoteKey = reader.GetInt32(ordinal.NoteKey), TagKey = reader.GetInt32(ordinal.TagKey) });
                        }
                    }
                }
                using (SqliteCommand command = connection.CreateCommand())
                {
                    command.CommandText = @"
SELECT
    *
FROM
    Tags;";
                    using (SqliteDataReader reader = command.ExecuteReader())
                    {
                        var ordinal = new
                        {
                            Key = reader.GetOrdinal("Key"),
                            Name = reader.GetOrdinal("Name"),
                        };
                        while (reader.Read())
                        {
                            tags.Add(new Tag { Key = reader.GetInt32(ordinal.Key), Name = reader.GetString(ordinal.Name) });
                        }
                    }
                }
                using (Database context = new Database())
                {
                    context.Database.EnsureCreated();
                    context.Database.Migrate();
                    context.Notes.Load();
                    context.NoteTags.Load();
                    context.Tags.Load();

                    foreach (NoteTag noteTag in noteTags.Where(nt => notes.Any(n => n.Key == nt.NoteKey) && tags.Any(t => t.Key == nt.TagKey))) {
                        noteTag.Note = notes.FirstOrDefault(n => n.Key == noteTag.NoteKey);
                        noteTag.Tag = tags.FirstOrDefault(t => t.Key == noteTag.TagKey);
                        noteTag.Note.NoteTags.Add(noteTag);
                        noteTag.Tag.NoteTags.Add(noteTag);
                        context.NoteTags.Add(noteTag);
                    }
                    context.Notes.AddRange(notes);
                    context.Tags.AddRange(tags);
                    context.SaveChanges();
                }
            }
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }
            using (Database context = new Database())
            {
                context.Database.EnsureCreated();
                context.Database.Migrate();
                context.Notes.Load();
                context.NoteTags.Load();
                context.Tags.Load();

            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);
                }
                // Ensure the current window is active
                Window.Current.Activate();
            }

            if (AppSettings.Get("load_recent_on_startup", false))
            {
                string line = File.ReadLines($@"{ApplicationData.Current.LocalFolder.Path}\AccessList").First(); // gets the first line from file.
                using (Database context = new Database())
                {
                    if (context.TryGetNote(int.Parse(line), out Note note))
                    {
                        _ = MainPage.Get;
                        MainPage.CurrentNote = note;
                        MainPage.Get.NavView_Navigate("edit", null);
                    }
                }

            }
        }
        protected override async void OnActivated(IActivatedEventArgs args)
        {
            //base.OnActivated(args);
            if (args.Kind == ActivationKind.Protocol)
            {
                Frame rootFrame = Window.Current.Content as Frame;
                if (rootFrame == null)
                {
                    rootFrame = new Frame();
                    rootFrame.NavigationFailed += OnNavigationFailed;
                    Window.Current.Content = rootFrame;
                }
                if (rootFrame.Content == null)
                {
                    rootFrame.Navigate(typeof(MainPage));
                }
                Window.Current.Activate();

                if (args is ProtocolActivatedEventArgs eventArgs)
                {
                    try
                    {
                        switch (eventArgs.Uri.Host.ToLower())
                        {
                            case "open":
                                NoteEditor.Get.State = NoteEditorState.ProtocolNavigating;
                                await NoteEditor.Get.NavigateToNoteFromUri(eventArgs.Uri.OriginalString.Substring(0, eventArgs.Uri.OriginalString.Length));

                                MainPage.Get.NavView_Navigate("reset", null);
                                MainPage.Get.NavView_Navigate("edit", null);

                                break;

                            case "import":
                                NoteEditor.Get.State = NoteEditorState.ProtocolImportNavigation;
                                string serializedString = Uri.UnescapeDataString(eventArgs.Uri.LocalPath.Substring(1));

                                if (Note.TryDeserialize(serializedString, out Note note))
                                {
                                    MainPage.CurrentNote = note;
                                    MainPage.Get.SetNavigationIndex(3);
                                    MainPage.Get.NavView_Navigate("edit", null);
                                }
                                else
                                {
                                    ShowDialog("We could not parse that note.", $"No note could be parsed from the URI used to open Relanota. You will instead be sent to the note list.", "Okay");
                                    MainPage.Get.SetNavigationIndex(0);
                                    MainPage.Get.NavView_Navigate("list", null);
                                }

                                break;

                            default:
                                ShowDialog("We did not understand that one.", $"You opened Relanota from a link which lead to nowhere. You will instead be sent to the note list.", "Okay");
                                MainPage.Get.SetNavigationIndex(0);
                                MainPage.Get.NavView_Navigate("list", null);
                                break;
                        }
                    }
                    catch (UriFormatException)
                    {

                        ShowDialog("We did not understand that.",
                            $"You opened Relanota from a link which could not be interpreted. We managed to recover the state of the application and you will now be sent to the note list.",
                            "Okay");
                        MainPage.Get.SetNavigationIndex(0);
                        MainPage.Get.NavView_Navigate("list", null);
                    }
                }
            }

        }
        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }

        public static void ShowToastNotification(string header, string message)
        {
            ToastContent content = new ToastContentBuilder()
                .AddText(header, AdaptiveTextStyle.Base)
                .AddText(message, AdaptiveTextStyle.Body)
                .SetToastDuration(ToastDuration.Short)
                .GetToastContent();
            ToastNotification notification = new ToastNotification(content.GetXml());
            ToastNotificationManager.CreateToastNotifier().Show(notification);
        }

        public static async void ShowDialog(string title, string content, string primaryButtonText)
        {
            ContentDialog errorDialog = new ContentDialog
            {
                Title = title,
                Content = content,
                PrimaryButtonText = primaryButtonText
            };

            await errorDialog.ShowAsync();
        }



        public static async Task ShowDialog(string title, string content, string primaryButtonText, Action primaryCommand)
        {
            ContentDialog errorDialog = new ContentDialog
            {
                Title = title,
                Content = content,
                PrimaryButtonText = primaryButtonText
            };

            ContentDialogResult result = await errorDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                primaryCommand?.Invoke();
            }
        }

        public static async Task ShowDialog(string title, string content, string primaryButtonText, Action primaryCommand, Action fallback)
        {
            ContentDialog errorDialog = new ContentDialog
            {
                Title = title,
                Content = content,
                PrimaryButtonText = primaryButtonText
            };

            ContentDialogResult result = await errorDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                primaryCommand?.Invoke();
            }
            else
            {
                fallback?.Invoke();
            }
        }

        public static async Task ShowDialog(string title, string content, string primaryButtonText, Action primaryCommand, string secondaryButtonText, Action secondaryCommand)
        {
            ContentDialog errorDialog = new ContentDialog
            {
                Title = title,
                Content = content,
                PrimaryButtonText = primaryButtonText,
                SecondaryButtonText = secondaryButtonText
            };

            ContentDialogResult result = await errorDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                primaryCommand?.Invoke();
            }
            else if (result == ContentDialogResult.Secondary)
            {
                secondaryCommand?.Invoke();
            }
        }

        public static async Task ShowDialog(string title, string content, string primaryButtonText, Action primaryCommand, string secondaryButtonText, Action secondaryCommand, Action fallback)
        {
            ContentDialog errorDialog = new ContentDialog
            {
                Title = title,
                Content = content,
                PrimaryButtonText = primaryButtonText
            };

            ContentDialogResult result = await errorDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                primaryCommand?.Invoke();
            }
            else if (result == ContentDialogResult.Secondary)
            {
                secondaryCommand?.Invoke();
            }
            else
            {
                fallback?.Invoke();
            }
        }

        public static void SetClipboardContent(string content)
        {
            DataPackage dataPackage = new DataPackage();
            dataPackage.SetText(content);
            Clipboard.SetContent(dataPackage);
        }

        public static void SetClipboardContent(Uri content)
        {
            DataPackage dataPackage = new DataPackage();
            dataPackage.SetWebLink(content);
            Clipboard.SetContent(dataPackage);
        }

        public static void SetClipboardContent(RandomAccessStreamReference content)
        {
            DataPackage dataPackage = new DataPackage();
            dataPackage.SetBitmap(content);
            Clipboard.SetContent(dataPackage);
        }
    }
}
