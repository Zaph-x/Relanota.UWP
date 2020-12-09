using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.IO;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace UWP.FrontEnd.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Help : Page
    {
        public Help()
        {
            this.InitializeComponent();
        }


        private async void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string fileName = (((sender as ComboBox).SelectedItem as FrameworkElement).Tag as string).ToLower();

            StorageFolder assetDir = await Package.Current.InstalledLocation.GetFolderAsync(@"Assets/");
            StorageFolder helpDir = await assetDir.GetFolderAsync(@"Help/");
            StorageFile file = await helpDir.GetFileAsync($"{fileName}.md");

            string text = File.ReadAllText(file.Path);

            text = text.Replace("{{local_dir}}", ApplicationData.Current.LocalFolder.Path);
            text = text.Replace("{{cache_dir}}", ApplicationData.Current.LocalCacheFolder.Path);

            HelpView.Text = text;
        }

        private async void MarkdownText_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            if (!Uri.IsWellFormedUriString(e.Link, UriKind.Absolute))
            {
                await new MessageDialog("Masked relative links needs to be manually handled.").ShowAsync();
            }
            else
            {
                await Launcher.LaunchUriAsync(new Uri(e.Link));
            }
        }
    }
}
