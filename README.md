# Global Audio Soundboard

A Windows desktop application for playing audio files with global keyboard shortcuts.

## Features

- **Add Audio Files**: Import MP3, WAV, and other audio formats
- **Volume Control**: Individual volume slider for each sound
- **Global Hotkeys**: Assign keyboard shortcuts to play sounds from anywhere
- **Sound Management**: Play and delete sounds with confirmation
- **Local Copy**: Sounds are copied to a local folder for persistence
- **Configuration**: All settings saved in a config file next to the executable
- **Theme**: Respects browser theme choice for light and dark themes

## Requirements

- Windows 10 or later 

## Releases

Check the releas tab to download the latest version. 

## Build Process

### First-Time Setup

1. Run `setup.bat` - This will:
   - Check if .NET 8 SDK is installed
   - Download and install .NET 8 SDK if needed
   - Restore NuGet packages

### Building the Application

2. Run `build.bat` to compile the application
   - The executable will be in: `GlobalAudioSoundboard\bin\Release\net8.0-windows\GlobalAudioSoundboard.exe`

### Running the Application

3. Run `run.bat` or execute the compiled .exe file directly

## Usage

1. **Add a Sound**: Click the "+ Add Sound" button and select an audio file
2. **Set Volume**: Use the slider for each sound to adjust its volume (0-100%)
3. **Assign Hotkey**:
   - Click on the "Global Keybind" field for a sound
   - Press your desired key combination (must include Ctrl, Alt, Shift, or Win)
   - Press ESC to confirm the hotkey
4. **Play Sound**: Click the play button (▶) or use the assigned global hotkey
5. **Delete Sound**: Click the delete button (🗑) and confirm

## Configuration

- **Sounds Folder**: `Data/Sounds/` (next to the .exe)
- **Config File**: `Data/config.json` (next to the .exe)
- All imported audio files are stored in the Sounds folder
- Configuration is automatically saved when changes are made