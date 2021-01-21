using Core.StateHandler;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace UWP.FrontEnd.Components
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MarkdownEditor : Page
    {
        public NoteEditor ParentPage { get; set; }
        string MDContent { get; set; }
        private bool isReady { get; set; }
        public MarkdownEditor()
        {
            this.InitializeComponent();
            isReady = false;

        }

        private async void Editor_ScriptNotify(object sender, NotifyEventArgs e)
        {
            switch (e.Value)
            {
                case "change":
                    {
                        if (isReady)
                            ParentPage.SetSavedState(false);
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
    }
}
