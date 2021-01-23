using Core.StateHandler;
using Microsoft.Toolkit.Uwp.UI.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
using Core.ExtensionClasses;
using UWP.FrontEnd.Components.Helpers;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace UWP.FrontEnd.Components
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MarkdownEditor : Page, IEditorView
    {
        public NoteEditor ParentPage { get; set; }
        string MDContent { get; set; }
        private bool isReady { get; set; }
        public string EditorContent { get; set; }

        ThemeListener Listener = new ThemeListener();
        public MarkdownEditor()
        {
            this.InitializeComponent();
            isReady = false;

            Listener.ThemeChanged += Listener_ThemeChanged;

        }

        private async void Listener_ThemeChanged(ThemeListener sender)
        {
            string theme = sender.CurrentTheme == ApplicationTheme.Dark ? "monokai" : "default";
            //MainPage.IsDarkTheme = sender.CurrentTheme == ApplicationTheme.Dark;
            await Editor.InvokeScriptAsync("setTheme", new string[] { theme });
        }

        private async void Editor_ScriptNotify(object sender, NotifyEventArgs e)
        {
            switch (e.Value)
            {
                case "change":
                    {
                        if (isReady && this.Visibility == Visibility.Visible && (((int)AppState.Current >> 14) != 1))
                            ParentPage?.SetSavedState(false);
                        OnCodeContentChanged();
                        break;
                    }
                case "paste":
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
                        break;
                    }
                case "save":
                    {
                        AppState.Set(State.Saving, ParentPage.Save, State.Ready);
                        break;
                    }
                case "move":
                    {
                        string coords = await Editor.InvokeScriptAsync("getCursorCoords", null);
                        (int line, int offset) = (int.Parse(coords.Split('%')[0]), int.Parse(coords.Split('%')[1]));
                        string[] lines = MDContent.Split('\n');
                        ParentPage.SetHeaderValue(Regex.Match(lines[line], @"^#{1,6}").Length);
                        break;
                    }
            }

        }

        private async void OnCodeContentChanged()
        {
            MDContent = await Editor.InvokeScriptAsync("getContent", null);
            if (this.Visibility == Visibility.Visible)
                ParentPage.Update(MDContent);
        }

        public async void InsertImage(string path)
        {
            await Editor.InvokeScriptAsync("insertTextAtCursor", new string[] { path });
        }

        private void Editor_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
        {

        }

        private async void Editor_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            Update(MainPage.CurrentNote?.Content ?? "");
            string theme = MainPage.IsDarkTheme ? "monokai" : "default";
            await sender.InvokeScriptAsync("setTheme", new string[] { theme });
            await sender.InvokeScriptAsync("setFontFamily", new string[] { AppSettings.Get("font", "Lucida Console") });
            await sender.InvokeScriptAsync("setLineWrapping", new string[] { "true" });
            isReady = true;
        }

        public async void Update(string content)
        {
            await Editor.InvokeScriptAsync("setContent", new string[] { content });
        }

        public bool AreTextboxesEmpty()
        {
            return string.IsNullOrWhiteSpace(MDContent);
        }

        public async void InsertList()
        {
            string coords = await Editor.InvokeScriptAsync("getCursorCoords", null);
            (int line, int offset) = (int.Parse(coords.Split('%')[0]), int.Parse(coords.Split('%')[1]));
            string[] lines = MDContent.Split('\n');
            lines[line] = lines[line].Insert(0, "* ");
            MDContent = string.Join('\n', lines);
            Update(MDContent);
            await Editor.InvokeScriptAsync("setLocation", new string[] { line.ToString(), offset.ToString() });
        }

        public async void ApplyHeaderChange(int level)
        {

            if (level < 1 || level > 7) throw new InvalidDataException("Supplied level must be a valid markdown header level");
            MDContent = await Editor.InvokeScriptAsync("getContent", null);
            string coords = await Editor.InvokeScriptAsync("getCursorCoords", null);

            (int line, int offset) = (int.Parse(coords.Split('%')[0]), int.Parse(coords.Split('%')[1]));
            string[] lines = MDContent.Split('\n');
            if (level == 7)
            {
                Match match = Regex.Match(lines[line], @"^#{1,6} ");
                lines[line] = Regex.Replace(lines[line], @"^#{1,6} ", "");
                offset -= match.Length;
            }
            else
            {

                Match match = Regex.Match(lines[line], @"^#{1,6} ");
                lines[line] = Regex.Replace(lines[line], @"^#{1,6} ", "");
                lines[line] = lines[line].Insert(0, $"{"#".Repeat(level)} ");
                offset += $"{"#".Repeat(level)} ".Length - match.Length;
            }

            MDContent = string.Join('\n', lines);

            await Editor.InvokeScriptAsync("setContent", new string[] { MDContent });

            await Editor.InvokeScriptAsync("setLocation", new string[] { line.ToString(), offset.ToString() });
        }

        public Page GetEditor()
        {
            return this;
        }

        public string GetContent()
        {
            return MDContent;
        }

        public async void SetContent(string content)
        {
            await Editor.InvokeScriptAsync("setContent", new string[] { content });
        }

        public async void FormatBold()
        {
            await Editor.InvokeScriptAsync("applyFormatting", new[] { "**" });

        }

        public async void FormatItalics()
        {
            await Editor.InvokeScriptAsync("applyFormatting", new[] { "*" });
        }

        public async void FormatStrikethrough()
        {
            await Editor.InvokeScriptAsync("applyFormatting", new[] { "~~" });
        }

        
    }
}
