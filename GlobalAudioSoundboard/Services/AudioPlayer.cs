using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NAudio.Wave;

namespace GlobalAudioSoundboard.Services
{
    public class AudioPlayer : IDisposable
    {
        private List<int> _deviceNumbers = new List<int> { -1 }; // -1 = default device
        private Dictionary<int, float> _deviceVolumes = new Dictionary<int, float>(); // Per-device volume multipliers
        private List<PlaybackInstance> _activePlaybacks = new List<PlaybackInstance>();
        private object _playbackLock = new object();

        private class PlaybackInstance
        {
            public string Id { get; set; } = Guid.NewGuid().ToString();
            public List<AudioFileReader> Readers { get; set; } = new List<AudioFileReader>();
            public List<WaveOutEvent> OutputDevices { get; set; } = new List<WaveOutEvent>();
            public Action? OnStopped { get; set; }
            public int StoppedCount { get; set; }
            public int ExpectedStopCount { get; set; }
        }

        public static List<AudioDevice> GetAudioDevices()
        {
            var devices = new List<AudioDevice>();

            // Add default device
            devices.Add(new AudioDevice { DeviceNumber = -1, Name = "Default Device" });

            // Add all available output devices
            for (int i = 0; i < WaveOut.DeviceCount; i++)
            {
                var caps = WaveOut.GetCapabilities(i);
                devices.Add(new AudioDevice { DeviceNumber = i, Name = caps.ProductName });
            }

            return devices;
        }

        public void SetOutputDevices(List<int> deviceNumbers, Dictionary<int, float>? deviceVolumes = null)
        {
            // Make a copy of the list to avoid external modifications
            _deviceNumbers = new List<int>(deviceNumbers ?? new List<int> { -1 });
            if (_deviceNumbers.Count == 0)
                _deviceNumbers.Add(-1);

            if (deviceVolumes != null)
            {
                _deviceVolumes = new Dictionary<int, float>(deviceVolumes);
            }

            // Ensure all devices have a volume setting (default to 1.0)
            foreach (var deviceNum in _deviceNumbers)
            {
                if (!_deviceVolumes.ContainsKey(deviceNum))
                {
                    _deviceVolumes[deviceNum] = 1.0f;
                }
            }
        }

        public List<int> GetOutputDevices()
        {
            return new List<int>(_deviceNumbers);
        }

        public Dictionary<int, float> GetDeviceVolumes()
        {
            return new Dictionary<int, float>(_deviceVolumes);
        }

        public void SetDeviceVolume(int deviceNumber, float volume)
        {
            _deviceVolumes[deviceNumber] = Math.Clamp(volume, 0f, 1f);
        }

        public TimeSpan GetDuration(string filePath)
        {
            try
            {
                using var reader = new AudioFileReader(filePath);
                return reader.TotalTime;
            }
            catch
            {
                return TimeSpan.Zero;
            }
        }

        public string Play(string filePath, float volume, Action? onPlaybackStopped = null)
        {
            return PlaySegment(filePath, volume, TimeSpan.Zero, TimeSpan.Zero, onPlaybackStopped);
        }

        public string PlaySegment(string filePath, float volume, TimeSpan startTime, TimeSpan endTime, Action? onPlaybackStopped = null)
        {
            if (!File.Exists(filePath))
                return string.Empty;

            var playback = new PlaybackInstance
            {
                OnStopped = onPlaybackStopped,
                ExpectedStopCount = _deviceNumbers.Count
            };

            try
            {
                // Play to each selected output device
                foreach (var deviceNumber in _deviceNumbers)
                {
                    var audioFileReader = new AudioFileReader(filePath);

                    // Set position to start time if specified
                    if (startTime > TimeSpan.Zero)
                    {
                        audioFileReader.CurrentTime = startTime;
                    }

                    // Get the device-specific volume multiplier
                    float deviceVolume = _deviceVolumes.ContainsKey(deviceNumber) ? _deviceVolumes[deviceNumber] : 1.0f;

                    // Apply both the sound volume and device volume
                    audioFileReader.Volume = Math.Clamp(volume * deviceVolume, 0f, 1f);

                    // If end time is specified, create a trimmed provider
                    NAudio.Wave.ISampleProvider provider = audioFileReader;
                    if (endTime > TimeSpan.Zero && endTime < audioFileReader.TotalTime)
                    {
                        provider = new TrimmedAudioProvider(audioFileReader, startTime, endTime);
                    }

                    var outputDevice = new WaveOutEvent();

                    // Set the output device if specified
                    if (deviceNumber >= 0)
                    {
                        outputDevice.DeviceNumber = deviceNumber;
                    }

                    outputDevice.Init(provider);
                    outputDevice.PlaybackStopped += (sender, e) => OnPlaybackStopped(playback.Id, sender, e);

                    playback.Readers.Add(audioFileReader);
                    playback.OutputDevices.Add(outputDevice);

                    outputDevice.Play();
                }

                lock (_playbackLock)
                {
                    _activePlaybacks.Add(playback);
                }

                return playback.Id;
            }
            catch (Exception ex)
            {
                // Clean up on error
                foreach (var device in playback.OutputDevices)
                {
                    device?.Stop();
                    device?.Dispose();
                }
                foreach (var reader in playback.Readers)
                {
                    reader?.Dispose();
                }
                return string.Empty;
            }
        }

        public void Stop(string playbackId)
        {
            lock (_playbackLock)
            {
                var playback = _activePlaybacks.FirstOrDefault(p => p.Id == playbackId);
                if (playback == null)
                    return;

                foreach (var device in playback.OutputDevices)
                {
                    device?.Stop();
                    device?.Dispose();
                }
                playback.OutputDevices.Clear();

                foreach (var reader in playback.Readers)
                {
                    reader?.Dispose();
                }
                playback.Readers.Clear();

                _activePlaybacks.Remove(playback);
            }
        }

        public void StopAll()
        {
            lock (_playbackLock)
            {
                foreach (var playback in _activePlaybacks.ToList())
                {
                    foreach (var device in playback.OutputDevices)
                    {
                        device?.Stop();
                        device?.Dispose();
                    }
                    foreach (var reader in playback.Readers)
                    {
                        reader?.Dispose();
                    }
                }
                _activePlaybacks.Clear();
            }
        }

        private void OnPlaybackStopped(string playbackId, object? sender, StoppedEventArgs e)
        {
            PlaybackInstance? playback = null;
            Action? callback = null;

            lock (_playbackLock)
            {
                playback = _activePlaybacks.FirstOrDefault(p => p.Id == playbackId);
                if (playback == null)
                    return;

                playback.StoppedCount++;

                // Only call the callback when ALL devices have stopped for this playback
                if (playback.StoppedCount >= playback.ExpectedStopCount)
                {
                    callback = playback.OnStopped;

                    // Clean up
                    foreach (var device in playback.OutputDevices)
                    {
                        device?.Dispose();
                    }
                    foreach (var reader in playback.Readers)
                    {
                        reader?.Dispose();
                    }

                    _activePlaybacks.Remove(playback);
                }
            }

            // Call callback outside of lock to avoid deadlocks
            callback?.Invoke();
        }

        public void Dispose()
        {
            StopAll();
        }
    }

    public class AudioDevice
    {
        public int DeviceNumber { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    // Helper class to trim audio to a specific time range
    internal class TrimmedAudioProvider : NAudio.Wave.ISampleProvider
    {
        private readonly NAudio.Wave.ISampleProvider _source;
        private readonly long _startSample;
        private readonly long _endSample;
        private long _currentSample;

        public TrimmedAudioProvider(NAudio.Wave.ISampleProvider source, TimeSpan startTime, TimeSpan endTime)
        {
            _source = source;

            // Calculate sample positions
            _startSample = (long)(startTime.TotalSeconds * source.WaveFormat.SampleRate) * source.WaveFormat.Channels;
            _endSample = (long)(endTime.TotalSeconds * source.WaveFormat.SampleRate) * source.WaveFormat.Channels;
            _currentSample = _startSample;
        }

        public NAudio.Wave.WaveFormat WaveFormat => _source.WaveFormat;

        public int Read(float[] buffer, int offset, int count)
        {
            // Check if we've reached the end
            if (_currentSample >= _endSample)
                return 0;

            // Limit read to not exceed end time
            long samplesToRead = Math.Min(count, _endSample - _currentSample);
            int samplesRead = _source.Read(buffer, offset, (int)samplesToRead);

            _currentSample += samplesRead;
            return samplesRead;
        }
    }
}
