using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace FrontEnd
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private Configuration config { get; set; }
        public SettingsWindow(Configuration config)
        {
            this.config = config;
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
    }
}
