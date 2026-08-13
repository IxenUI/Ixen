using Ixen.Core.Input;

namespace Ixen.Platform.Windows.NativeApi
{
    internal enum NativeKeyKind
    {
        Down = 0,
        Up = 1,
        Char = 2
    }

    internal static class NativeKeys
    {
        private const int MOD_SHIFT = 1;
        private const int MOD_CONTROL = 2;
        private const int MOD_ALT = 4;

        internal static KeyModifiers ToModifiers(int modifiers)
        {
            KeyModifiers result = KeyModifiers.None;

            if ((modifiers & MOD_SHIFT) != 0)
            {
                result |= KeyModifiers.Shift;
            }

            if ((modifiers & MOD_CONTROL) != 0)
            {
                result |= KeyModifiers.Control;
            }

            if ((modifiers & MOD_ALT) != 0)
            {
                result |= KeyModifiers.Alt;
            }

            return result;
        }

        internal static Key ToKey(int virtualKey)
        {
            if (virtualKey >= 0x41 && virtualKey <= 0x5A)
            {
                return Key.A + (virtualKey - 0x41);
            }

            if (virtualKey >= 0x30 && virtualKey <= 0x39)
            {
                return Key.Digit0 + (virtualKey - 0x30);
            }

            if (virtualKey >= 0x70 && virtualKey <= 0x7B)
            {
                return Key.F1 + (virtualKey - 0x70);
            }

            switch (virtualKey)
            {
                case 0x1B: return Key.Escape;
                case 0x09: return Key.Tab;
                case 0x0D: return Key.Enter;
                case 0x20: return Key.Space;
                case 0x08: return Key.Backspace;
                case 0x2E: return Key.Delete;
                case 0x2D: return Key.Insert;

                case 0x25: return Key.Left;
                case 0x27: return Key.Right;
                case 0x26: return Key.Up;
                case 0x28: return Key.Down;
                case 0x24: return Key.Home;
                case 0x23: return Key.End;
                case 0x21: return Key.PageUp;
                case 0x22: return Key.PageDown;

                case 0x10: return Key.Shift;
                case 0x11: return Key.Control;
                case 0x12: return Key.Alt;

                default: return Key.None;
            }
        }
    }
}
