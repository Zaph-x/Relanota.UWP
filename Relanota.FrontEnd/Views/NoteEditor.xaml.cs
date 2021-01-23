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
using Core.StateHandler;
using UWP.FrontEnd.Views.Interfaces;
using UWP.FrontEnd.Components;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace UWP.FrontEnd.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class NoteEditor : Page, IContextInteractible<Note>
    {
        private IEditorView _editor { get; set; }
        public int WordCount { get; set; } = 0;
        private AdvancedCollectionView Acv { get; set; }
        public static bool IsSaved { get; set; } = true;
        private static NoteEditor _instance { get; set; }
        public static NoteEditor Get => _instance ?? new NoteEditor();
        private IEnumerable<int> TableWidth = Enumerable.Range(1, 10);
        private IEnumerable<int> TableHeight = Enumerable.Range(1, 20);
        Timer renderTimer;
        int _interval = 1000;
        private BackgroundWorker changesWorker = new BackgroundWorker();

        public NoteEditor()
        {
            this.InitializeComponent();
            _instance = (_editor = AppSettings.Get("FancyEditor", true) ? Editor : PlainEditor as IEditorView).ParentPage = this;
            if (AppSettings.Get("FancyEditor", true))
            {

                Editor.Visibility = Visibility.Visible;
                PlainEditor.Visibility = Visibility.Collapsed;
            }
            else
            {
                Editor.Visibility = Visibility.Collapsed;
                PlainEditor.Visibility = Visibility.Visible;
                
            }

        }

        private void ChangesWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (AppState.Current != State.NotSaved)
                UnsavedChangesText.Text = "";
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

        internal void SetHeaderValue(int poundLength)
        {
            ParagraphSelector.SelectionChanged -= Header_Changed;
            ParagraphSelector.SelectedIndex = poundLength == 0 ? 6 : poundLength - 1;
            ParagraphSelector.SelectionChanged += Header_Changed;
        }

        private void FinishSave()
        {
            AppState.Set(State.SaveCompleted, () => {
                SetTagsState(true);
                SetSavedState(true);
                MainPage.Get.OnNoteSave(MainPage.CurrentNote.Name);
                MainPage.Get.SetDividerNoteName(MainPage.CurrentNote.Name);
                MainPage.Get.LogRecentAccess(MainPage.CurrentNote);
            }, State.Ready);            
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
            //History = new FixedSizeObservableCollection<(string text, int index)>(100);
            //UndoneHistory = new FixedSizeObservableCollection<(string text, int index)>(100);
            MainPage.Get.SetNavigationIndex(3);
            if (MainPage.CurrentNote != null)
            {
                NoteNameTextBox.Text = MainPage.CurrentNote.Name ?? "";
                RenderBlock.Text = $"# {NoteNameTextBox.Text}\n{Regex.Replace(MainPage.CurrentNote.Content ?? "", @"(?<!\n)\r", Environment.NewLine)}";
                SetWordCount(MainPage.CurrentNote.Content ?? "");
                if (AppState.Current  == State.ProtocolImportNavigation)
                {
                    SetSavedState(false);
                } else
                {
                    TagTokens.ItemsSource = new ObservableCollection<Tag>(MainPage.CurrentNote.NoteTags.Select(nt => nt.Tag));
                    TagTokens.IsEnabled = true;
                }
                MainPage.Get.SetDividerNoteName(MainPage.CurrentNote.Name ?? "New Note");


                using (Database context = new Database())
                {
                    if (MainPage.CurrentNote.TryGetFullNote(context, out Note note) && AppState.Current != State.ProtocolImportNavigation)
                    {
                        MainPage.Get.LogRecentAccess(note);
                    }
                }

            }
            else
            {
                SetTagsState(false);
            }
            renderTimer = new Timer(Tick, null, _interval, Timeout.Infinite);

        }

        private async void Tick(object state)
        {
            try
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
               {
                   string text = _editor.GetContent() ?? "";
                   if (text.Contains("$"))
                       text = new MathMode(text, $"{ApplicationData.Current.LocalCacheFolder.Path}").WriteComponent();

                   RenderBlock.Text = $"# {NoteNameTextBox.Text}\n" + Regex.Replace(text, @"(?<!\n)\r", Environment.NewLine);
               });
            }
            finally
            {
                try
                {
                    renderTimer?.Change(_interval, Timeout.Infinite);
                }
                catch (ObjectDisposedException)
                {
                    // Object has been disposed
                }
            }
        }

        protected override async void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            if (AppState.Current == State.NotSaved)
            {
                MainPage.CurrentNote.Name = NoteNameTextBox.Text.Trim();
                MainPage.CurrentNote.Content = _editor.GetContent().Trim();
                await ShowUnsavedChangesDialog();
                if (AppState.Current == State.NavigationCancelled)
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
            if (MainPage.CurrentNote == null && string.IsNullOrWhiteSpace(NoteNameTextBox.Text) && string.IsNullOrWhiteSpace(_editor.GetContent()))
                SetSavedState(true);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            renderTimer.Dispose();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

        public void SetWordCount(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                WordCount = 0;
            else
                WordCount = Regex.Split(text.Trim(), @"\s+").Length;
            WordCounter.Text = $"{WordCount} word" + ((WordCount != 1) ? "s" : "");
        }

        private void ShowUnsavablePrompt()
        {
            IsSaved = false;
            App.ShowDialog("We could not save that.", "The note does not seem to have a name. Please provide a name to save the note.", "Okay");
            MainPage.Get.SetNavigationIndex(3);
        }

        public void Save()
        {
            if (string.IsNullOrWhiteSpace(NoteNameTextBox.Text))
            {
                AppState.Set(State.SaveError, ShowUnsavablePrompt, State.Ready);
                return;
            }

            AppState.Set(State.Saving, () =>
            {
                using (Database context = new Database())
                {
                    if (MainPage.CurrentNote == null || MainPage.CurrentNote.Key == 0)
                    {

                        if (context.TryGetNote(NoteNameTextBox.Text, out Note note))
                        {
                            note.Update($"{note.Content}\n\n{_editor.GetContent()}", NoteNameTextBox.Text, context);
                            SetCurrentEditorNote(note);
                        }
                        else
                        {
                            MainPage.CurrentNote = new Note();
                            MainPage.CurrentNote.Save(_editor.GetContent(), NoteNameTextBox.Text, context);
                            TagTokens.IsEnabled = true;
                        }

                    }
                    else
                    {
                        MainPage.CurrentNote.Update(_editor.GetContent(), NoteNameTextBox.Text, context);
                        context.SaveChanges();
                    }
                }
            }, FinishSave);
        }

        public void SetCurrentEditorNote(Note note)
        {
            MainPage.CurrentNote = note;
            TagTokens.ItemsSource = new ObservableCollection<Tag>(MainPage.CurrentNote.NoteTags.Select(nt => nt.Tag));
            RenderBlock.Text = $"# {note.Name}\n{note.Content}";
            _editor.SetContent($"{note.Content}");
            TagTokens.IsEnabled = true;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            AppState.Set(State.Saving, Save, State.Ready);
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

                string imagePath = $@"![{file.DisplayName}]({{{{local_dir}}}}\{file.Name})";
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
                _editor.InsertImage(imagePath);
                SetSavedState(false);

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
            using (Database context = new Database())
            {
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
                await Launcher.LaunchUriAsync(new Uri(e.Link));
                // TODO implement a default handler for this
                //await new MessageDialog("Masked relative links needs to be manually handled.").ShowAsync();
            }
            else
            {
                // Handle any known protocol
                if (Uri.IsWellFormedUriString(e.Link, UriKind.Relative)) return;
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
                SecondaryButtonText = "No",
                CloseButtonText = "Cancel"
            };

            ContentDialogResult result = await unsavedDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                // Change state as user requested
                AppState.Set(State.Saving, Save, State.Ready);
            }
            else if (result == ContentDialogResult.Secondary)
            {
                // Simulate saved note to escape save loop
                SetSavedState(true);
            }
            else
            {
                MainPage.Get.SetNavigationIndex(3);
                AppState.Set(State.NavigationCancelled, null, State.Ready);
                // No action taken. Return to view
            }
        }


        public void SetSavedState(bool isSaved)
        {
            IsSaved = isSaved;
            if (isSaved)
            {
                UnsavedChangesText.Text = "Changes Saved!";
                if (!changesWorker.IsBusy)
                    changesWorker.RunWorkerAsync();
                AppState.Set(State.Ready);
            }
            else
            {
                AppState.Set(State.NotSaved);
                UnsavedChangesText.Text = "Unsaved Changes.";
                changesWorker.CancelAsync();
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
                            if (AppState.Current == State.SaveError)
                            {
                                return;
                            }
                        }
                        // Only set note name and save
                        MainPage.CurrentNote = new Note() { Name = noteName, Content = "" };
                        MainPage.Get.NavView_Navigate("reset", null);
                        MainPage.Get.NavView_Navigate("edit", null);
                        SetSavingState();
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

        public void SetSavingState() => AppState.Set(State.Saving, Save);

        private void ShareNoteButton_Click(object sender, RoutedEventArgs e)
        {
            // Quick copy content of the note. Does not have to be saved.
            App.SetClipboardContent(MainPage.CurrentNote.GetImportLink());
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

        public (int, int) CalculatePoint(string text, int currentIndex)
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

        private void EmojiButton_Click(object sender, RoutedEventArgs e)
        {
            CoreInputView.GetForCurrentView().TryShow(CoreInputViewKind.Emoji);
            _editor.GetEditor().Focus(FocusState.Programmatic);
        }

        private void Header_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
            _editor.ApplyHeaderChange(ParagraphSelector.SelectedIndex + 1);
            //string[] lines = Lines;
            //int selectionStart = EditorTextBox.SelectionStart;
            //ApplyHeaderChange(ref selectionStart, ref lines);

            //_editor.GetContent() = string.Join('\r', lines);
            //EditorTextBox.SelectionStart = selectionStart;

            //EditorTextBox.Focus(FocusState.Programmatic);

        }

        public void ApplyHeaderChange(ref int selectionStart, ref string[] lines)
        {
            (_, int y) = CalculatePoint(_editor.GetContent(), selectionStart);
            if (ParagraphSelector.SelectedIndex == 6)
            {
                Match match = Regex.Match(lines[y], @"^#{1,6} ");
                lines[y] = Regex.Replace(lines[y], @"^#{1,6} ", "");
                selectionStart -= match.Length;
            }
            else
            {

                Match match = Regex.Match(lines[y], @"^#{1,6} ");
                lines[y] = Regex.Replace(lines[y], @"^#{1,6} ", "");
                lines[y] = lines[y].Insert(0, $"{"#".Repeat(ParagraphSelector.SelectedIndex + 1)} ");
                selectionStart += $"{"#".Repeat(ParagraphSelector.SelectedIndex + 1)} ".Length - match.Length;
            }
        }

        private void ListButton_OnClick(object sender, RoutedEventArgs e)
        {
            _editor.InsertList();
        }

        

        private void FormatButton_OnClick(object sender, RoutedEventArgs e)
        {
            string formatter = (sender as AppBarButton)?.Tag as string;
            switch (formatter)
            {
                case "*":
                    _editor.FormatItalics();
                    break;
                case "**":
                    _editor.FormatBold();

                    break;
                case "~~":
                    _editor.FormatStrikethrough();
                    break;
            }
            
        }

        public Note Load(int key)
        {
            throw new NotImplementedException();
        }

        public void Update(string content)
        {
            Render(content);
        }

        #region RENDER LOGIC

        private void Render(string content )
        {
            RenderBlock.Text = content;
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
                        if (!File.Exists($@"{ApplicationData.Current.LocalCacheFolder.Path}\{cachefilename}.jpg") && AppSettings.Get("MathOnDisk", true))
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
        #endregion
    }
}
