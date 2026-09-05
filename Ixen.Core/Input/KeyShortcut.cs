using System;

namespace Ixen.Core.Input
{
    internal readonly struct KeyShortcut
    {
        internal const char SEPARATOR = '+';

        internal Key Key { get; }
        internal KeyModifiers Modifiers { get; }

        private KeyShortcut(Key key, KeyModifiers modifiers)
        {
            Key = key;
            Modifiers = modifiers;
        }

        internal bool Matches(Key key, KeyModifiers modifiers)
            => Key == key && Modifiers == modifiers;

        internal static KeyShortcut Parse(string source)
        {
            if (!TryParse(source, out KeyShortcut shortcut))
            {
                throw new ArgumentException(
                    $"'{source}' is not a shortcut. Write the modifiers and the key joined by "
                        + $"'{SEPARATOR}', as in 'Ctrl{SEPARATOR}S', 'Shift{SEPARATOR}F3' or "
                        + "'Delete'. The modifiers are Ctrl, Shift and Alt, and the key is one of "
                        + $"{nameof(Input.Key)}'s members.",
                    nameof(source));
            }

            return shortcut;
        }

        internal static bool TryParse(string source, out KeyShortcut shortcut)
        {
            shortcut = default;

            if (string.IsNullOrWhiteSpace(source))
            {
                return false;
            }

            string[] parts = source.Split(SEPARATOR);
            var modifiers = KeyModifiers.None;
            var key = Input.Key.None;

            for (int index = 0; index < parts.Length; index++)
            {
                string part = parts[index].Trim();

                if (part.Length == 0)
                {
                    return false;
                }

                if (index < parts.Length - 1)
                {
                    KeyModifiers modifier = ModifierOf(part);

                    if (modifier == KeyModifiers.None || (modifiers & modifier) != 0)
                    {
                        return false;
                    }

                    modifiers |= modifier;
                    continue;
                }

                if (!TryKey(part, out key))
                {
                    return false;
                }
            }

            if (key == Input.Key.None)
            {
                return false;
            }

            shortcut = new KeyShortcut(key, modifiers);

            return true;
        }

        private static KeyModifiers ModifierOf(string part)
        {
            switch (part.ToLowerInvariant())
            {
                case "ctrl":
                case "control":
                    return KeyModifiers.Control;

                case "shift":
                    return KeyModifiers.Shift;

                case "alt":
                    return KeyModifiers.Alt;

                default:
                    return KeyModifiers.None;
            }
        }

        private static bool TryKey(string part, out Key key)
        {
            if (part.Length == 1 && part[0] >= '0' && part[0] <= '9')
            {
                part = "Digit" + part;
            }

            if (!Enum.TryParse(part, true, out key))
            {
                return false;
            }

            switch (key)
            {
                case Input.Key.None:
                case Input.Key.Shift:
                case Input.Key.Control:
                case Input.Key.Alt:
                    key = Input.Key.None;
                    return false;

                default:
                    return true;
            }
        }
    }
}
