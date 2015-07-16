using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace WcfLiveTraceViewer
{
    public class AppData : INotifyPropertyChanged
    {
        private bool selected;

        public bool Selected
        {
            get
            {
                return selected;
            }
            set
            {
                selected = value;
                OnPropertyChanged(new PropertyChangedEventArgs("Selected"));
            }
        }
        public string Path { get; set; }
        public string Name { get; set; }
        public string ConfigName { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, e);
        }

        public AppData(string name)
        {
            Name = name;
        }

        public AppData(string name, bool selected)
        {
            Name = name;
            Selected = selected;
        }

        public AppData(string name, string path, string configName)
        {
            Name = name;
            Path = path;
            ConfigName = configName;
            Selected = false;
        }
    }
}
