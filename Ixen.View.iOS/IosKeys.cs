using Ixen.Core.Input;
using UIKit;

namespace Ixen.View.iOS
{
    internal static class IosKeys
    {
        internal static KeyModifiers ToModifiers(UIKeyModifierFlags flags)
        {
            KeyModifiers modifiers = KeyModifiers.None;

            if ((flags & UIKeyModifierFlags.Shift) != 0)
            {
                modifiers |= KeyModifiers.Shift;
            }

            if ((flags & UIKeyModifierFlags.Control) != 0)
            {
                modifiers |= KeyModifiers.Control;
            }

            if ((flags & UIKeyModifierFlags.Alternate) != 0)
            {
                modifiers |= KeyModifiers.Alt;
            }

            if ((flags & UIKeyModifierFlags.Command) != 0)
            {
                modifiers |= KeyModifiers.Meta;
            }

            return modifiers;
        }

        internal static Key ToKey(UIKey key)
        {
            if (key == null)
            {
                return Key.None;
            }

            string unmodified = key.CharactersIgnoringModifiers;

            if (unmodified != null && unmodified.Length == 1)
            {
                char letter = char.ToUpperInvariant(unmodified[0]);

                if (letter >= 'A' && letter <= 'Z')
                {
                    return Key.A + (letter - 'A');
                }
            }

            return ToKey(key.KeyCode);
        }

        internal static Key ToKey(UIKeyboardHidUsage usage)
        {
            int code = (int)usage;

            if (code >= (int)UIKeyboardHidUsage.KeyboardA && code <= (int)UIKeyboardHidUsage.KeyboardZ)
            {
                return Key.A + (code - (int)UIKeyboardHidUsage.KeyboardA);
            }

            if (code >= (int)UIKeyboardHidUsage.Keyboard1 && code < (int)UIKeyboardHidUsage.Keyboard0)
            {
                return Key.Digit1 + (code - (int)UIKeyboardHidUsage.Keyboard1);
            }

            if (code >= (int)UIKeyboardHidUsage.KeyboardF1 && code <= (int)UIKeyboardHidUsage.KeyboardF12)
            {
                return Key.F1 + (code - (int)UIKeyboardHidUsage.KeyboardF1);
            }

            switch (usage)
            {
                case UIKeyboardHidUsage.Keyboard0: return Key.Digit0;
                case UIKeyboardHidUsage.KeyboardEscape: return Key.Escape;
                case UIKeyboardHidUsage.KeyboardTab: return Key.Tab;
                case UIKeyboardHidUsage.KeyboardReturnOrEnter: return Key.Enter;
                case UIKeyboardHidUsage.KeyboardSpacebar: return Key.Space;
                case UIKeyboardHidUsage.KeyboardDeleteOrBackspace: return Key.Backspace;
                case UIKeyboardHidUsage.KeyboardDeleteForward: return Key.Delete;
                case UIKeyboardHidUsage.KeyboardInsert: return Key.Insert;
                case UIKeyboardHidUsage.KeyboardLeftArrow: return Key.Left;
                case UIKeyboardHidUsage.KeyboardRightArrow: return Key.Right;
                case UIKeyboardHidUsage.KeyboardUpArrow: return Key.Up;
                case UIKeyboardHidUsage.KeyboardDownArrow: return Key.Down;
                case UIKeyboardHidUsage.KeyboardHome: return Key.Home;
                case UIKeyboardHidUsage.KeyboardEnd: return Key.End;
                case UIKeyboardHidUsage.KeyboardPageUp: return Key.PageUp;
                case UIKeyboardHidUsage.KeyboardPageDown: return Key.PageDown;
                case UIKeyboardHidUsage.KeyboardLeftShift:
                case UIKeyboardHidUsage.KeyboardRightShift: return Key.Shift;
                case UIKeyboardHidUsage.KeyboardLeftControl:
                case UIKeyboardHidUsage.KeyboardRightControl: return Key.Control;
                case UIKeyboardHidUsage.KeyboardLeftAlt:
                case UIKeyboardHidUsage.KeyboardRightAlt: return Key.Alt;
                case UIKeyboardHidUsage.KeyboardLeftGui:
                case UIKeyboardHidUsage.KeyboardRightGui: return Key.Meta;
                default: return Key.None;
            }
        }

        internal static bool ProducesText(Key key, KeyModifiers modifiers)
        {
            if ((modifiers & (KeyModifiers.Control | KeyModifiers.Meta)) != 0)
            {
                return false;
            }

            switch (key)
            {
                case Key.Escape:
                case Key.Tab:
                case Key.Enter:
                case Key.Backspace:
                case Key.Delete:
                case Key.Insert:
                case Key.Left:
                case Key.Right:
                case Key.Up:
                case Key.Down:
                case Key.Home:
                case Key.End:
                case Key.PageUp:
                case Key.PageDown:
                case Key.Shift:
                case Key.Control:
                case Key.Alt:
                case Key.Meta:
                    return false;
            }

            return key < Key.F1 || key > Key.F12;
        }
    }
}
