using System;
using System.Windows;
using System.Windows.Input;
using GlobalAudio.Services;

namespace GlobalAudio
{
    public partial class HotkeyDialog : Window
    {
        public string? HotkeyString { get; private set; }
        public int HotkeyModifiers { get; private set; }
        public int HotkeyVirtualKey { get; private set; }
        public bool WasCleared { get; private set; }

        private bool _hasValidKey = false;

        public HotkeyDialog()
        {
            InitializeComponent();
            ThemeManager.ApplyTheme(this);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Focus the window to capture keys
            this.Focus();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            var key = e.Key == Key.System ? e.SystemKey : e.Key;

            // Ignore modifier keys alone
            if (key == Key.LeftShift || key == Key.RightShift ||
                key == Key.LeftCtrl || key == Key.RightCtrl ||
                key == Key.LeftAlt || key == Key.RightAlt ||
                key == Key.LWin || key == Key.RWin)
            {
                KeyDisplayText.Text = "Press a key with modifiers...";
                return;
            }

            // Build the hotkey
            int modifiers = 0;

            if ((Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) != 0)
                modifiers |= Services.HotkeyModifiers.MOD_CONTROL;
            if ((Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Alt) != 0)
                modifiers |= Services.HotkeyModifiers.MOD_ALT;
            if ((Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Shift) != 0)
                modifiers |= Services.HotkeyModifiers.MOD_SHIFT;
            if ((Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Windows) != 0)
                modifiers |= Services.HotkeyModifiers.MOD_WIN;

            // Check if it's a safe key to use without modifiers
            bool isSafeKeyWithoutModifier =
                (key >= Key.F1 && key <= Key.F24) ||
                (key >= Key.NumPad0 && key <= Key.Divide) ||
                key == Key.Insert || key == Key.Delete || key == Key.Home || key == Key.End ||
                key == Key.PageUp || key == Key.PageDown || key == Key.Pause || key == Key.Print;

            // Require at least one modifier for regular keys
            if (modifiers == 0 && !isSafeKeyWithoutModifier)
            {
                KeyDisplayText.Text = "Requires modifier key (Ctrl/Alt/Shift) or use F1-F12";
                _hasValidKey = false;
                return;
            }

            // Get virtual key code
            int virtualKey = KeyInterop.VirtualKeyFromKey(key);

            // Build display string
            var hotkeyString = "";
            if ((modifiers & Services.HotkeyModifiers.MOD_CONTROL) != 0) hotkeyString += "Ctrl+";
            if ((modifiers & Services.HotkeyModifiers.MOD_ALT) != 0) hotkeyString += "Alt+";
            if ((modifiers & Services.HotkeyModifiers.MOD_SHIFT) != 0) hotkeyString += "Shift+";
            if ((modifiers & Services.HotkeyModifiers.MOD_WIN) != 0) hotkeyString += "Win+";

            // Clean up key names
            var keyName = key.ToString();
            if (keyName.StartsWith("D") && keyName.Length == 2 && char.IsDigit(keyName[1]))
            {
                keyName = keyName.Substring(1); // D0 -> 0, D1 -> 1, etc.
            }
            hotkeyString += keyName;

            KeyDisplayText.Text = hotkeyString;
            HotkeyString = hotkeyString;
            HotkeyModifiers = modifiers;
            HotkeyVirtualKey = virtualKey;
            _hasValidKey = true;

            e.Handled = true;
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            if (_hasValidKey)
            {
                DialogResult = true;
                Close();
            }
        }

        private void Clear_Click(object? sender, RoutedEventArgs? e)
        {
            WasCleared = true;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
