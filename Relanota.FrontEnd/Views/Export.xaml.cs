﻿using Core.Interfaces;
using Core.Objects;
using Core.Objects.DocumentTypes;
using Core.Objects.Entities;
using Core.Test.Objects.DocumentTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Toolkit.Uwp.UI;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Core.SqlHelper;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace UWP.FrontEnd.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Export : Page
    {
        private AdvancedCollectionView Acv { get; set; }

        public Export()
        {
            this.InitializeComponent();
            using (Database context = new Database()) {
                Acv = new AdvancedCollectionView(context.Tags.ToList(), false);
                Acv.SortDescriptions.Add(new SortDescription(nameof(Core.Objects.Entities.Tag.Name),
                    SortDirection.Ascending));
                Acv.Filter = itm =>
                    !TagTokens.Items.Contains(itm) &&
                    (itm as Tag).Name.Contains(TagTokens.Text, StringComparison.InvariantCultureIgnoreCase);
                TagTokens.ItemsSource = new ObservableCollection<Tag>();
                TagTokens.SuggestedItemsSource = Acv;
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (MainPage.CurrentNote == null)
            {
                OnlyThisCheckbox.Visibility = Visibility.Collapsed;
                OnlyThisText.Visibility = Visibility.Collapsed;
            }
        }

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            List<Note> notes = null;
            using (Database context = new Database()) {
                if (OnlyThisCheckbox.IsChecked ?? false) {
                    notes = new List<Note>() {MainPage.CurrentNote};
                }
                else if (TagTokens.Items.Count == 1) {
                    notes = context.Notes.Include(n => n.NoteTags).ThenInclude(n => n.Tag).ToList();
                }
                else {
                    notes = context.Notes.Include(n => n.NoteTags).ThenInclude(nt => nt.Tag).ToList();
                    notes = notes.Where(note => note.NoteTags.Select(nt => nt.Tag).Intersect(TagTokens.Items).Any())
                        .ToList();
                }
            }

            FileSavePicker fileSavePicker = new FileSavePicker();
            string type = (FileFormatPicker.SelectedItem as FrameworkElement).Tag as string;
            List<string> fileTypes = new List<string>() { type };

            fileSavePicker.FileTypeChoices.Add((FileFormatPicker.SelectedItem as ComboBoxItem).Content as string, fileTypes);
            fileSavePicker.SuggestedStartLocation = PickerLocationId.Desktop;
            fileSavePicker.SuggestedFileName = "notes";
            StorageFile file = await fileSavePicker.PickSaveFileAsync();
            if (file != null)
            {
                CachedFileManager.DeferUpdates(file);
#pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
                IDocumentType document = (type.ToLower()) switch
                {
#pragma warning restore CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
                    ".txt" => new TxtDocument(notes),
                    ".md" => new MdDocument(notes)
                };
                await FileIO.WriteTextAsync(file, document.ConvertNotes(), Windows.Storage.Streams.UnicodeEncoding.Utf8);
                FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(file);
                if (status != FileUpdateStatus.Complete)
                {
                    App.ShowToastNotification("The notes file could not be saved", "An error occured, and we cannot save the note.");
                    return;
                }
                //                BufferedStream stream = await file.OpenStreamForWriteAsync() as BufferedStream;
                //                stream.Flush();
                //document.Export(stream);
            }
        }

        private void TagTokens_TokenItemAdding(TokenizingTextBox sender, TokenItemAddingEventArgs args)
        {
            // If the tag exists, use it. Otherwise cancel the event.
            using (Database context = new Database()) {
                if (context.Tags.Local.Any(tag =>
                    tag.Name.Equals(args.TokenText, StringComparison.InvariantCultureIgnoreCase))) {
                    Tag tag = context.Tags.Local.First(tag =>
                        tag.Name.Equals(args.TokenText, StringComparison.InvariantCultureIgnoreCase));
                    args.Item = tag;

                    args.Cancel = false;
                    return;
                }
            }

            args.Cancel = true;
        }

        private void TagTokens_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            // Make sure the event is fired because of the user
            if (args.CheckCurrent() && args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                Acv.RefreshFilter();
            }
        }

        private void OnlyThisCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            ByTagText.Visibility = Visibility.Collapsed;
            TagTokens.Visibility = Visibility.Collapsed;
        }

        private void OnlyThisCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            ByTagText.Visibility = Visibility.Visible;
            TagTokens.Visibility = Visibility.Visible;
        }
    }
}
