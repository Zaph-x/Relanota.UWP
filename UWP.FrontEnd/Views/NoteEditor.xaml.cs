using Core.Objects;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Core.ExtensionClasses;
using Core.Macros;
using Windows.UI;
using Windows.UI.Xaml.Documents;
using Microsoft.Toolkit.Uwp.UI;
using System.Collections.ObjectModel;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml.Input;
using System.Drawing;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Streams;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace UWP.FrontEnd.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class NoteEditor : Page
    {
        private int WordCount { get; set; } = 0;
        private AdvancedCollectionView Acv { get; set; }
        public static bool IsSaved { get; set; } = true;
        private static NoteEditor _instance { get; set; }
        public static NoteEditor Get => _instance ?? new NoteEditor();


        public NoteEditor()
        {
            _instance = this;
            this.InitializeComponent();
            if (MainPage.CurrentNote == null) { TagTokens.IsEnabled = false; }
            Acv = new AdvancedCollectionView(MainPage.context.Tags.ToList(), false);
            Acv.SortDescriptions.Add(new SortDescription(nameof(Core.Objects.Tag.Name), SortDirection.Ascending));
            Acv.Filter = itm => !TagTokens.Items.Contains(itm) && (itm as Tag).Name.Contains(TagTokens.Text, StringComparison.InvariantCultureIgnoreCase);
            TagTokens.SuggestedItemsSource = Acv;
        }

        public async Task SaveImageToFileAsync(string fileName, string path, Uri uri)
        {
            using (var http = new HttpClient())
            {

                HttpResponseMessage response = await http.GetAsync(uri);
                if (response.StatusCode != HttpStatusCode.OK) return;
                FileInfo fileInfo = new FileInfo($"{path}\\{fileName}.jpg");
                StorageFolder storageFolder = await StorageFolder.GetFolderFromPathAsync(path);
                StorageFile storageFile = await storageFolder.CreateFileAsync($"{fileName}.jpg");

                if (storageFile == null)
                    return;

                using (Stream ms = await response.Content.ReadAsStreamAsync())
                {
                    using (FileStream fs = File.Create(fileInfo.FullName))
                    {
                        ms.Seek(0, SeekOrigin.Begin);
                        ms.CopyTo(fs);
                    }

                }
            }

        }

        public async Task SaveImageToFileAsync(string fileName, string path, RandomAccessStreamReference stream)
        {
            FileInfo fileInfo = new FileInfo($"{path}\\{fileName}.jpg");
            StorageFolder storageFolder = await StorageFolder.GetFolderFromPathAsync(path);
            StorageFile storageFile = await storageFolder.CreateFileAsync($"{fileName}.jpg");

            if (storageFile == null)
                return;

            using (FileStream fs = File.Create(fileInfo.FullName))
            {
                Stream baseStream = (await stream.OpenReadAsync()).AsStream();
                baseStream.Seek(0, SeekOrigin.Begin);
                baseStream.CopyTo(fs);
            }



        }
        // [note](note://bayesian networks)
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            MainPage.Get.SetNavigationIndex(3);
            if (MainPage.CurrentNote != null)
            {
                EditorTextBox.TextChanged -= NoteEditorTextBox_TextChanged;
                EditorTextBox.Text = MainPage.CurrentNote.Content ?? "";
                RenderBlock.Text = MainPage.CurrentNote.Content ?? "";
                NoteNameTextBox.Text = MainPage.CurrentNote.Name ?? "";
                EditorTextBox.TextChanged += NoteEditorTextBox_TextChanged;
                MainPage.Get.SetDividerNoteName(MainPage.CurrentNote.Name ?? "New Note");
                TagTokens.ItemsSource = new ObservableCollection<Tag>(MainPage.CurrentNote.NoteTags.Select(nt => nt.Tag));
                TagTokens.IsEnabled = true;
                NoteEditorTextBox_TextChanged(this.EditorTextBox, null);
                SetSavedState(true);
            }
        }

        protected override async void OnNavigatedFrom(NavigationEventArgs e)
        {

        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void NoteEditorTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string text = ((TextBox)sender).Text;

            text = new MathMode(text, $"{ApplicationData.Current.LocalCacheFolder.Path}").WriteComponent();


            RenderBlock.Text = $"# {NoteNameTextBox.Text}\n" + Regex.Replace(text, @"(?<!\n)\r", Environment.NewLine);

            if (string.IsNullOrWhiteSpace(text))
                WordCount = 0;
            else
                WordCount = Regex.Split(text.Trim(), @"\s+").Length;
            WordCounter.Text = $"{WordCount} words.";
            if (e != null && IsSaved)
            {
                SetSavedState(false);
            }
        }

        private async Task SaveAsync()
        {
            if (string.IsNullOrWhiteSpace(NoteNameTextBox.Text))
            {
                IsSaved = false;
                ContentDialog unsavedDialog = new ContentDialog
                {
                    Title = "We could not save that.",
                    Content = "The note does not seem to have a name. Please provide a name to save the note.",
                    PrimaryButtonText = "Okay"
                };

                await unsavedDialog.ShowAsync();
                MainPage.Get.SetNavigationIndex(3);
                return;
            }
            if (MainPage.CurrentNote == null)
            {
                MainPage.CurrentNote = new Note();
                MainPage.CurrentNote.Save(EditorTextBox.Text, NoteNameTextBox.Text, MainPage.context);
                TagTokens.IsEnabled = true;
            }
            else
            {
                MainPage.CurrentNote.Update(EditorTextBox.Text, NoteNameTextBox.Text, MainPage.context);
            }
            if (!IsSaved)
            {
                SetSavedState(true);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveAsync();
        }
        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
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
                MainPage.CurrentNote.Delete(MainPage.context, MainPage.Get.ShowMessageBox);
                SetSavedState(true);
                MainPage.Get.NavView_Navigate("list", null);
                MainPage.Get.SetNavigationIndex(0);
            }
            else
            {
                // The user clicked the CLoseButton, pressed ESC, Gamepad B, or the system back button.
                // Do nothing.
            }
        }
        private void NewNoteButton_Click(object sender, RoutedEventArgs e)
        {

        }
        private async void ImportPictureButton_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker fileOpenPicker = new FileOpenPicker();
            fileOpenPicker.FileTypeFilter.Add(".jpg");
            fileOpenPicker.FileTypeFilter.Add(".jpeg");
            fileOpenPicker.FileTypeFilter.Add(".png");
            fileOpenPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;

            StorageFile file = await fileOpenPicker.PickSingleFileAsync();
            if (file != null)
            {
                // Application now has read/write access to the picked file
                try
                {
                    file = await file.CopyAsync(ApplicationData.Current.LocalCacheFolder);
                }
                catch
                {

                }
                string imagePath = $@"![{file.DisplayName}]({{{{local_dir}}}}\{file.Name})";
                var selectionIndex = EditorTextBox.SelectionStart;
                EditorTextBox.Text = EditorTextBox.Text.Insert(selectionIndex, imagePath);
                EditorTextBox.SelectionStart = selectionIndex + imagePath.Length;
                SetSavedState(false);

            }
        }

        private async void MarkdownText_OnImageResolving(object sender, ImageResolvingEventArgs e)
        {
            // This is basically the default implementation

            try
            {
                if (e.Url.ToLower().StartsWith("http"))
                {
                    string url = e.Url.Replace("(", "%28").Replace(")", "%29");
                    BitmapImage image = new BitmapImage(new Uri(url));
                    e.Image = image;

                    string cachefilename = url.CreateMD5();
                    try
                    {
                        if (!File.Exists($@"{ApplicationData.Current.LocalCacheFolder.Path}\{cachefilename}.jpg"))
                            await SaveImageToFileAsync(cachefilename, ApplicationData.Current.LocalCacheFolder.Path, new Uri(url));
                    }
                    catch
                    {
                        // Do nothing
                    }
                }
                else
                {
                    string path = e.Url.Replace("{{cache_dir}}", ApplicationData.Current.LocalCacheFolder.Path);
                    path = e.Url.Replace("{{local_dir}}", ApplicationData.Current.LocalFolder.Path);
                    e.Image = new BitmapImage(new Uri(path));
                }
            }
            catch (Exception ex)
            {
                e.Handled = false;
                return;
            }

            e.Handled = true;
        }

        private void RenderBlock_CodeBlockResolving(object sender, CodeBlockResolvingEventArgs e)
        {
            if (e.CodeLanguage == "CUSTOM")
            {
                e.Handled = true;
                e.InlineCollection.Add(new Run { Foreground = new SolidColorBrush(Colors.Red), Text = e.Text, FontWeight = FontWeights.Bold });
            }

        }

        private void TagTokens_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.CheckCurrent() && args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                Acv.RefreshFilter();
            }
        }

        private void TagTokens_TokenItemAdding(TokenizingTextBox sender, TokenItemAddingEventArgs args)
        {
            Console.WriteLine(args);
            if (MainPage.context.Tags.Local.Any(tag => tag.Name.Equals(args.TokenText, StringComparison.InvariantCultureIgnoreCase)))
            {
                Tag tag = MainPage.context.Tags.Local.First(tag => tag.Name.Equals(args.TokenText, StringComparison.InvariantCultureIgnoreCase));
                args.Item = tag;
                MainPage.CurrentNote.AddTag(tag, MainPage.context);
            }
            else
            {
                Tag tag = new Tag();
                tag.Name = args.TokenText;
                args.Item = tag;
                MainPage.CurrentNote.AddTag(tag, MainPage.context);
            }
            SetSavedState(false);

            args.Cancel = false;
        }

        private void TagTokens_TokenItemRemoving(TokenizingTextBox sender, TokenItemRemovingEventArgs args)
        {
            MainPage.CurrentNote.RemoveTag(args.Item as Tag, MainPage.context);
            SetSavedState(false);

        }

        private void TagTokens_TokenItemAdded(TokenizingTextBox sender, object data)
        {
            if (data is Tag tag)
            {
                MainPage.CurrentNote.AddTag(tag, MainPage.context);
            }
            SetSavedState(false);
        }

        private async void MarkdownText_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            if (e.Link.ToLower().StartsWith("note://"))
            {
                await NavigateToNoteFromUri(e.Link);

            }
            else if (!Uri.IsWellFormedUriString(e.Link, UriKind.Absolute))
            {
                //await new MessageDialog("Masked relative links needs to be manually handled.").ShowAsync();
            }
            else
            {
                await Launcher.LaunchUriAsync(new Uri(e.Link));
            }
        }

        private void EditorTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            CoreVirtualKeyStates ctrlState = CoreWindow.GetForCurrentThread().GetKeyState(VirtualKey.Control);
            bool isCtrlDown = ctrlState == CoreVirtualKeyStates.Down;
            if (!isCtrlDown || e.Key != VirtualKey.I)
            {
                // We don't want to block anything if it's not CTRL+I
                return;
            }
            ImportPictureButton_Click(null, null);
            e.Handled = true;
        }

        public static async Task ShowUnsavedChangesDialog()
        {
            ContentDialog unsavedDialog = new ContentDialog
            {
                Title = "You have unsaved changes.",
                Content = "Do you wish to save these changes, before navigating? Picking No will delete these changes permanently.",
                PrimaryButtonText = "Yes",
                SecondaryButtonText = "No"
            };

            ContentDialogResult result = await unsavedDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                await Get.SaveAsync();
            }
            else if (result == ContentDialogResult.Secondary)
            {
                Get.SetSavedState(true);
            }
            else
            {
                return;
            }
        }

        private void SetSavedState(bool isSaved)
        {
            IsSaved = isSaved;
            UnsavedChangesText.Text = isSaved ? "" : "Unsaved Changes.";
        }

        private async void EditorTextBox_Paste(object sender, TextControlPasteEventArgs e)
        {
            TextBox tb = sender as TextBox;
            DataPackageView dataPackageView = Clipboard.GetContent();
            if (dataPackageView.Contains(StandardDataFormats.Bitmap))
            {
                e.Handled = true;

                string fileName = $"Paste {DateTime.Now:ddMMyyyy hhmmss tt}";

                await SaveImageToFileAsync(fileName, ApplicationData.Current.LocalFolder.Path, await dataPackageView.GetBitmapAsync());

                string imagePath = $@"![{fileName}]({{{{local_dir}}}}\{fileName}.jpg)";
                var selectionIndex = tb.SelectionStart;
                tb.Text = tb.Text.Insert(selectionIndex, imagePath);
                tb.SelectionStart = selectionIndex + imagePath.Length;
            }
        }

        public async Task NavigateToNoteFromUri(string uri)
        {
            if (!IsSaved)
            {
                await ShowUnsavedChangesDialog();
            }
            string noteName = Uri.UnescapeDataString(uri.Substring(12));
            File.AppendAllText($@"{ApplicationData.Current.LocalFolder.Path}\log.txt", $"{uri}");
            if (MainPage.context.TryGetNote(noteName, true, out Note note))
            {
                MainPage.CurrentNote = note;
                
                OnNavigatedTo(null);
            }
            else
            {
                ContentDialog errorDialog = new ContentDialog
                {
                    Title = "We could not find that note.",
                    Content = $"The note '{noteName}' could note be found in the database.",
                    PrimaryButtonText = "Okay"
                };
                await errorDialog.ShowAsync();
                MainPage.Get.NavView_Navigate("list", null);
            }
        }
    }
}
