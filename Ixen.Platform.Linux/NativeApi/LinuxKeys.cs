using Ixen.Core.Input;

namespace Ixen.Platform.Linux.NativeApi
{
    internal static class LinuxKeys
    {
        private const int MOD_SHIFT = 1;
        private const int MOD_CONTROL = 2;
        private const int MOD_ALT = 4;
        private const int MOD_META = 8;

        internal const int XK_SPACE = 0x0020;
        internal const int XK_0 = 0x0030;
        internal const int XK_9 = 0x0039;
        internal const int XK_A = 0x0041;
        internal const int XK_Z = 0x005A;
        internal const int XK_LOWER_A = 0x0061;
        internal const int XK_LOWER_Z = 0x007A;

        internal const int XK_BACKSPACE = 0xFF08;
        internal const int XK_TAB = 0xFF09;
        internal const int XK_RETURN = 0xFF0D;
        internal const int XK_ESCAPE = 0xFF1B;
        internal const int XK_HOME = 0xFF50;
        internal const int XK_LEFT = 0xFF51;
        internal const int XK_UP = 0xFF52;
        internal const int XK_RIGHT = 0xFF53;
        internal const int XK_DOWN = 0xFF54;
        internal const int XK_PAGE_UP = 0xFF55;
        internal const int XK_PAGE_DOWN = 0xFF56;
        internal const int XK_END = 0xFF57;
        internal const int XK_INSERT = 0xFF63;
        internal const int XK_KP_ENTER = 0xFF8D;
        internal const int XK_KP_HOME = 0xFF95;
        internal const int XK_KP_LEFT = 0xFF96;
        internal const int XK_KP_UP = 0xFF97;
        internal const int XK_KP_RIGHT = 0xFF98;
        internal const int XK_KP_DOWN = 0xFF99;
        internal const int XK_KP_PAGE_UP = 0xFF9A;
        internal const int XK_KP_PAGE_DOWN = 0xFF9B;
        internal const int XK_KP_END = 0xFF9C;
        internal const int XK_KP_INSERT = 0xFF9E;
        internal const int XK_KP_DELETE = 0xFF9F;
        internal const int XK_KP_0 = 0xFFB0;
        internal const int XK_KP_9 = 0xFFB9;
        internal const int XK_F1 = 0xFFBE;
        internal const int XK_F12 = 0xFFC9;
        internal const int XK_SHIFT_L = 0xFFE1;
        internal const int XK_SHIFT_R = 0xFFE2;
        internal const int XK_CONTROL_L = 0xFFE3;
        internal const int XK_CONTROL_R = 0xFFE4;
        internal const int XK_META_L = 0xFFE7;
        internal const int XK_META_R = 0xFFE8;
        internal const int XK_ALT_L = 0xFFE9;
        internal const int XK_ALT_R = 0xFFEA;
        internal const int XK_SUPER_L = 0xFFEB;
        internal const int XK_SUPER_R = 0xFFEC;
        internal const int XK_ISO_LEFT_TAB = 0xFE20;
        internal const int XK_DELETE = 0xFFFF;

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

            if ((modifiers & MOD_META) != 0)
            {
                result |= KeyModifiers.Meta;
            }

            return result;
        }

        internal static Key ToKey(int symbol)
        {
            if (symbol >= XK_LOWER_A && symbol <= XK_LOWER_Z)
            {
                return Key.A + (symbol - XK_LOWER_A);
            }

            if (symbol >= XK_A && symbol <= XK_Z)
            {
                return Key.A + (symbol - XK_A);
            }

            if (symbol >= XK_0 && symbol <= XK_9)
            {
                return Key.Digit0 + (symbol - XK_0);
            }

            if (symbol >= XK_KP_0 && symbol <= XK_KP_9)
            {
                return Key.Digit0 + (symbol - XK_KP_0);
            }

            if (symbol >= XK_F1 && symbol <= XK_F12)
            {
                return Key.F1 + (symbol - XK_F1);
            }

            switch (symbol)
            {
                case XK_SPACE: return Key.Space;
                case XK_BACKSPACE: return Key.Backspace;
                case XK_TAB: return Key.Tab;
                case XK_ISO_LEFT_TAB: return Key.Tab;
                case XK_RETURN: return Key.Enter;
                case XK_KP_ENTER: return Key.Enter;
                case XK_ESCAPE: return Key.Escape;
                case XK_DELETE: return Key.Delete;
                case XK_KP_DELETE: return Key.Delete;
                case XK_INSERT: return Key.Insert;
                case XK_KP_INSERT: return Key.Insert;

                case XK_LEFT: return Key.Left;
                case XK_KP_LEFT: return Key.Left;
                case XK_RIGHT: return Key.Right;
                case XK_KP_RIGHT: return Key.Right;
                case XK_UP: return Key.Up;
                case XK_KP_UP: return Key.Up;
                case XK_DOWN: return Key.Down;
                case XK_KP_DOWN: return Key.Down;
                case XK_HOME: return Key.Home;
                case XK_KP_HOME: return Key.Home;
                case XK_END: return Key.End;
                case XK_KP_END: return Key.End;
                case XK_PAGE_UP: return Key.PageUp;
                case XK_KP_PAGE_UP: return Key.PageUp;
                case XK_PAGE_DOWN: return Key.PageDown;
                case XK_KP_PAGE_DOWN: return Key.PageDown;

                case XK_SHIFT_L: return Key.Shift;
                case XK_SHIFT_R: return Key.Shift;
                case XK_CONTROL_L: return Key.Control;
                case XK_CONTROL_R: return Key.Control;
                case XK_ALT_L: return Key.Alt;
                case XK_ALT_R: return Key.Alt;
                case XK_META_L: return Key.Meta;
                case XK_META_R: return Key.Meta;
                case XK_SUPER_L: return Key.Meta;
                case XK_SUPER_R: return Key.Meta;

                default: return Key.None;
            }
        }
    }
}
