using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace UWP.FrontEnd
{
    public class AppSettings : INotifyPropertyChanged
    {
        public bool StoreMath {
            get { return Get("MathOnDisk", true); }
            set {
                Set("MathOnDisk", value);
                NotifyPropertyChanged();
            }
        }

        public bool LoadRecentOnStartup {
            get => Get("load_recent_on_startup", false);
            set {
                Set("load_recent_on_startup", value);
                NotifyPropertyChanged();
            }
        }

        public string PreferredFont {
            get => Get("font", "Lucida Console");
            set {
                Set("font", value);
                NotifyPropertyChanged();
            }
        }

        public ApplicationDataContainer LocalSettings { get; set; }

        public AppSettings()
        {
            LocalSettings = ApplicationData.Current.LocalSettings;
        }

        private void Set<T>(string key, T value)
        {
            LocalSettings.Values[key] = value;
        }

        private T Get<T>(string key, T defaultValue)
        {
            if (LocalSettings.Values.ContainsKey(key))
            {
                return (T)LocalSettings.Values[key];
            }

            if (null != defaultValue)
            {
                return defaultValue;
            }

            return default(T);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string propName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }
}
