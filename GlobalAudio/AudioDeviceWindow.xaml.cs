using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using GlobalAudio.Services;

namespace GlobalAudio
{
    public partial class AudioDeviceWindow : Window
    {
        private List<SelectableAudioDevice> _devices = new List<SelectableAudioDevice>();

        public List<int> SelectedDeviceNumbers { get; private set; } = new List<int>();
        public Dictionary<int, float> DeviceVolumes { get; private set; } = new Dictionary<int, float>();

        public AudioDeviceWindow(List<int> currentlySelectedDevices, Dictionary<int, float> currentDeviceVolumes)
        {
            InitializeComponent();

            // Load all available devices
            var allDevices = AudioPlayer.GetAudioDevices();

            foreach (var device in allDevices)
            {
                float volume = currentDeviceVolumes.ContainsKey(device.DeviceNumber)
                    ? currentDeviceVolumes[device.DeviceNumber]
                    : 1.0f;

                _devices.Add(new SelectableAudioDevice
                {
                    DeviceNumber = device.DeviceNumber,
                    Name = device.Name,
                    IsSelected = currentlySelectedDevices.Contains(device.DeviceNumber),
                    Volume = volume
                });
            }

            DeviceListView.ItemsSource = _devices;
        }

        private void Device_CheckedChanged(object sender, RoutedEventArgs e)
        {
            // Ensure at least one device is selected
            if (!_devices.Any(d => d.IsSelected))
            {
                MessageBox.Show("At least one output device must be selected.", "No Devices Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);

                // Re-check the first device
                if (_devices.Count > 0)
                {
                    _devices[0].IsSelected = true;
                }
            }
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            SelectedDeviceNumbers = _devices
                .Where(d => d.IsSelected)
                .Select(d => d.DeviceNumber)
                .ToList();

            // Collect volume settings only for selected devices
            DeviceVolumes = _devices
                .Where(d => d.IsSelected)
                .ToDictionary(d => d.DeviceNumber, d => d.Volume);

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    public class SelectableAudioDevice : INotifyPropertyChanged
    {
        private bool _isSelected;
        private float _volume = 1.0f;

        public int DeviceNumber { get; set; }
        public string Name { get; set; } = string.Empty;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        public float Volume
        {
            get => _volume;
            set
            {
                _volume = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(VolumePercent));
            }
        }

        public string VolumePercent => $"{(int)(_volume * 100)}%";

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
