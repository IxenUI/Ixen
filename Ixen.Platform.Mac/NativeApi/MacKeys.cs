using Ixen.Core.Input;

namespace Ixen.Platform.Mac.NativeApi
{
    internal static class MacKeys
    {
        private const int MOD_SHIFT = 1;
        private const int MOD_CONTROL = 2;
        private const int MOD_ALT = 4;
        private const int MOD_META = 8;

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

        internal static Key ToKey(int keyCode)
        {
            switch (keyCode)
            {
                case 0x00: return Key.A;
                case 0x0B: return Key.B;
                case 0x08: return Key.C;
                case 0x02: return Key.D;
                case 0x0E: return Key.E;
                case 0x03: return Key.F;
                case 0x05: return Key.G;
                case 0x04: return Key.H;
                case 0x22: return Key.I;
                case 0x26: return Key.J;
                case 0x28: return Key.K;
                case 0x25: return Key.L;
                case 0x2E: return Key.M;
                case 0x2D: return Key.N;
                case 0x1F: return Key.O;
                case 0x23: return Key.P;
                case 0x0C: return Key.Q;
                case 0x0F: return Key.R;
                case 0x01: return Key.S;
                case 0x11: return Key.T;
                case 0x20: return Key.U;
                case 0x09: return Key.V;
                case 0x0D: return Key.W;
                case 0x07: return Key.X;
                case 0x10: return Key.Y;
                case 0x06: return Key.Z;

                case 0x1D: return Key.Digit0;
                case 0x12: return Key.Digit1;
                case 0x13: return Key.Digit2;
                case 0x14: return Key.Digit3;
                case 0x15: return Key.Digit4;
                case 0x17: return Key.Digit5;
                case 0x16: return Key.Digit6;
                case 0x1A: return Key.Digit7;
                case 0x1C: return Key.Digit8;
                case 0x19: return Key.Digit9;

                case 0x7A: return Key.F1;
                case 0x78: return Key.F2;
                case 0x63: return Key.F3;
                case 0x76: return Key.F4;
                case 0x60: return Key.F5;
                case 0x61: return Key.F6;
                case 0x62: return Key.F7;
                case 0x64: return Key.F8;
                case 0x65: return Key.F9;
                case 0x6D: return Key.F10;
                case 0x67: return Key.F11;
                case 0x6F: return Key.F12;

                case 0x35: return Key.Escape;
                case 0x30: return Key.Tab;
                case 0x24: return Key.Enter;
                case 0x4C: return Key.Enter;
                case 0x31: return Key.Space;
                case 0x33: return Key.Backspace;
                case 0x75: return Key.Delete;
                case 0x72: return Key.Insert;

                case 0x7B: return Key.Left;
                case 0x7C: return Key.Right;
                case 0x7E: return Key.Up;
                case 0x7D: return Key.Down;
                case 0x73: return Key.Home;
                case 0x77: return Key.End;
                case 0x74: return Key.PageUp;
                case 0x79: return Key.PageDown;

                case 0x38: return Key.Shift;
                case 0x3C: return Key.Shift;
                case 0x3B: return Key.Control;
                case 0x3E: return Key.Control;
                case 0x3A: return Key.Alt;
                case 0x3D: return Key.Alt;
                case 0x37: return Key.Meta;
                case 0x36: return Key.Meta;

                default: return Key.None;
            }
        }
    }
}
