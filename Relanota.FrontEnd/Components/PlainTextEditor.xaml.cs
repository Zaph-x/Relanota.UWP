using Core.Objects.Wrappers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Threading;
using UWP.FrontEnd.Views;
using UWP.FrontEnd.Views.Interfaces;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using UWP.FrontEnd.Components.Helpers;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace UWP.FrontEnd.Components
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PlainTextEditor : Page, IEditorView
    {
        public NoteEditor ParentPage { get; set; }
        public string[] Lines { get => Editor.Text.Split('\r'); }

        Timer _timer;
        int _interval = 1000;
        private FixedSizeObservableCollection<(string text, int index)> History { get; set; } = new FixedSizeObservableCollection<(string text, int index)>(100);
        private FixedSizeObservableCollection<(string text, int index)> UndoneHistory { get; set; } = new FixedSizeObservableCollection<(string text, int index)>(100);

        public PlainTextEditor()
        {
            this.InitializeComponent();
            _timer = new Timer(Tick, null, _interval, Timeout.Infinite);
        }

        private async void Tick(object state)
        {
            try
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                {
                    string currentText = Editor.Text ?? "";

                    if (History.First.text == currentText)
                    {
                        return;
                    }

                    History.Insert((currentText, Editor.SelectionStart));
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

        public bool AreTextboxesEmpty()
        {
            return string.IsNullOrWhiteSpace(Editor.Text);
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

        public void InsertImage(string image)
        {
            int selectionIndex = Editor.SelectionStart;
            Editor.Text = Editor.Text.Insert(selectionIndex, image);
            Editor.SelectionStart += image.Length;
        }

        public void Update(string content)
        {
            throw new NotImplementedException();
        }

        public void InsertList()
        {
            string[] lines = Editor.Text.Split('\r');
            (_, int y) = CalculatePoint(Editor.Text, Editor.SelectionStart);
            if (!lines[y].Trim().StartsWith("*"))
            {
                lines[y] = lines[y].Insert(0, "* ");
            }
            else
            {
                lines[y] = Regex.Replace(lines[y], @"^\s*[-*>+]\s", "");
            }

            // A list denoter is always 2 characters long
            Editor.SelectionStart += 2;
            Editor.Text = string.Join('\r', lines);
        }

        private async void Editor_Paste(object sender, TextControlPasteEventArgs e)
        {
            DataPackageView dataPackageView = Clipboard.GetContent();

            // Check if data is an image
            if (dataPackageView.Contains(StandardDataFormats.Bitmap))
            {
                // Generate file name and save image
                string fileName = $"Paste {DateTime.Now:ddMMyyyy hhmmss tt}";
                await ParentPage.SaveImageToFileAsync(fileName, ApplicationData.Current.LocalFolder.Path, await dataPackageView.GetBitmapAsync());

                // Insert image into note, at current position
                string imagePath = $@"![{fileName}]({{{{local_dir}}}}\{fileName}.jpg)";
                App.SetClipboardContent(imagePath);
                InsertImage(imagePath);
            }
        }

        private void InsertTextInTextBox(TextBox textBox, string text, int newIndex)
        {
            textBox.Text = textBox.Text.Insert(textBox.SelectionStart, text);
            textBox.SelectionStart = newIndex;
        }

        private void Editor_OnPreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            bool ctrlIsPressed = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
            bool shiftIsPressed = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);

            string[] lines = Lines;

            switch (c: ctrlIsPressed, s: shiftIsPressed, k: e.OriginalKey)
            {
                case (false, false, VirtualKey.Enter):
                    if (Editor.Text.Split('\r').Any())
                    {
                        int selectionStart = Editor.SelectionStart;
                        (int x, int y) = CalculatePoint(Editor.Text, selectionStart);
                        Match match = Regex.Match(Lines[y], @"^\s*[-*>+]\s");

                        if (match.Success && x >= match.Index + match.Length)
                        {
                            e.Handled = true;

                            if (Lines[y].Trim().Length == "*".Length)
                            {
                                lines[y] = "";
                                Editor.Text = string.Join('\r', lines);
                                Editor.SelectionStart = selectionStart - match.Length;
                                return;
                            }
                            InsertTextInTextBox(Editor, $"\r{(match.Value)}", selectionStart + match.Index + match.Length + 1);
                            return;
                        }
                        match = Regex.Match(Editor.Text.Split('\r')[y], @"^\s*([1-9][0-9]*)\.\s");
                        if (match.Success && x >= match.Index + match.Length)
                        {
                            e.Handled = true;
                            int newValue = int.Parse(match.Groups[1].Value) + 1;
                            if (Lines[y].Trim().Length == match.Value.Trim().Length)
                            {
                                lines[y] = "";
                                Editor.Text = string.Join('\r', lines);
                                Editor.SelectionStart = selectionStart - match.Length;
                                return;
                            }
                            InsertTextInTextBox(Editor, $"\r{match.Value.Replace(match.Groups[1].Value, newValue.ToString())}", selectionStart + match.Index + match.Length
                                + 1 + (newValue.ToString().Length > (newValue - 1).ToString().Length ? 1 : 0));
                            return;
                        }
                    }

                    break;
                case (true, false, VirtualKey.Z):
                    if (History.First.text != Editor.Text) History.Insert((Editor.Text, Editor.SelectionStart));
                    if (History.Count > 1)
                    {
                        UndoneHistory.Insert(History.First);
                        History.RemoveFirst();
                        Editor.Text = History.First.text;
                        Editor.SelectionStart = History.First.index;
                    }
                    e.Handled = true;
                    break;
                case (true, false, VirtualKey.Y):
                    if (UndoneHistory.Any())
                    {
                        History.Insert(UndoneHistory.First);
                        UndoneHistory.RemoveFirst();
                        Editor.Text = History.First.text;
                        Editor.SelectionStart = History.First.index;
                    }
                    e.Handled = true;
                    break;
            }
        }

        private void Editor_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            (string Text, int Start, int End) formattedText;
            bool ctrlIsPressed = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
            bool shiftIsPressed = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);

            switch (c: ctrlIsPressed, s: shiftIsPressed, k: e.OriginalKey)
            {
                case (true, false, VirtualKey.I):
                    formattedText = Formatter.ApplyFormatting(Editor.Text, "*", Editor.SelectionStart, Editor.SelectionLength);

                    SetContent(formattedText.Text);
                    Editor.SelectionStart = formattedText.Start;
                    Editor.SelectionLength = formattedText.End;
                    e.Handled = true;
                    break;
                case (true, false, VirtualKey.B):
                    formattedText = Formatter.ApplyFormatting(Editor.Text, "**", Editor.SelectionStart, Editor.SelectionLength);

                    SetContent(formattedText.Text);
                    Editor.SelectionStart = formattedText.Start;
                    Editor.SelectionLength = formattedText.End;
                    e.Handled = true;
                    break;
            }
        }

        private void Editor_Loaded(object sender, RoutedEventArgs e)
        {
            PreFill(MainPage.CurrentNote?.Content ?? "");
            History.Insert((Editor.Text, Editor.SelectionStart));
        }

        public void ApplyHeaderChange(int level)
        {
            if (level < 0 || level > 6) throw new InvalidDataException("Supplied level must be a valid markdown header level");

        }


        private void Editor_SelectionChanged(object sender, RoutedEventArgs e)
        {

            int selectionStart = Editor.SelectionStart;
            (_, int y) = CalculatePoint(Editor.Text, selectionStart);

            if (Lines[y].Trim().StartsWith('#'))
            {
                Match match = Regex.Match(Lines[y].Trim(), @"^#{1,6}");
                ParentPage.SetHeaderValue( match.Groups[0].Length - 1);
            }
            else
            {
                ParentPage.SetHeaderValue(6);
            }
        }

        public Page GetEditor()
        {
            return this;
        }

        public void PreFill(string content)
        {
            Editor.TextChanged -= Editor_TextChanged;
            Editor.Text = content;
            Editor.TextChanged += Editor_TextChanged;
        }

        private void Editor_TextChanged(object sender, TextChangedEventArgs e)
        {
            ParentPage?.SetWordCount(Editor.Text);
            ParentPage?.SetSavedState(false);
        }

        public string GetContent()
        {
            return Editor.Text;
        }

        public void SetContent(string content)
        {
            Editor.Text = content;
        }

        public void FormatBold()
        {
            (string Text, int Start, int End) formattedText = Formatter.ApplyFormatting(Editor.Text, "**", Editor.SelectionStart, Editor.SelectionLength);

            SetContent(formattedText.Text);
            Editor.SelectionStart = formattedText.Start;
            Editor.SelectionLength = formattedText.End;
        }

        public void FormatItalics()
        {
            (string Text, int Start, int End) formattedText = Formatter.ApplyFormatting(Editor.Text, "*", Editor.SelectionStart, Editor.SelectionLength);

            SetContent(formattedText.Text);
            Editor.SelectionStart = formattedText.Start;
            Editor.SelectionLength = formattedText.End;
        }

        public void FormatStrikethrough()
        {
            (string Text, int Start, int End) formattedText = Formatter.ApplyFormatting(Editor.Text, "~~", Editor.SelectionStart, Editor.SelectionLength);

            SetContent(formattedText.Text);
            Editor.SelectionStart = formattedText.Start;
            Editor.SelectionLength = formattedText.End;
        }
    }
}
