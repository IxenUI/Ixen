using IxenKey = Ixen.Core.Input.Key;
using Ixen.Core.Input;
using System.Windows.Input;
using WpfKey = System.Windows.Input.Key;

namespace Ixen.View.WPF
{
    internal static class WpfKeys
    {
        internal static KeyModifiers ToModifiers(ModifierKeys modifiers)
        {
            KeyModifiers result = KeyModifiers.None;

            if ((modifiers & ModifierKeys.Shift) != 0)
            {
                result |= KeyModifiers.Shift;
            }

            if ((modifiers & ModifierKeys.Control) != 0)
            {
                result |= KeyModifiers.Control;
            }

            if ((modifiers & ModifierKeys.Alt) != 0)
            {
                result |= KeyModifiers.Alt;
            }

            return result;
        }

        internal static IxenKey ToKey(WpfKey key)
        {
            if (key >= WpfKey.A && key <= WpfKey.Z)
            {
                return IxenKey.A + (key - WpfKey.A);
            }

            if (key >= WpfKey.D0 && key <= WpfKey.D9)
            {
                return IxenKey.Digit0 + (key - WpfKey.D0);
            }

            if (key >= WpfKey.NumPad0 && key <= WpfKey.NumPad9)
            {
                return IxenKey.Digit0 + (key - WpfKey.NumPad0);
            }

            if (key >= WpfKey.F1 && key <= WpfKey.F12)
            {
                return IxenKey.F1 + (key - WpfKey.F1);
            }

            switch (key)
            {
                case WpfKey.Escape: return IxenKey.Escape;
                case WpfKey.Tab: return IxenKey.Tab;
                case WpfKey.Return: return IxenKey.Enter;
                case WpfKey.Space: return IxenKey.Space;
                case WpfKey.Back: return IxenKey.Backspace;
                case WpfKey.Delete: return IxenKey.Delete;
                case WpfKey.Insert: return IxenKey.Insert;

                case WpfKey.Left: return IxenKey.Left;
                case WpfKey.Right: return IxenKey.Right;
                case WpfKey.Up: return IxenKey.Up;
                case WpfKey.Down: return IxenKey.Down;
                case WpfKey.Home: return IxenKey.Home;
                case WpfKey.End: return IxenKey.End;
                case WpfKey.PageUp: return IxenKey.PageUp;
                case WpfKey.PageDown: return IxenKey.PageDown;

                case WpfKey.LeftShift:
                case WpfKey.RightShift:
                    return IxenKey.Shift;

                case WpfKey.LeftCtrl:
                case WpfKey.RightCtrl:
                    return IxenKey.Control;

                case WpfKey.LeftAlt:
                case WpfKey.RightAlt:
                case WpfKey.System:
                    return IxenKey.Alt;

                default: return IxenKey.None;
            }
        }
    }
}
