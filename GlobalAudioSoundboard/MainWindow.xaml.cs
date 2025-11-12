using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using GlobalAudioSoundboard.Models;
using GlobalAudioSoundboard.Services;
using Microsoft.Win32;

namespace GlobalAudioSoundboard
{
    public partial class MainWindow : Window
    {
        private ObservableCollection<SoundItem> _sounds = new ObservableCollection<SoundItem>();
        private ObservableCollection<ActiveDeviceDisplay> _activeDevices = new ObservableCollection<ActiveDeviceDisplay>();
        private ConfigManager _configManager = new ConfigManager();
        private AudioPlayer _audioPlayer = new AudioPlayer();
        private HotkeyManager _hotkeyManager = new HotkeyManager();
        private Dictionary<string, int> _hotkeyIds = new Dictionary<string, int>();

        private SoundItem? _editingHotkeyFor = null;
        private bool _isCapturingHotkey = false;

        public MainWindow()
        {
            InitializeComponent();

            // Apply system theme
            ThemeManager.ApplyTheme(this);

            SoundsListView.ItemsSource = _sounds;
            ActiveDevicesList.ItemsSource = _activeDevices;

            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialize hotkey manager
            var helper = new WindowInteropHelper(this);
            _hotkeyManager.Initialize(helper.Handle);

            // Load audio devices
            LoadAudioDevices();

            // Load sounds from config
            LoadSounds();
        }

        private void LoadAudioDevices()
        {
            // Load saved settings
            var settings = _configManager.LoadSettings();

            // Set the output devices with their volumes
            _audioPlayer.SetOutputDevices(settings.OutputDeviceNumbers, settings.DeviceVolumes);

            // Update button text to show count
            UpdateDeviceButtonText(settings.OutputDeviceNumbers.Count);

            // Update the active devices display
            UpdateActiveDevicesDisplay();
        }

        private void UpdateDeviceButtonText(int count)
        {
            AudioDeviceButton.Content = count == 1 ? "1 Device Selected" : $"{count} Devices Selected";
        }

        private void UpdateActiveDevicesDisplay()
        {
            _activeDevices.Clear();

            var deviceNumbers = _audioPlayer.GetOutputDevices();
            var deviceVolumes = _audioPlayer.GetDeviceVolumes();

            foreach (var deviceNum in deviceNumbers)
            {
                string deviceName;
                if (deviceNum == -1)
                {
                    deviceName = "Default Device";
                }
                else
                {
                    try
                    {
                        deviceName = NAudio.Wave.WaveOut.GetCapabilities(deviceNum).ProductName;
                    }
                    catch
                    {
                        deviceName = $"Device {deviceNum}";
                    }
                }

                float volume = deviceVolumes.ContainsKey(deviceNum) ? deviceVolumes[deviceNum] : 1.0f;

                _activeDevices.Add(new ActiveDeviceDisplay
                {
                    Name = deviceName,
                    Volume = volume
                });
            }
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveSounds();
            _hotkeyManager.Dispose();
            _audioPlayer.Dispose();
        }

        private void LoadSounds()
        {
            var sounds = _configManager.LoadSounds();
            _sounds.Clear();

            var failedHotkeys = new List<string>();

            foreach (var sound in sounds)
            {
                _sounds.Add(sound);

                // Try to register hotkey (only need to check if virtual key is set)
                if (sound.HotkeyVirtualKey != 0)
                {
                    var oldHotkey = sound.Hotkey;
                    RegisterHotkeyForSound(sound);

                    // Check if it failed
                    if (sound.Hotkey.Contains("CONFLICT"))
                    {
                        failedHotkeys.Add($"• {sound.Name}: {oldHotkey}");
                    }
                }
            }

            // Show a summary if any hotkeys failed
            if (failedHotkeys.Count > 0)
            {
                MessageBox.Show(
                    $"Warning: {failedHotkeys.Count} hotkey(s) could not be registered:\n\n" +
                    string.Join("\n", failedHotkeys) + "\n\n" +
                    "These hotkeys are in use by other applications.\n" +
                    "Please reassign them to different keys.",
                    "Hotkey Conflicts Detected",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void SaveSounds()
        {
            _configManager.SaveSounds(_sounds.ToList());
        }

        private void AddSound_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Audio Files|*.mp3;*.wav;*.m4a;*.wma;*.aac;*.flac|All Files|*.*",
                Title = "Select an Audio File"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                ImportSoundFile(openFileDialog.FileName);
            }
        }

        private void ImportSoundFile(string filePath)
        {
            if (!File.Exists(filePath))
                return;

            // Check if it's an audio file
            var extension = Path.GetExtension(filePath).ToLower();
            var validExtensions = new[] { ".mp3", ".wav", ".m4a", ".wma", ".aac", ".flac", ".ogg" };

            if (!validExtensions.Contains(extension))
            {
                MessageBox.Show($"Unsupported file type: {extension}\n\nSupported formats: MP3, WAV, M4A, WMA, AAC, FLAC, OGG",
                    "Invalid File Type", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Import the file
                var importedPath = _configManager.ImportSound(filePath);

                // Get duration
                var duration = _audioPlayer.GetDuration(importedPath);

                // Create sound item
                var sound = new SoundItem
                {
                    Name = Path.GetFileNameWithoutExtension(filePath),
                    FilePath = importedPath,
                    Duration = duration,
                    Volume = 1.0f,
                    Hotkey = "Not Set"
                };

                _sounds.Add(sound);
                SaveSounds();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to add sound: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is SoundItem sound)
            {
                if (sound.IsPlaying && !string.IsNullOrEmpty(sound.PlaybackId))
                {
                    // Stop this specific sound
                    _audioPlayer.Stop(sound.PlaybackId);
                    sound.IsPlaying = false;
                    sound.PlaybackId = null;
                }
                else
                {
                    // Play the sound (allow multiple simultaneous sounds)
                    sound.IsPlaying = true;

                    // Use custom start/end times if set, otherwise play full audio
                    TimeSpan startTime = sound.StartTime;
                    TimeSpan endTime = sound.EndTime > TimeSpan.Zero ? sound.EndTime : sound.Duration;

                    var playbackId = _audioPlayer.PlaySegment(sound.FilePath, sound.Volume, startTime, endTime, () =>
                    {
                        // Called when playback stops naturally
                        Dispatcher.Invoke(() =>
                        {
                            sound.IsPlaying = false;
                            sound.PlaybackId = null;
                        });
                    });
                    sound.PlaybackId = playbackId;
                }
            }
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is SoundItem sound)
            {
                var editor = new WaveformEditor(sound) { Owner = this };
                if (editor.ShowDialog() == true)
                {
                    // Update the sound with the new start and end times
                    sound.StartTime = editor.StartTime;
                    sound.EndTime = editor.EndTime;
                    SaveSounds();
                }
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is SoundItem sound)
            {
                var result = MessageBox.Show(
                    $"Are you sure you want to delete '{sound.Name}'?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    // Unregister hotkey
                    UnregisterHotkeyForSound(sound);

                    // Delete file
                    _configManager.DeleteSound(sound.FilePath);

                    // Remove from list
                    _sounds.Remove(sound);
                    SaveSounds();
                }
            }
        }

        private void Hotkey_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is SoundItem sound)
            {
                // Unregister current hotkey while editing
                UnregisterHotkeyForSound(sound);

                var dialog = new HotkeyDialog { Owner = this };
                if (dialog.ShowDialog() == true)
                {
                    if (dialog.WasCleared)
                    {
                        // Clear the hotkey
                        sound.Hotkey = "Not Set";
                        sound.HotkeyModifiers = 0;
                        sound.HotkeyVirtualKey = 0;
                    }
                    else if (dialog.HotkeyString != null)
                    {
                        // Set the new hotkey
                        sound.Hotkey = dialog.HotkeyString;
                        sound.HotkeyModifiers = dialog.HotkeyModifiers;
                        sound.HotkeyVirtualKey = dialog.HotkeyVirtualKey;

                        // Register the new hotkey
                        RegisterHotkeyForSound(sound);
                    }

                    SaveSounds();
                }
                else
                {
                    // Cancelled - restore the old hotkey
                    if (sound.HotkeyVirtualKey != 0)
                    {
                        RegisterHotkeyForSound(sound);
                    }
                }
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            // Old inline capture code removed - now using dialog
            if (false && _isCapturingHotkey && _editingHotkeyFor != null)
            {
                // ESC confirms the hotkey
                if (e.Key == Key.Escape)
                {
                    // If nothing was set, clear it
                    if (_editingHotkeyFor.Hotkey == "Press keys... (ESC to confirm)")
                    {
                        _editingHotkeyFor.Hotkey = "Not Set";
                        _editingHotkeyFor.HotkeyModifiers = 0;
                        _editingHotkeyFor.HotkeyVirtualKey = 0;
                    }

                    _isCapturingHotkey = false;
                    _editingHotkeyFor = null;
                    SaveSounds();
                    return;
                }

                // Don't allow ESC as a hotkey
                if (e.Key == Key.Escape)
                    return;

                // Build the hotkey
                int modifiers = 0;
                var key = e.Key == Key.System ? e.SystemKey : e.Key;

                // Ignore if the key itself is a modifier key
                if (key == Key.LeftShift || key == Key.RightShift ||
                    key == Key.LeftCtrl || key == Key.RightCtrl ||
                    key == Key.LeftAlt || key == Key.RightAlt ||
                    key == Key.LWin || key == Key.RWin)
                {
                    // Just show that we're waiting for a key
                    _editingHotkeyFor.Hotkey = "Press a key... (ESC to cancel)";
                    return;
                }

                // Handle modifiers
                if ((Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) != 0)
                    modifiers |= HotkeyModifiers.MOD_CONTROL;
                if ((Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Alt) != 0)
                    modifiers |= HotkeyModifiers.MOD_ALT;
                if ((Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Shift) != 0)
                    modifiers |= HotkeyModifiers.MOD_SHIFT;
                if ((Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Windows) != 0)
                    modifiers |= HotkeyModifiers.MOD_WIN;

                // Check if it's a safe key to use without modifiers (F1-F24, NumPad, etc.)
                bool isSafeKeyWithoutModifier =
                    (key >= Key.F1 && key <= Key.F24) ||
                    (key >= Key.NumPad0 && key <= Key.Divide) ||
                    key == Key.Insert || key == Key.Delete || key == Key.Home || key == Key.End ||
                    key == Key.PageUp || key == Key.PageDown || key == Key.Pause || key == Key.Print;

                // Require at least one modifier for regular keys (for safety, to avoid conflicts)
                if (modifiers == 0 && !isSafeKeyWithoutModifier)
                {
                    _editingHotkeyFor.Hotkey = "Need modifier key (Ctrl/Alt/Shift) or use F1-F12... ESC to cancel";
                    return;
                }

                // Get virtual key code
                int virtualKey = KeyInterop.VirtualKeyFromKey(key);

                // Build display string with cleaner key names
                var hotkeyString = "";
                if ((modifiers & HotkeyModifiers.MOD_CONTROL) != 0) hotkeyString += "Ctrl+";
                if ((modifiers & HotkeyModifiers.MOD_ALT) != 0) hotkeyString += "Alt+";
                if ((modifiers & HotkeyModifiers.MOD_SHIFT) != 0) hotkeyString += "Shift+";
                if ((modifiers & HotkeyModifiers.MOD_WIN) != 0) hotkeyString += "Win+";

                // Use cleaner names for common keys
                var keyName = key.ToString();
                if (keyName.StartsWith("D") && keyName.Length == 2 && char.IsDigit(keyName[1]))
                {
                    keyName = keyName.Substring(1); // D0 -> 0, D1 -> 1, etc.
                }
                hotkeyString += keyName;

                _editingHotkeyFor.Hotkey = hotkeyString;
                _editingHotkeyFor.HotkeyModifiers = modifiers;
                _editingHotkeyFor.HotkeyVirtualKey = virtualKey;

                // Register the new hotkey
                RegisterHotkeyForSound(_editingHotkeyFor);

                e.Handled = true;
            }
        }

        private void RegisterHotkeyForSound(SoundItem sound)
        {
            // Only skip if virtual key is 0 (not set). Modifiers can be 0 for F1-F12, NumPad, etc.
            if (sound.HotkeyVirtualKey == 0)
                return;

            var id = _hotkeyManager.RegisterHotkey(
                sound.HotkeyModifiers,
                sound.HotkeyVirtualKey,
                () =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (sound.IsPlaying && !string.IsNullOrEmpty(sound.PlaybackId))
                        {
                            // Stop this specific sound
                            _audioPlayer.Stop(sound.PlaybackId);
                            sound.IsPlaying = false;
                            sound.PlaybackId = null;
                        }
                        else
                        {
                            // Play the sound (allow multiple simultaneous sounds)
                            sound.IsPlaying = true;

                            // Use custom start/end times if set, otherwise play full audio
                            TimeSpan startTime = sound.StartTime;
                            TimeSpan endTime = sound.EndTime > TimeSpan.Zero ? sound.EndTime : sound.Duration;

                            var playbackId = _audioPlayer.PlaySegment(sound.FilePath, sound.Volume, startTime, endTime, () =>
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    sound.IsPlaying = false;
                                    sound.PlaybackId = null;
                                });
                            });
                            sound.PlaybackId = playbackId;
                        }
                    });
                });

            if (id != -1)
            {
                _hotkeyIds[sound.Id] = id;
            }
            else
            {
                // Failed to register - hotkey is already in use
                sound.Hotkey = "CONFLICT - Not Set";

                MessageBox.Show(
                    $"Failed to register hotkey '{sound.Hotkey}' for '{sound.Name}'.\n\n" +
                    "This hotkey is already in use by another application or Windows feature.\n\n" +
                    "Common conflicts:\n" +
                    "• Graphics drivers (NVIDIA, AMD)\n" +
                    "• Gaming overlays (Discord, GeForce Experience, Steam)\n" +
                    "• Screen capture tools (OBS, ShareX)\n" +
                    "• Other soundboard/macro software\n\n" +
                    "Try a different key combination or close conflicting software.",
                    "Hotkey Conflict",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void UnregisterHotkeyForSound(SoundItem sound)
        {
            if (_hotkeyIds.TryGetValue(sound.Id, out var id))
            {
                _hotkeyManager.UnregisterHotkey(id);
                _hotkeyIds.Remove(sound.Id);
            }
        }

        private void AudioDevice_Click(object sender, RoutedEventArgs e)
        {
            var currentDevices = _audioPlayer.GetOutputDevices();
            var currentVolumes = _audioPlayer.GetDeviceVolumes();

            var deviceWindow = new AudioDeviceWindow(currentDevices, currentVolumes)
            {
                Owner = this
            };

            if (deviceWindow.ShowDialog() == true)
            {
                // Update the audio player
                _audioPlayer.SetOutputDevices(deviceWindow.SelectedDeviceNumbers, deviceWindow.DeviceVolumes);

                // Save the settings
                var settings = new AppSettings
                {
                    OutputDeviceNumbers = deviceWindow.SelectedDeviceNumbers,
                    DeviceVolumes = deviceWindow.DeviceVolumes
                };
                _configManager.SaveSettings(settings);

                // Update button text
                UpdateDeviceButtonText(deviceWindow.SelectedDeviceNumbers.Count);

                // Update the active devices display
                UpdateActiveDevicesDisplay();
            }
        }

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);

                if (files != null && files.Length > 0)
                {
                    // Import all dropped files
                    foreach (var file in files)
                    {
                        ImportSoundFile(file);
                    }
                }
            }
        }
    }
}
