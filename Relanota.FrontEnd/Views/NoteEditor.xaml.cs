using Core.Objects;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections.Generic;
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
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Streams;
using System.Text;
using System.Threading;
using Core.Objects.Entities;
using System.ComponentModel;
using Windows.UI.ViewManagement.Core;
using Core.Objects.Wrappers;
using Core.SqlHelper;

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
        public string NoteContent => EditorTextBox?.Text.Trim() ?? "";
        public string NoteName => NoteNameTextBox?.Text.Trim() ?? "";
        private static NoteEditorState _state { get; set; }
        public NoteEditorState State {
            get => _state;
            set {
                switch (value)
                {
                    case NoteEditorState.Ready:
                        _state = value;
                        break;
                    case NoteEditorState.Saving:
                        Save();
                        break;
                    case NoteEditorState.SaveCompleted:
                        SetTagsState(true);
                        SetSavedState(true);
                        MainPage.Get.OnNoteSave(MainPage.CurrentNote.Name);
                        MainPage.Get.SetDividerNoteName(MainPage.CurrentNote.Name);
                        MainPage.Get.LogRecentAccess(MainPage.CurrentNote);
                        break;
                    case NoteEditorState.SaveError:
                        ShowUnsavablePrompt();
                        SetState(NoteEditorState.NotSaved);
                        _state = value;
                        break;
                    case NoteEditorState.Loading:
                        break;
                    case NoteEditorState.LoadError:
                        break;
                    case NoteEditorState.RecentNavigation:
                        _state = value;
                        break;
                    case NoteEditorState.WorkerCanceled:
                        _state = value;
                        break;
                    case NoteEditorState.ProtocolNavigating:
                        _state = value;
                        break;
                    case NoteEditorState.ListNavigation:
                        _state = value;
                        break;
                    case NoteEditorState.NotSaved:
                        SetSavedState(false);
                        break;
                    default:
                        _state = value;
                        break;

                }
            }
        }
        Timer _timer;
        int _interval = 1000;
        BackgroundWorker changesWorker = new BackgroundWorker();
        private FixedSizeObservableCollection<(string text, int index)> History { get; set; } = new FixedSizeObservableCollection<(string text, int index)>(100);
        private FixedSizeObservableCollection<(string text, int index)> UndoneHistory { get; set; } = new FixedSizeObservableCollection<(string text, int index)>(100);

        public NoteEditor()
        {
            _instance = this;

            this.InitializeComponent();
        }

        public bool AreTextboxesEmpty() => string.IsNullOrWhiteSpace(NoteNameTextBox.Text) && string.IsNullOrWhiteSpace(EditorTextBox.Text);

        private void ChangesWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (State != NoteEditorState.WorkerCanceled)
                UnsavedChangesText.Text = "";
            SetState(NoteEditorState.Ready);
        }

        private void ChangesWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            for (int i = 0; i < 10; i++)
            {
                if (changesWorker.CancellationPending)
                    break;
                Thread.Sleep(200);
            }
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

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            IsSaved = true;
            changesWorker.DoWork += ChangesWorker_DoWork;
            changesWorker.RunWorkerCompleted += ChangesWorker_RunWorkerCompleted;
            changesWorker.WorkerSupportsCancellation = true;

            if (MainPage.CurrentNote == null) { TagTokens.IsEnabled = false; }


            using (Database context = new Database())
            {
                Acv = new AdvancedCollectionView(context.Tags.ToList(), false);
                Acv.SortDescriptions.Add(new SortDescription(nameof(Core.Objects.Entities.Tag.Name), SortDirection.Ascending));
                Acv.Filter = itm => !TagTokens.Items.Contains(itm) && (itm as Tag).Name.Contains(TagTokens.Text, StringComparison.InvariantCultureIgnoreCase);

            }
            TagTokens.SuggestedItemsSource = Acv;
            History = new FixedSizeObservableCollection<(string text, int index)>(100);
            UndoneHistory = new FixedSizeObservableCollection<(string text, int index)>(100);
            MainPage.Get.SetNavigationIndex(3);
            if (MainPage.CurrentNote != null)
            {
                EditorTextBox.TextChanged -= NoteEditorTextBox_TextChanged;
                EditorTextBox.Text = MainPage.CurrentNote.Content ?? "";
                RenderBlock.Text = $"# {NoteNameTextBox.Text}\n{Regex.Replace(MainPage.CurrentNote.Content ?? "", @"(?<!\n)\r", Environment.NewLine)}";
                NoteNameTextBox.Text = MainPage.CurrentNote.Name ?? "";
                EditorTextBox.TextChanged += NoteEditorTextBox_TextChanged;
                MainPage.Get.SetDividerNoteName(MainPage.CurrentNote.Name ?? "New Note");
                TagTokens.ItemsSource = new ObservableCollection<Tag>(MainPage.CurrentNote.NoteTags.Select(nt => nt.Tag));
                TagTokens.IsEnabled = true;
                using (Database context = new Database())
                {
                    if (MainPage.CurrentNote.TryGetFullNote(context, out Note note) && State != NoteEditorState.RecentNavigation)
                    {
                        MainPage.Get.LogRecentAccess(note);
                    }
                }

                FillRenderbox(this.EditorTextBox);
            }
            else
            {
                SetTagsState(false);
            }
            History.Insert((EditorTextBox.Text, EditorTextBox.SelectionStart));
            _timer = new Timer(Tick, null, _interval, Timeout.Infinite);
            SetState(NoteEditorState.Ready);
        }

        private async void Tick(object state)
        {
            try
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
               {
                   string text = EditorTextBox.Text ?? "";
                   if (EditorTextBox.Text.Contains("$"))
                       text = new MathMode(text, $"{ApplicationData.Current.LocalCacheFolder.Path}").WriteComponent();

                   RenderBlock.Text = $"# {NoteNameTextBox.Text}\n" + Regex.Replace(text, @"(?<!\n)\r", Environment.NewLine);
               });
                await Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                {
                    string currentText = EditorTextBox.Text ?? "";

                    if (History.First.text == currentText)
                    {
                        return;
                    }

                    History.Insert((currentText, EditorTextBox.SelectionStart));
                    UndoneHistory.Clear();
                });

            }
            finally
            {
                try
                {
                    _timer?.Change(_interval, Timeout.Infinite);
                }
                catch (ObjectDisposedException)
                {
                    // Object has been disposed
                }
            }
        }

        protected override async void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            if (!IsSaved) {
                MainPage.CurrentNote.Name = NoteNameTextBox.Text.Trim();
                MainPage.CurrentNote.Content = EditorTextBox.Text.Trim();
            }
            if (MainPage.CurrentNote == null && string.IsNullOrWhiteSpace(NoteNameTextBox.Text) && string.IsNullOrWhiteSpace(EditorTextBox.Text))
                SetSavedState(true);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {

            _timer.Dispose();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

        private void FillRenderbox(TextBox sender)
        {
            string text = sender.Text;

            if (string.IsNullOrWhiteSpace(text))
                WordCount = 0;
            else
                WordCount = Regex.Split(text.Trim(), @"\s+").Length;
            WordCounter.Text = $"{WordCount} word" + ((WordCount != 1) ? "s" : "");
        }

        private void NoteEditorTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FillRenderbox(sender as TextBox);
            if ((State ^ NoteEditorState.Navigation) == NoteEditorState.NotSaved || State == NoteEditorState.Ready)
            {
                SetSavedState(false);
            }
        }

        private async void ShowUnsavablePrompt()
        {
            IsSaved = false;
            await App.ShowDialog("We could not save that.", "The note does not seem to have a name. Please provide a name to save the note.", "Okay");
            MainPage.Get.SetNavigationIndex(3);
        }

        private void Save()
        {
            if (string.IsNullOrWhiteSpace(NoteNameTextBox.Text))
            {
                State = NoteEditorState.SaveError;
                return;
            }

            using (Database context = new Database())
            {
                if (MainPage.CurrentNote == null || MainPage.CurrentNote.Key == 0)
                {

                    if (context.TryGetNote(NoteNameTextBox.Text, out Note note))
                    {
                        note.Update($"{note.Content}\n\n{EditorTextBox.Text}", NoteNameTextBox.Text, context);
                        SetCurrentEditorNote(note);
                    }
                    else
                    {
                        MainPage.CurrentNote = new Note();
                        MainPage.CurrentNote.Save(EditorTextBox.Text, NoteNameTextBox.Text, context);
                        TagTokens.IsEnabled = true;
                    }

                }
                else
                {
                    MainPage.CurrentNote.Update(EditorTextBox.Text, NoteNameTextBox.Text, context);
                    context.SaveChanges();
                }
            }

            SetState(NoteEditorState.SaveCompleted);
        }

        private void SetCurrentEditorNote(Note note)
        {
            MainPage.CurrentNote = note;
            TagTokens.ItemsSource = new ObservableCollection<Tag>(MainPage.CurrentNote.NoteTags.Select(nt => nt.Tag));
            RenderBlock.Text = $"# {note.Name}\n{note.Content}";
            EditorTextBox.Text = $"{note.Content}";
            TagTokens.IsEnabled = true;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SetState(NoteEditorState.Saving);
        }
        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            await App.ShowDialog("Delete note permanently?", "If you delete this note, you won't be able to recover it. Do you want to delete it?", "Delete", () =>
            {
                using (Database context = new Database())
                {


                    // The user chose to delete the note
                    MainPage.CurrentNote.Delete(context, App.ShowToastNotification);
                    SetSavedState(true);
                    MainPage.Get.NavView_Navigate("list", null);
                    MainPage.Get.SetNavigationIndex(0);
                }
            },
            "Cancel", null);
        }
        private void NewNoteButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void ImportPictureButton_Click(object sender, RoutedEventArgs e)
        {
            // Set up file picker
            FileOpenPicker fileOpenPicker = new FileOpenPicker();
            fileOpenPicker.FileTypeFilter.Add(".jpg");
            fileOpenPicker.FileTypeFilter.Add(".jpeg");
            fileOpenPicker.FileTypeFilter.Add(".png");
            fileOpenPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;

            StorageFile file = await fileOpenPicker.PickSingleFileAsync();

            // Make sure a file was actually created
            if (file != null)
            {
                // Application now has read/write access to the picked file
                try
                {
                    // Copy it to the local directory
                    file = await file.CopyAsync(ApplicationData.Current.LocalFolder, file.Name, NameCollisionOption.GenerateUniqueName);
                    await file.RenameAsync(file.Name.Replace(" ", "_"));
                }
                catch
                {

                }
                // Insert reference into the textbox and set save state to false
                string imagePath = $@"![{file.DisplayName}]({{{{local_dir}}}}\{file.Name})";
                var selectionIndex = EditorTextBox.SelectionStart;
                EditorTextBox.Text = EditorTextBox.Text.Insert(selectionIndex, imagePath);
                // Make sure cursor is at the end of the image insert
                EditorTextBox.SelectionStart = selectionIndex + imagePath.Length;
                SetSavedState(false);

            }
        }

        private async void MarkdownText_OnImageResolving(object sender, ImageResolvingEventArgs e)
        {
            // This is basically the default implementation

            try
            {
                // check if the image is from online. Find on disk
                if (e.Url.ToLower().StartsWith("http"))
                {
                    // Fix urls
                    string url = e.Url.Replace("(", "%28").Replace(")", "%29");
                    // Get Bitmap from URL
                    BitmapImage image = new BitmapImage(new Uri(url));

                    e.Image = image;

                    // Generate name for local storage
                    string cachefilename = url.CreateMD5();
                    try
                    {
                        // Download if the file does not exist
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
                    // Make sure we convert long file names to smaller shorter ones
                    string path = e.Url.Replace("{{cache_dir}}", ApplicationData.Current.LocalCacheFolder.Path);
                    path = path.Replace("{{local_dir}}", ApplicationData.Current.LocalFolder.Path);
                    e.Image = new BitmapImage(new Uri(path));
                }
            }
            catch (Exception)
            {
                e.Handled = false;
                return;
            }

            e.Handled = true;
        }

        private void RenderBlock_CodeBlockResolving(object sender, CodeBlockResolvingEventArgs e)
        {
            // TODO: Add more language support later
            if (e.CodeLanguage == "CUSTOM")
            {
                e.Handled = true;
                e.InlineCollection.Add(new Run { Foreground = new SolidColorBrush(Colors.Red), Text = e.Text, FontWeight = FontWeights.Bold });
            }

        }

        private void TagTokens_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            // Make sure the event is fired because of the user
            if (args.CheckCurrent() && args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                Acv.RefreshFilter();
            }
        }

        private void TagTokens_TokenItemAdding(TokenizingTextBox sender, TokenItemAddingEventArgs args)
        {
            using (Database context = new Database())
            {
                // If the tag exists, use it. Otherwise create a new tag.
                if (context.Tags.AsEnumerable().Any(tag =>
                    tag.Name.Equals(args.TokenText, StringComparison.InvariantCultureIgnoreCase)))
                {
                    Tag tag = context.Tags.AsEnumerable().First(tag =>
                        tag.Name.Equals(args.TokenText, StringComparison.InvariantCultureIgnoreCase));
                    args.Item = tag;
                }
                else
                {
                    Tag tag = new Tag();
                    tag.Name = args.TokenText;
                    args.Item = tag;
                }
            }

            SetSavedState(false);

            args.Cancel = false;
        }

        private void TagTokens_TokenItemRemoving(TokenizingTextBox sender, TokenItemRemovingEventArgs args)
        {
            using (Database context = new Database()) {
                Tag tag = args.Item as Tag;
                MainPage.CurrentNote.RemoveTag(tag, context);
                
            }
            SetSavedState(false);
        }

        private void TagTokens_TokenItemAdded(TokenizingTextBox sender, object data)
        {
            // Only act on tag objects
            if (data is Tag tag)
            {
                using (Database context = new Database())
                    MainPage.CurrentNote.AddTag(tag, context);
            }
            SetSavedState(false);
        }

        private async void MarkdownText_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            if (!Uri.IsWellFormedUriString(e.Link, UriKind.Absolute))
            {
                // TODO implement a default handler for this
                //await new MessageDialog("Masked relative links needs to be manually handled.").ShowAsync();
            }
            else
            {
                // Handle any known protocol
                await Launcher.LaunchUriAsync(new Uri(e.Link));
            }
        }

        public async Task ShowUnsavedChangesDialog()
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
                // Change state as user requested
                SetState(NoteEditorState.Saving);
            }
            else if (result == ContentDialogResult.Secondary)
            {
                // Simulate saved note to escape save loop
                SetSavedState(true);
            }
            else
            {
                // No action taken. Return to view
            }
        }


        private void SetSavedState(bool isSaved)
        {
            IsSaved = isSaved;
            if (isSaved)
            {
                UnsavedChangesText.Text = "Changes Saved!";
                if (!changesWorker.IsBusy)
                    changesWorker.RunWorkerAsync();
                SetState(NoteEditorState.Ready);
            }
            else
            {
                UnsavedChangesText.Text = "Unsaved Changes.";
                changesWorker.CancelAsync();
                SetState(NoteEditorState.WorkerCanceled);
            }
        }

        private async void EditorTextBox_Paste(object sender, TextControlPasteEventArgs e)
        {
            // Get the target text box and data package
            TextBox tb = sender as TextBox;
            DataPackageView dataPackageView = Clipboard.GetContent();

            // Check if data is an image
            if (dataPackageView.Contains(StandardDataFormats.Bitmap))
            {
                // Generate file name and save image
                string fileName = $"Paste {DateTime.Now:ddMMyyyy hhmmss tt}";
                await SaveImageToFileAsync(fileName, ApplicationData.Current.LocalFolder.Path, await dataPackageView.GetBitmapAsync());

                // Insert image into note, at current position
                string imagePath = $@"![{fileName}]({{{{local_dir}}}}\{fileName}.jpg)";
                App.SetClipboardContent(imagePath);
                int selectionIndex = tb.SelectionStart;
                tb.Text = tb.Text.Insert(selectionIndex, imagePath);
                tb.SelectionStart = selectionIndex + imagePath.Length;

                e.Handled = true;
            }
        }

        public async Task NavigateToNoteFromUri(string uri)
        {
            // Check if the note has been saved.
            if (!IsSaved)
            {
                await ShowUnsavedChangesDialog();
            }
            // We have note checked if the note is saved. We can continue
            string noteName = Uri.UnescapeDataString(uri.Substring(12));

            using (Database context = new Database())
                if (context.TryGetNote(noteName, out Note note))
                {
                    // The note was found. We must now fill the view.
                    MainPage.CurrentNote = note;
                    OnNavigatedTo(null);
                }
                else
                {
                    ContentDialog errorDialog = new ContentDialog
                    {
                        Title = "We could not find that note.",
                        Content = $"The note '{noteName}' could note be found in the database. Do you wish to create it?",
                        PrimaryButtonText = "Yes",
                        CloseButtonText = "No"
                    };
                    ContentDialogResult result = await errorDialog.ShowAsync();

                    if (result == ContentDialogResult.Primary)
                    {
                        // Always check if the note is saved
                        if (!IsSaved)
                        {
                            await ShowUnsavedChangesDialog();
                            if (State == NoteEditorState.SaveError)
                            {
                                return;
                            }
                        }
                        // Only set note name and save
                        MainPage.CurrentNote = new Note() { Name = noteName, Content = "" };
                        MainPage.Get.NavView_Navigate("reset", null);
                        MainPage.Get.NavView_Navigate("edit", null);
                        SetState(NoteEditorState.Saving);
                        MainPage.Get.LogRecentAccess(MainPage.CurrentNote);
                        OnNavigatedTo(null);
                    }
                    else
                    {
                        // The user specified they don't want to create the note. Therefore we should go to the note list
                        MainPage.Get.NavView_Navigate("list", null);
                    }

                }
        }

        private void ShareNoteButton_Click(object sender, RoutedEventArgs e)
        {
            // Quick copy content of the note. Does not have to be saved.
            App.SetClipboardContent($"note://import/|{Convert.ToBase64String(Encoding.Default.GetBytes(NoteNameTextBox.Text))}|{Convert.ToBase64String(Encoding.Default.GetBytes(EditorTextBox.Text))}|");
            App.ShowToastNotification("Note copied!", "A sharable link has been copied to your clipboard.");
        }

        private void SetTagsState(bool activate)
        {
            TagTokens.PlaceholderText = activate ? "Enter Tags" : "Save the note to enter tags";
        }

        private void CopyLinkButton_Click(object sender, RoutedEventArgs e)
        {
            // We only want to get a link if there is a saved note open
            if (MainPage.CurrentNote != null)
            {
                App.SetClipboardContent($"note://open/{Uri.EscapeDataString(MainPage.CurrentNote.Name)}");
                App.ShowToastNotification("Link copied!", "A link to the current note has been copied.");
            }
            else
            {
                App.ShowToastNotification("Could not link to note!", "You can only link to notes stored in the note database.");
            }
        }

        private void CollapsePreviewPane_Click(object sender, RoutedEventArgs e)
        {
            RenderColumn.Width = new GridLength(RenderColumn.MinWidth);
            EditorColumn.Width = new GridLength(1, GridUnitType.Star);
        }

        private void CollapseEditorPane_Click(object sender, RoutedEventArgs e)
        {
            EditorColumn.Width = new GridLength(EditorColumn.MinWidth);
            RenderColumn.Width = new GridLength(1, GridUnitType.Star);
        }

        private void GridSplitter_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            e.Handled = true;
            EditorColumn.Width = new GridLength(1, GridUnitType.Star);
            RenderColumn.Width = new GridLength(1, GridUnitType.Star);
        }

        public static void SetState(NoteEditorState state)
        {
            Get.State = state;
        }

        private void EditorTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            int newIndex = -1;
            string newText = EditorTextBox.Text;
            bool ctrlIsPressed = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
            bool shiftIsPressed = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);

            switch (c: ctrlIsPressed, s: shiftIsPressed, k: e.OriginalKey)
            {
                case (true, false, VirtualKey.I):
                    newText = newText.Insert(EditorTextBox.SelectionStart, "*");
                    newText = newText.Insert(EditorTextBox.SelectionStart + EditorTextBox.SelectionLength + 1, "*");
                    if (EditorTextBox.SelectionStart + EditorTextBox.SelectionLength == 0)
                    {
                        newIndex = 1;
                    }
                    else
                    {
                        newIndex = EditorTextBox.SelectionStart + EditorTextBox.SelectionLength + 1;
                    }
                    EditorTextBox.Text = newText;
                    EditorTextBox.SelectionStart = newIndex;
                    e.Handled = true;
                    break;
                case (true, false, VirtualKey.B):
                    newText = newText.Insert(EditorTextBox.SelectionStart, "**");
                    newText = newText.Insert(EditorTextBox.SelectionStart + EditorTextBox.SelectionLength + 2, "**");
                    if (EditorTextBox.SelectionStart + EditorTextBox.SelectionLength == 0)
                    {
                        newIndex = 2;
                    }
                    else
                    {
                        newIndex = EditorTextBox.SelectionStart + EditorTextBox.SelectionLength + 2;
                    }
                    EditorTextBox.Text = newText;
                    EditorTextBox.SelectionStart = newIndex;
                    e.Handled = true;
                    break;
            }
        }

        private (int, int) CalculatePoint(string text, int currentIndex)
        {
            List<string> lines = text.Split('\r').Select(str => str = $"{str}\r").ToList();
            int lineCount = 0;
            int indexCount = 0;
            if (lines.Count == 1) return (currentIndex, 0);
            while (lineCount < lines.Count && currentIndex >= 0)
            {
                indexCount = lines[lineCount].Length;
                currentIndex -= indexCount;

                lineCount++;
            }

            return (indexCount, lineCount - 1);
        }

        private void EditorTextBox_OnPreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            bool ctrlIsPressed = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
            bool shiftIsPressed = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);

            switch (c: ctrlIsPressed, s: shiftIsPressed, k: e.OriginalKey)
            {
                case (false, false, VirtualKey.Enter):
                    if (EditorTextBox.Text.Split('\r').Any())
                    {
                        int selectionStart = EditorTextBox.SelectionStart;
                        (int x, int y) point = CalculatePoint(EditorTextBox.Text, selectionStart);
                        string[] lines = EditorTextBox.Text.Split('\r');
                        Match match = Regex.Match(lines[point.y], @"^\s*[-*>+]\s");

                        if (match.Success && point.x >= match.Index + match.Length)
                        {
                            e.Handled = true;

                            if (lines[point.y].Length == match.Length)
                            {
                                InsertTextInTextBox(EditorTextBox, $"\r",
                                    selectionStart + match.Index + match.Length + 1);
                                return;
                            }
                            InsertTextInTextBox(EditorTextBox, $"\r{(match.Value)}", selectionStart + match.Index + match.Length + 1);
                            return;
                        }
                        match = Regex.Match(EditorTextBox.Text.Split('\r')[point.y], @"^\s*([1-9][0-9]*)\.\s");
                        if (match.Success && point.x >= match.Index + match.Length)
                        {
                            e.Handled = true;
                            int newValue = int.Parse(match.Groups[1].Value) + 1;
                            if (lines[point.y].Length == match.Length)
                            {
                                InsertTextInTextBox(EditorTextBox, "\r", selectionStart + match.Index + match.Length
                                                                         + 1 + (newValue.ToString().Length > (newValue - 1).ToString().Length ? 1 : 0));
                                return;
                            }
                            InsertTextInTextBox(EditorTextBox, $"\r{match.Value.Replace(match.Groups[1].Value, newValue.ToString())}", selectionStart + match.Index + match.Length
                                + 1 + (newValue.ToString().Length > (newValue - 1).ToString().Length ? 1 : 0));
                            return;
                        }
                    }

                    break;
                case (true, false, VirtualKey.Z):
                    if (History.First.text != EditorTextBox.Text) History.Insert((EditorTextBox.Text, EditorTextBox.SelectionStart));
                    if (History.Count > 1)
                    {
                        UndoneHistory.Insert(History.First);
                        History.RemoveFirst();
                        EditorTextBox.Text = History.First.text;
                        EditorTextBox.SelectionStart = History.First.index;
                    }
                    e.Handled = true;
                    break;
                case (true, false, VirtualKey.Y):
                    if (UndoneHistory.Any())
                    {
                        History.Insert(UndoneHistory.First);
                        UndoneHistory.RemoveFirst();
                        EditorTextBox.Text = History.First.text;
                        EditorTextBox.SelectionStart = History.First.index;
                    }
                    e.Handled = true;
                    break;
            }
        }

        private void InsertTextInTextBox(TextBox textBox, string text, int newIndex)
        {
            textBox.Text = textBox.Text.Insert(textBox.SelectionStart, text);
            textBox.SelectionStart = newIndex;
        }

        private void EmojiButton_Click(object sender, RoutedEventArgs e) {
            CoreInputView.GetForCurrentView().TryShow(CoreInputViewKind.Emoji);
            EditorTextBox.Focus(FocusState.Programmatic);
        }
    }

    [Flags]
    public enum NoteEditorState
    {
        Error = 0b_0000,
        Ready = 0b_0001,
        Saving = 0b_0010,
        Loading = 0b_0100,
        SaveCompleted = 0b_1000,
        SaveError = 0b_0001_0000,
        LoadError = 0b_0010_0000,
        NotSaved = 0b_0000_0011_0000,
        ProtocolNavigating = 0b_0000_0100_0000,
        ListNavigation = 0b_0000_1100_0000,
        RecentNavigation = 0b_0000_1000_0000,
        WorkerCanceled = 0b_0001_0000_0000,
        Navigation = 0b_0010_1100_0000,
        ProtocolImportNavigation = 0b_0010_1111_0000,
        SearchNavigation = 0b_0001_1100_0000


        // Error states
    }
}
