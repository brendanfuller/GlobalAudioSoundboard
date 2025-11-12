using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using GlobalAudioSoundboard.Models;
using GlobalAudioSoundboard.Services;
using NAudio.Wave;

namespace GlobalAudioSoundboard
{
    public partial class WaveformEditor : Window
    {
        private SoundItem _sound;
        private AudioPlayer _audioPlayer = new AudioPlayer();
        private float[] _waveformData = Array.Empty<float>();
        private bool _isDraggingStart = false;
        private bool _isDraggingEnd = false;
        private TimeSpan _originalStartTime;
        private TimeSpan _originalEndTime;
        private string? _currentPlaybackId;
        private DispatcherTimer? _positionTimer;

        public TimeSpan StartTime { get; private set; }
        public TimeSpan EndTime { get; private set; }

        public WaveformEditor(SoundItem sound)
        {
            InitializeComponent();
            _sound = sound;

            TitleText.Text = $"Edit: {sound.Name}";
            DurationText.Text = $"Total Duration: {sound.Duration:mm\\:ss\\.fff}";

            // Store original values
            _originalStartTime = sound.StartTime;
            _originalEndTime = sound.EndTime == TimeSpan.Zero ? sound.Duration : sound.EndTime;

            StartTime = _originalStartTime;
            EndTime = _originalEndTime;

            // Set initial textbox values
            StartTimeTextBox.Text = FormatTimeSpan(StartTime);
            EndTimeTextBox.Text = FormatTimeSpan(EndTime);

            ThemeManager.ApplyTheme(this);

            // Setup position update timer
            _positionTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16) // Update ~60 times per second
            };
            _positionTimer.Tick += PositionTimer_Tick;

            // Setup volume slider
            VolumeSlider.ValueChanged += VolumeSlider_ValueChanged;
            // Initialize volume percentage text
            VolumePercentText.Text = "100%";

            Loaded += WaveformEditor_Loaded;
            Closing += WaveformEditor_Closing;
        }

        private void WaveformEditor_Loaded(object sender, RoutedEventArgs e)
        {
            LoadWaveform();
            UpdateMarkers();
        }

        private void WaveformEditor_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            _positionTimer?.Stop();
            _audioPlayer.Dispose();
        }

        private void PositionTimer_Tick(object? sender, EventArgs e)
        {
            if (_currentPlaybackId == null)
            {
                PositionMarker.Visibility = Visibility.Collapsed;
                _positionTimer?.Stop();
                return;
            }

            var position = _audioPlayer.GetPlaybackPosition(_currentPlaybackId);
            if (position == null)
            {
                // Playback has stopped
                PositionMarker.Visibility = Visibility.Collapsed;
                _positionTimer?.Stop();
                _currentPlaybackId = null;
                return;
            }

            UpdatePositionMarker(position.Value);
        }

        private void UpdatePositionMarker(TimeSpan currentPosition)
        {
            double width = WaveformCanvas.ActualWidth;
            double height = WaveformCanvas.ActualHeight;

            if (width <= 0 || height <= 0)
                return;

            // Calculate position relative to total duration (currentPosition is absolute from file start)
            double positionX = (currentPosition.TotalSeconds / _sound.Duration.TotalSeconds) * width;

            PositionMarker.X1 = positionX;
            PositionMarker.X2 = positionX;
            PositionMarker.Y2 = height;
            PositionMarker.Visibility = Visibility.Visible;
        }

        private void LoadWaveform()
        {
            try
            {
                _waveformData = GenerateWaveformData(_sound.FilePath);
                DrawWaveform();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load waveform: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private float[] GenerateWaveformData(string filePath)
        {
            const int targetSamples = 2000; // Number of points to display

            using var reader = new AudioFileReader(filePath);
            var samplesPerPixel = (int)(reader.Length / reader.WaveFormat.BlockAlign / targetSamples);
            if (samplesPerPixel < 1) samplesPerPixel = 1;

            var samples = new float[targetSamples];
            var buffer = new float[samplesPerPixel * reader.WaveFormat.Channels];
            int sampleIndex = 0;

            while (reader.Position < reader.Length && sampleIndex < targetSamples)
            {
                int samplesRead = reader.Read(buffer, 0, buffer.Length);
                if (samplesRead == 0) break;

                // Calculate RMS (Root Mean Square) for this block
                float sum = 0;
                for (int i = 0; i < samplesRead; i++)
                {
                    sum += buffer[i] * buffer[i];
                }
                samples[sampleIndex] = (float)Math.Sqrt(sum / samplesRead);
                sampleIndex++;
            }

            // Normalize
            float max = 0;
            for (int i = 0; i < sampleIndex; i++)
            {
                if (samples[i] > max) max = samples[i];
            }
            if (max > 0)
            {
                for (int i = 0; i < sampleIndex; i++)
                {
                    samples[i] /= max;
                }
            }

            return samples;
        }

        private void DrawWaveform()
        {
            WaveformCanvas.Children.Clear();

            if (_waveformData.Length == 0)
                return;

            double width = WaveformCanvas.ActualWidth;
            double height = WaveformCanvas.ActualHeight;

            if (width <= 0 || height <= 0)
            {
                // Defer drawing until size is known
                WaveformCanvas.SizeChanged += (s, e) => DrawWaveform();
                return;
            }

            double centerY = height / 2;
            double pointWidth = width / _waveformData.Length;

            for (int i = 0; i < _waveformData.Length; i++)
            {
                double x = i * pointWidth;
                double barHeight = _waveformData[i] * (height / 2) * 0.9;

                var line = new Line
                {
                    X1 = x,
                    Y1 = centerY - barHeight,
                    X2 = x,
                    Y2 = centerY + barHeight,
                    Stroke = new SolidColorBrush(Color.FromRgb(100, 181, 246)),
                    StrokeThickness = Math.Max(1, pointWidth * 0.8)
                };

                WaveformCanvas.Children.Add(line);
            }

            UpdateMarkers();
        }

        private void UpdateMarkers()
        {
            double width = WaveformCanvas.ActualWidth;
            double height = WaveformCanvas.ActualHeight;

            if (width <= 0 || height <= 0)
                return;

            // Calculate positions
            double startX = (StartTime.TotalSeconds / _sound.Duration.TotalSeconds) * width;
            double endX = (EndTime.TotalSeconds / _sound.Duration.TotalSeconds) * width;

            // Update start marker
            StartMarker.X1 = startX;
            StartMarker.X2 = startX;
            StartMarker.Y2 = height;

            // Update end marker
            EndMarker.X1 = endX;
            EndMarker.X2 = endX;
            EndMarker.Y2 = height;

            // Update gray overlays
            LeftOverlay.Width = startX;
            LeftOverlay.Height = height;

            RightOverlay.Width = width - endX;
            RightOverlay.Height = height;
            Canvas.SetLeft(RightOverlay, endX);
        }

        private void WaveformCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var pos = e.GetPosition(WaveformCanvas);
            double startX = (StartTime.TotalSeconds / _sound.Duration.TotalSeconds) * WaveformCanvas.ActualWidth;
            double endX = (EndTime.TotalSeconds / _sound.Duration.TotalSeconds) * WaveformCanvas.ActualWidth;

            // Check if clicking near start or end marker (within 10 pixels)
            if (Math.Abs(pos.X - startX) < 10)
            {
                _isDraggingStart = true;
                WaveformCanvas.CaptureMouse();
            }
            else if (Math.Abs(pos.X - endX) < 10)
            {
                _isDraggingEnd = true;
                WaveformCanvas.CaptureMouse();
            }
            else
            {
                // Set start marker to click position
                SetStartTimeFromPosition(pos.X);
            }
        }

        private void WaveformCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDraggingStart || _isDraggingEnd)
            {
                var pos = e.GetPosition(WaveformCanvas);

                if (_isDraggingStart)
                {
                    SetStartTimeFromPosition(pos.X);
                }
                else if (_isDraggingEnd)
                {
                    SetEndTimeFromPosition(pos.X);
                }
            }
        }

        private void WaveformCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDraggingStart = false;
            _isDraggingEnd = false;
            WaveformCanvas.ReleaseMouseCapture();
        }

        private void SetStartTimeFromPosition(double x)
        {
            double width = WaveformCanvas.ActualWidth;
            if (width <= 0) return;

            double ratio = Math.Clamp(x / width, 0, 1);
            StartTime = TimeSpan.FromSeconds(_sound.Duration.TotalSeconds * ratio);

            // Ensure start is before end
            if (StartTime >= EndTime)
            {
                StartTime = EndTime - TimeSpan.FromMilliseconds(100);
            }

            StartTimeTextBox.Text = FormatTimeSpan(StartTime);
            UpdateMarkers();
        }

        private void SetEndTimeFromPosition(double x)
        {
            double width = WaveformCanvas.ActualWidth;
            if (width <= 0) return;

            double ratio = Math.Clamp(x / width, 0, 1);
            EndTime = TimeSpan.FromSeconds(_sound.Duration.TotalSeconds * ratio);

            // Ensure end is after start
            if (EndTime <= StartTime)
            {
                EndTime = StartTime + TimeSpan.FromMilliseconds(100);
            }

            EndTimeTextBox.Text = FormatTimeSpan(EndTime);
            UpdateMarkers();
        }

        private void StartTimeTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (TryParseTimeSpan(StartTimeTextBox.Text, out var time))
            {
                StartTime = time;
                if (StartTime < TimeSpan.Zero) StartTime = TimeSpan.Zero;
                if (StartTime >= EndTime) StartTime = EndTime - TimeSpan.FromMilliseconds(100);
                UpdateMarkers();
            }
        }

        private void EndTimeTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (TryParseTimeSpan(EndTimeTextBox.Text, out var time))
            {
                EndTime = time;
                if (EndTime > _sound.Duration) EndTime = _sound.Duration;
                if (EndTime <= StartTime) EndTime = StartTime + TimeSpan.FromMilliseconds(100);
                UpdateMarkers();
            }
        }

        private void ResetStart_Click(object sender, RoutedEventArgs e)
        {
            StartTime = TimeSpan.Zero;
            StartTimeTextBox.Text = FormatTimeSpan(StartTime);
            UpdateMarkers();
        }

        private void ResetEnd_Click(object sender, RoutedEventArgs e)
        {
            EndTime = _sound.Duration;
            EndTimeTextBox.Text = FormatTimeSpan(EndTime);
            UpdateMarkers();
        }

        private void PlayStop_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPlaybackId != null)
            {
                // Stop playback
                _audioPlayer.StopAll();
                _positionTimer?.Stop();
                _currentPlaybackId = null;
                PositionMarker.Visibility = Visibility.Collapsed;
                PlayStopButton.Content = "▶ Play Preview";
                PlayStopButton.Background = new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Green
            }
            else
            {
                // Start playback
                float previewVolume = (float)VolumeSlider.Value;
                _currentPlaybackId = _audioPlayer.PlaySegment(_sound.FilePath, previewVolume, StartTime, EndTime, OnPreviewStopped);

                if (!string.IsNullOrEmpty(_currentPlaybackId))
                {
                    _positionTimer?.Start();
                    PlayStopButton.Content = "■ Stop Preview";
                    PlayStopButton.Background = new SolidColorBrush(Color.FromRgb(244, 67, 54)); // Red
                }
            }
        }

        private void OnPreviewStopped()
        {
            Dispatcher.Invoke(() =>
            {
                _positionTimer?.Stop();
                _currentPlaybackId = null;
                PositionMarker.Visibility = Visibility.Collapsed;
                PlayStopButton.Content = "▶ Play Preview";
                PlayStopButton.Background = new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Green
            });
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (VolumePercentText != null) // Null check for initialization
            {
                VolumePercentText.Text = $"{(int)(e.NewValue * 100)}%";
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Update the sound's duration to reflect the edited selection
            _sound.Duration = EndTime - StartTime;

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            // Restore original values
            StartTime = _originalStartTime;
            EndTime = _originalEndTime;
            DialogResult = false;
            Close();
        }

        private string FormatTimeSpan(TimeSpan time)
        {
            return time.ToString(@"mm\:ss\.fff");
        }

        private bool TryParseTimeSpan(string input, out TimeSpan result)
        {
            // Try to parse formats like "01:23.456" or "1:23.456"
            if (TimeSpan.TryParseExact(input, @"mm\:ss\.fff", null, out result))
                return true;
            if (TimeSpan.TryParseExact(input, @"m\:ss\.fff", null, out result))
                return true;
            if (TimeSpan.TryParseExact(input, @"mm\:ss", null, out result))
                return true;
            if (TimeSpan.TryParseExact(input, @"m\:ss", null, out result))
                return true;

            result = TimeSpan.Zero;
            return false;
        }
    }
}
