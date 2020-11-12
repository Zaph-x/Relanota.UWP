using Core.Objects;
using Microsoft.Toolkit.Uwp.Helpers;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Core.ExtensionClasses;
using Core.Macros;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace UWP.FrontEnd.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class NoteEditor : Page
    {
        public NoteEditor()
        {

            this.InitializeComponent();
        }

        public async Task SaveImageToFileAsync(string fileName, string path, Uri uri)
        {
            using (var http = new HttpClient())
            {

                var response = await http.GetAsync(uri);
                response.EnsureSuccessStatusCode();
                var fileInfo = new FileInfo($"{path}\\{fileName}.jpg");
                StorageFolder storageFolder = await StorageFolder.GetFolderFromPathAsync(path);
                StorageFile storageFile = await storageFolder.CreateFileAsync($"{fileName}.jpg");

                if (storageFile == null)
                    return;

                using (var ms = await response.Content.ReadAsStreamAsync())
                {
                    using (FileStream fs = File.Create(fileInfo.FullName))
                    {
                        ms.Seek(0, SeekOrigin.Begin);
                        ms.CopyTo(fs);
                    }

                }
            }

        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            MainPage.Get.SetNavigationIndex(3);
            if (MainPage.CurrentNote != null)
            {
                EditorTextBox.Text = MainPage.CurrentNote.Content;
                RenderBlock.Text = MainPage.CurrentNote.Content;
                NoteNameTextBox.Text = MainPage.CurrentNote.Name;
                MainPage.CurrentNote = MainPage.CurrentNote;
                MainPage.Get.SetDividerNoteName(MainPage.CurrentNote.Name);
            }
            NoteEditorTextBox_TextChanged(this.EditorTextBox, null);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {

        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void NoteEditorTextBox_TextChanged(object sender, RoutedEventArgs e)
        {
            string text = ((TextBox)sender).Text;

            text = new MathMode(text).Process($"{ApplicationData.Current.LocalFolder.Path}");

            //    {
            //        string encodedString = HttpUtility.UrlEncode(match.Groups[1].Value.Replace(" ", ""))
            //            .Replace("(", "%28")
            //            .Replace(")", "%29");
            //        string md5 = ("https://latex.codecogs.com/png.latex?" + encodedString).CreateMD5();
            //        if (!File.Exists(ApplicationData.Current.LocalFolder.Path + @"\" + md5 + ".jpg"))
            //        {
            //            if (match.Groups[1].Value.Length > 0)
            //                text = text.Replace(match.Groups[0].Value, "![latex math](https://latex.codecogs.com/png.latex?" + encodedString + ")");
            //        }
            //        else
            //        {
            //            text = text.Replace(match.Groups[0].Value, $"![cached image]({ApplicationData.Current.LocalFolder.Path + "\\" + md5}.jpg)");
            //        }

            //    }
            RenderBlock.Text = text;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainPage.CurrentNote == null)
            {
                MainPage.CurrentNote = new Note() { Content = EditorTextBox.Text, Name = NoteNameTextBox.Text };
                MainPage.context.Add(MainPage.CurrentNote);
                MainPage.context.SaveChangesAsync();
            }
            else
            {
                MainPage.CurrentNote.Update(EditorTextBox.Text, NoteNameTextBox.Text, MainPage.context);
            }
        }
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {

        }
        private void NewNoteButton_Click(object sender, RoutedEventArgs e)
        {

        }
        private void ImportPictureButton_Click(object sender, RoutedEventArgs e)
        {

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

                    await SaveImageToFileAsync(cachefilename, ApplicationData.Current.LocalFolder.Path, new Uri(url));

                    //image.UriSource;
                    //SaveImageToFileAsync(CreateMD5(e.Url), (BitmapImage)e.Image, ApplicationData.Current.LocalFolder.Path);
                }
                else
                {
                    e.Image = new BitmapImage(new Uri(e.Url));
                }
            }
            catch (Exception ex)
            {
                e.Handled = false;
                return;
            }

            e.Handled = true;
        }
    }
}
