using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Core.Interfaces;
using Core.Objects;
using Core.Objects.DocumentTypes;
using Core.SqlHelper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;

namespace FrontEnd
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private Configuration config { get; set; }
        private Database context { get; set; }
        public SettingsWindow(Configuration config, Database context)
        {
            this.config = config;
            this.context = context;
            InitializeComponent();
            FontPicker.SelectedValue = config.AppSettings.Settings["DefaultFont"].Value;
            DevCheck.IsChecked = config.AppSettings.Settings["Mode"].Value == "DEV";
            TabCheck.IsChecked = bool.Parse(config.AppSettings.Settings["TabToSpace"].Value);
            TabSizePicker.SelectedValue = config.AppSettings.Settings["TabSize"].Value;

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            config.AppSettings.Settings["DefaultFont"].Value = (FontPicker.SelectedItem as ComboBoxItem).Content.ToString();

            config.AppSettings.Settings["Mode"].Value = DevCheck.IsChecked ?? false ? "DEV" : "Normal";

            config.AppSettings.Settings["TabToSpace"].Value = (TabCheck.IsChecked ?? false).ToString();

            config.AppSettings.Settings["TabSize"].Value = TabSizePicker.SelectedValue.ToString();

            config.Save(ConfigurationSaveMode.Modified);


            this.Close();
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            IDocumentType doc;
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.OverwritePrompt = true;
            dialog.CreatePrompt = false;
            dialog.DefaultExt = "*.txt";
            dialog.Filter = "Text File (*.txt)|*.txt";
            dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            List<Note> notes = context.Notes
                .Include(note => note.NoteTags)
                .ThenInclude(nt => nt.Tag).ToList();
            if (dialog.ShowDialog() != true) {
                return;
            }

            switch (dialog.FileName.Split(".").Last().ToLower()) {
                case "txt":
                    doc = new TxtDocument(notes);
                    doc.Export(new FileStream(dialog.FileName, FileMode.CreateNew));
                    break;
            }
        }
    }
}
