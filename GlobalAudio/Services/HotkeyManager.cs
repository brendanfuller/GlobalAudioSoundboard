using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace GlobalAudio.Services
{
    public class HotkeyManager : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int WM_HOTKEY = 0x0312;

        private IntPtr _windowHandle;
        private HwndSource? _source;
        private Dictionary<int, Action> _hotkeyActions = new Dictionary<int, Action>();
        private int _currentId = 1;

        public void Initialize(IntPtr windowHandle)
        {
            _windowHandle = windowHandle;
            _source = HwndSource.FromHwnd(_windowHandle);
            _source?.AddHook(WndProc);
        }

        public int RegisterHotkey(int modifiers, int virtualKey, Action action)
        {
            int id = _currentId++;

            if (RegisterHotKey(_windowHandle, id, (uint)modifiers, (uint)virtualKey))
            {
                _hotkeyActions[id] = action;
                return id;
            }

            return -1;
        }

        public void UnregisterHotkey(int id)
        {
            if (_hotkeyActions.ContainsKey(id))
            {
                UnregisterHotKey(_windowHandle, id);
                _hotkeyActions.Remove(id);
            }
        }

        public void UnregisterAll()
        {
            foreach (var id in _hotkeyActions.Keys)
            {
                UnregisterHotKey(_windowHandle, id);
            }
            _hotkeyActions.Clear();
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY)
            {
                int id = wParam.ToInt32();
                if (_hotkeyActions.TryGetValue(id, out var action))
                {
                    action?.Invoke();
                    handled = true;
                }
            }

            return IntPtr.Zero;
        }

        public void Dispose()
        {
            UnregisterAll();
            _source?.RemoveHook(WndProc);
        }
    }

    // Modifier keys for hotkeys
    public static class HotkeyModifiers
    {
        public const int MOD_ALT = 0x0001;
        public const int MOD_CONTROL = 0x0002;
        public const int MOD_SHIFT = 0x0004;
        public const int MOD_WIN = 0x0008;
        public const int MOD_NOREPEAT = 0x4000;
    }
}
