using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
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
    public sealed partial class Settings : Page
    {
        public Settings()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

            if (string.IsNullOrWhiteSpace(localSettings.Values["theme"] as string))
            {
                ThemeComboBox.SelectedIndex = 0;
            } else
            {
                ThemeComboBox.SelectedItem = ThemeComboBox.Items.First(itm => (itm as FrameworkElement).Tag as string == localSettings.Values["theme"] as string);
            }
            base.OnNavigatedTo(e);
        }

        private async void ExportDatabase_Click(object sender, RoutedEventArgs e)
        {
            FileSavePicker fileSavePicker = new FileSavePicker();
            fileSavePicker.FileTypeChoices.Add("Zip Compressed Folder (*.zip)", new List<string>() { ".zip" });
            fileSavePicker.SuggestedStartLocation = PickerLocationId.Desktop;
            fileSavePicker.SuggestedFileName = "notes";
            StorageFile file = await fileSavePicker.PickSaveFileAsync();
            if (file != null)
            {
                //file.Path
                if (File.Exists($@"{ApplicationData.Current.TemporaryFolder.Path}\export.zip"))
                {
                    File.Delete($@"{ApplicationData.Current.TemporaryFolder.Path}\export.zip");
                }
                ZipFile.CreateFromDirectory(ApplicationData.Current.LocalFolder.Path, $@"{ApplicationData.Current.TemporaryFolder.Path}\export.zip");
                IBuffer buffer = await FileIO.ReadBufferAsync(await StorageFile.GetFileFromPathAsync($@"{ApplicationData.Current.TemporaryFolder.Path}\export.zip"));

                await FileIO.WriteBufferAsync(file, buffer);

                File.Delete($@"{ApplicationData.Current.TemporaryFolder.Path}\export.zip");
                App.ShowToastNotification("Notes Exported", "Your notes were successfully exported to the chosen location.");
            }
        }

        private async void ImportDatabase_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog unsavedDialog = new ContentDialog
            {
                Title = "You are about to overwrite your current notes.",
                Content = "This action can not be undone. Continuing will permanently replace your current notes, with the notes you are importing. Do you still wish to continue?",
                PrimaryButtonText = "Yes",
                SecondaryButtonText = "No"
            };

            ContentDialogResult result = await unsavedDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                FileOpenPicker fileOpenPicker = new FileOpenPicker();
                fileOpenPicker.FileTypeFilter.Add(".zip");
                fileOpenPicker.SuggestedStartLocation = PickerLocationId.Desktop;

                StorageFile file = await fileOpenPicker.PickSingleFileAsync();

                if (file != null)
                {

                    StorageFolder temp = ApplicationData.Current.TemporaryFolder;
                    DirectoryInfo directory = new DirectoryInfo(ApplicationData.Current.LocalFolder.Path);
                    FileInfo[] files = directory.GetFiles();

                    if (File.Exists($@"{temp.Path}\notes.zip"))
                    {
                        File.Delete($@"{temp.Path}\notes.zip");
                    }
                    foreach (FileInfo fileInfo in files)
                    {
                        fileInfo.Delete();
                    }


                    using (Stream reader = await file.OpenStreamForReadAsync())
                    {
                        using (Stream writer = await (await temp.CreateFileAsync($@"notes.zip")).OpenStreamForWriteAsync())
                        {
                            reader.Seek(0, SeekOrigin.Begin);
                            reader.CopyTo(writer);
                        }
                    }

                    ZipFile.ExtractToDirectory($@"{temp.Path}\notes.zip", ApplicationData.Current.LocalFolder.Path);
                    File.Delete($@"{temp.Path}\notes.zip");
                    App.ShowToastNotification("Notes Imported", "Your notes were successfully imported.");
                }
            }



        }

        private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values["theme"] as string != ((sender as ComboBox).SelectedItem as FrameworkElement).Tag as string)
            {
                localSettings.Values["theme"] = ((sender as ComboBox).SelectedItem as FrameworkElement).Tag as string;
                MainPage.Get.SetTheme(localSettings.Values["theme"] as string);
            }
        }
    }
}
