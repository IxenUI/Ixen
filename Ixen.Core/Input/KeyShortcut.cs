using System;

namespace Ixen.Core.Input
{
    internal readonly struct KeyShortcut
    {
        internal const char SEPARATOR = '+';

        internal const KeyModifiers DEFAULT_ACCELERATOR = KeyModifiers.Control;

        private const string ACCELERATOR = "accel";

        internal Key Key { get; }
        internal KeyModifiers Modifiers { get; }
        internal bool UsesAccelerator { get; }

        private KeyShortcut(Key key, KeyModifiers modifiers, bool usesAccelerator)
        {
            Key = key;
            Modifiers = modifiers;
            UsesAccelerator = usesAccelerator;
        }

        internal bool Matches(Key key, KeyModifiers modifiers,
            KeyModifiers accelerator = DEFAULT_ACCELERATOR)
        {
            KeyModifiers expected = UsesAccelerator ? Modifiers | accelerator : Modifiers;

            return Key == key && expected == modifiers;
        }

        internal static string Describe(string source, KeyModifiers accelerator)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                return source;
            }

            string[] parts = source.Split(SEPARATOR);
            bool found = false;

            for (int index = 0; index < parts.Length; index++)
            {
                if (!string.Equals(parts[index].Trim(), ACCELERATOR,
                    StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                parts[index] = NameOf(accelerator);
                found = true;
            }

            return found ? string.Join(SEPARATOR.ToString(), parts) : source;
        }

        private static string NameOf(KeyModifiers accelerator)
        {
            switch (accelerator)
            {
                case KeyModifiers.Meta:
                    return "Cmd";

                case KeyModifiers.Control:
                    return "Ctrl";

                default:
                    return accelerator.ToString();
            }
        }

        internal static KeyShortcut Parse(string source)
        {
            if (!TryParse(source, out KeyShortcut shortcut))
            {
                throw new ArgumentException(
                    $"'{source}' is not a shortcut. Write the modifiers and the key joined by "
                        + $"'{SEPARATOR}', as in 'Ctrl{SEPARATOR}S', 'Shift{SEPARATOR}F3' or "
                        + "'Delete'. The modifiers are Ctrl, Shift, Alt, Cmd and Accel, and the key is one of "
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
            bool accelerated = false;

            for (int index = 0; index < parts.Length; index++)
            {
                string part = parts[index].Trim();

                if (part.Length == 0)
                {
                    return false;
                }

                if (index < parts.Length - 1)
                {
                    if (string.Equals(part, ACCELERATOR, StringComparison.OrdinalIgnoreCase))
                    {
                        if (accelerated)
                        {
                            return false;
                        }

                        accelerated = true;
                        continue;
                    }

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

            if (accelerated && (modifiers & (KeyModifiers.Control | KeyModifiers.Meta)) != 0)
            {
                return false;
            }

            shortcut = new KeyShortcut(key, modifiers, accelerated);

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

                case "cmd":
                case "command":
                case "meta":
                case "super":
                case "win":
                    return KeyModifiers.Meta;

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
                case Input.Key.Meta:
                    key = Input.Key.None;
                    return false;

                default:
                    return true;
            }
        }
    }
}
