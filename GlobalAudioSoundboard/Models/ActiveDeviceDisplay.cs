using System.ComponentModel;

namespace GlobalAudioSoundboard.Models
{
    public class ActiveDeviceDisplay : INotifyPropertyChanged
    {
        private string _name = "";
        private float _volume;

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }

        public float Volume
        {
            get => _volume;
            set { _volume = value; OnPropertyChanged(nameof(Volume)); OnPropertyChanged(nameof(VolumeDisplay)); }
        }

        public string VolumeDisplay => $"({(int)(_volume * 100)}%)";

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
