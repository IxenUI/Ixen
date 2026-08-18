using Android.Views;
using Ixen.Core.Input;

namespace Ixen.View.Android
{
    internal static class AndroidKeys
    {
        internal static KeyModifiers ToModifiers(MetaKeyStates state)
        {
            KeyModifiers modifiers = KeyModifiers.None;

            if ((state & MetaKeyStates.ShiftOn) != 0)
            {
                modifiers |= KeyModifiers.Shift;
            }

            if ((state & MetaKeyStates.CtrlOn) != 0)
            {
                modifiers |= KeyModifiers.Control;
            }

            if ((state & MetaKeyStates.AltOn) != 0)
            {
                modifiers |= KeyModifiers.Alt;
            }

            return modifiers;
        }

        internal static Key ToKey(Keycode code)
        {
            switch (code)
            {
                case Keycode.A: return Key.A;
                case Keycode.B: return Key.B;
                case Keycode.C: return Key.C;
                case Keycode.D: return Key.D;
                case Keycode.E: return Key.E;
                case Keycode.F: return Key.F;
                case Keycode.G: return Key.G;
                case Keycode.H: return Key.H;
                case Keycode.I: return Key.I;
                case Keycode.J: return Key.J;
                case Keycode.K: return Key.K;
                case Keycode.L: return Key.L;
                case Keycode.M: return Key.M;
                case Keycode.N: return Key.N;
                case Keycode.O: return Key.O;
                case Keycode.P: return Key.P;
                case Keycode.Q: return Key.Q;
                case Keycode.R: return Key.R;
                case Keycode.S: return Key.S;
                case Keycode.T: return Key.T;
                case Keycode.U: return Key.U;
                case Keycode.V: return Key.V;
                case Keycode.W: return Key.W;
                case Keycode.X: return Key.X;
                case Keycode.Y: return Key.Y;
                case Keycode.Z: return Key.Z;

                case Keycode.Num0: return Key.Digit0;
                case Keycode.Num1: return Key.Digit1;
                case Keycode.Num2: return Key.Digit2;
                case Keycode.Num3: return Key.Digit3;
                case Keycode.Num4: return Key.Digit4;
                case Keycode.Num5: return Key.Digit5;
                case Keycode.Num6: return Key.Digit6;
                case Keycode.Num7: return Key.Digit7;
                case Keycode.Num8: return Key.Digit8;
                case Keycode.Num9: return Key.Digit9;

                case Keycode.F1: return Key.F1;
                case Keycode.F2: return Key.F2;
                case Keycode.F3: return Key.F3;
                case Keycode.F4: return Key.F4;
                case Keycode.F5: return Key.F5;
                case Keycode.F6: return Key.F6;
                case Keycode.F7: return Key.F7;
                case Keycode.F8: return Key.F8;
                case Keycode.F9: return Key.F9;
                case Keycode.F10: return Key.F10;
                case Keycode.F11: return Key.F11;
                case Keycode.F12: return Key.F12;

                case Keycode.Escape: return Key.Escape;
                case Keycode.Tab: return Key.Tab;
                case Keycode.Enter: return Key.Enter;
                case Keycode.NumpadEnter: return Key.Enter;
                case Keycode.Space: return Key.Space;
                case Keycode.Del: return Key.Backspace;
                case Keycode.ForwardDel: return Key.Delete;
                case Keycode.Insert: return Key.Insert;

                case Keycode.DpadLeft: return Key.Left;
                case Keycode.DpadRight: return Key.Right;
                case Keycode.DpadUp: return Key.Up;
                case Keycode.DpadDown: return Key.Down;
                case Keycode.MoveHome: return Key.Home;
                case Keycode.MoveEnd: return Key.End;
                case Keycode.PageUp: return Key.PageUp;
                case Keycode.PageDown: return Key.PageDown;

                case Keycode.ShiftLeft:
                case Keycode.ShiftRight:
                    return Key.Shift;

                case Keycode.CtrlLeft:
                case Keycode.CtrlRight:
                    return Key.Control;

                case Keycode.AltLeft:
                case Keycode.AltRight:
                    return Key.Alt;

                default: return Key.None;
            }
        }

        internal static bool IsSystemKey(Keycode code)
        {
            switch (code)
            {
                case Keycode.Back:
                case Keycode.Home:
                case Keycode.Menu:
                case Keycode.AppSwitch:
                case Keycode.VolumeUp:
                case Keycode.VolumeDown:
                case Keycode.VolumeMute:
                case Keycode.Power:
                case Keycode.Camera:
                case Keycode.Search:
                    return true;

                default:
                    return false;
            }
        }
    }
}
