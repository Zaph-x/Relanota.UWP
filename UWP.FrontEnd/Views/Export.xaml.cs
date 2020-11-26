using Core.Interfaces;
using Core.Objects;
using Core.Objects.DocumentTypes;
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
    public sealed partial class Export : Page
    {
        private AdvancedCollectionView _acv { get; set; }

        List<Tag> filterTags = new List<Tag>();
        public Export()
        {
            this.InitializeComponent();
            _acv = new AdvancedCollectionView(App.context.Tags.ToList(), false);
            _acv.SortDescriptions.Add(new SortDescription(nameof(Core.Objects.Tag.Name), SortDirection.Ascending));
            _acv.Filter = itm => !TagTokens.Items.Contains(itm) && (itm as Tag).Name.Contains(TagTokens.Text, StringComparison.InvariantCultureIgnoreCase);
            TagTokens.ItemsSource = new ObservableCollection<Tag>();
        }

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            List<Note> notes = null;
            if (!filterTags.Any())
            {
                notes = App.context.Notes.Include(n => n.NoteTags).ThenInclude(n => n.Tag).ToList();
            }
            else
            {
                notes = App.context.Notes.Include(n => n.NoteTags)
                    .ThenInclude(n => n.Tag)
                    .Where(n => n.NoteTags
                        .Select(nt => nt.Tag)
                        .Intersect(filterTags)
                        .Any())
                    .ToList();
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
                Stream stream = await file.OpenStreamForWriteAsync();
#pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
                IDocumentType document = (type.ToLower()) switch {
#pragma warning restore CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
                    ".txt" => new TxtDocument(notes),
                    ".md" => new MdDocument(notes)
                };
                document.Export(stream);
            }
        }

        private void TagTokens_TokenItemAdding(TokenizingTextBox sender, TokenItemAddingEventArgs args)
        {
            if (App.context.Tags.Contains(args.Item as Tag))
            {
                filterTags.Add(args.Item as Tag);
                return;
            }
            args.Cancel = true;
        }
    }
}
