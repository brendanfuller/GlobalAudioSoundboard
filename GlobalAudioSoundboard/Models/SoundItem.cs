using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GlobalAudioSoundboard.Models
{
    public class SoundItem : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private string _filePath = string.Empty;
        private TimeSpan _duration;
        private float _volume = 1.0f;
        private string _hotkey = string.Empty;
        private int _hotkeyModifiers;
        private int _hotkeyVirtualKey;
        private bool _isPlaying;
        private string? _playbackId;
        private TimeSpan _startTime = TimeSpan.Zero;
        private TimeSpan _endTime = TimeSpan.Zero;

        public string Id { get; set; } = Guid.NewGuid().ToString();

        public bool IsPlaying
        {
            get => _isPlaying;
            set { _isPlaying = value; OnPropertyChanged(); OnPropertyChanged(nameof(PlayButtonText)); }
        }

        public string? PlaybackId
        {
            get => _playbackId;
            set { _playbackId = value; OnPropertyChanged(); }
        }

        public string PlayButtonText => IsPlaying ? "■" : "▶";

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public string FilePath
        {
            get => _filePath;
            set { _filePath = value; OnPropertyChanged(); }
        }

        public TimeSpan Duration
        {
            get => _duration;
            set { _duration = value; OnPropertyChanged(); OnPropertyChanged(nameof(DurationString)); }
        }

        public string DurationString => Duration.ToString(@"mm\:ss");

        public float Volume
        {
            get => _volume;
            set { _volume = Math.Clamp(value, 0f, 1f); OnPropertyChanged(); OnPropertyChanged(nameof(VolumePercent)); }
        }

        public string VolumePercent => $"{(int)(_volume * 100)}%";

        public string Hotkey
        {
            get => _hotkey;
            set { _hotkey = value; OnPropertyChanged(); }
        }

        public int HotkeyModifiers
        {
            get => _hotkeyModifiers;
            set { _hotkeyModifiers = value; OnPropertyChanged(); }
        }

        public int HotkeyVirtualKey
        {
            get => _hotkeyVirtualKey;
            set { _hotkeyVirtualKey = value; OnPropertyChanged(); }
        }

        public TimeSpan StartTime
        {
            get => _startTime;
            set { _startTime = value; OnPropertyChanged(); }
        }

        public TimeSpan EndTime
        {
            get => _endTime;
            set { _endTime = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
