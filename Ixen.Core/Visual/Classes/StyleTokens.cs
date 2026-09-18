using Ixen.Core.Visual.Styles;
using System;
using System.Collections.Generic;

namespace Ixen.Core.Visual.Classes
{
    public sealed class StyleTokens
    {
        internal const char MARKER = '$';

        private readonly Dictionary<string, string> _declared = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _defaults = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _overrides = new Dictionary<string, string>();

        private int _version = 1;

        internal int Version => _version;

        public int Count => _declared.Count + _defaults.Count;

        internal static string Key(string name)
            => name != null && name.Length > 0 && name[0] == MARKER ? name : MARKER + name;

        internal void Declare(string name, string value, bool defaults)
        {
            if (string.IsNullOrEmpty(name) || value == null)
            {
                return;
            }

            if (defaults)
            {
                _defaults[Key(name)] = value;
            }
            else
            {
                _declared[Key(name)] = value;
            }

            _version++;
        }

        public void Set(string name, string color)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("A style token needs a name.", nameof(name));
            }

            if (!StyleColors.IsLiteral(color))
            {
                throw new ArgumentException(
                    $"'{color}' is not a colour, so it cannot be the value of the style token "
                    + $"'{name}'. A token holds a literal such as #4C6EF5 or #80000000.",
                    nameof(color));
            }

            _overrides[Key(name)] = color;
            _version++;
        }

        public void Reset(string name)
        {
            if (name != null && _overrides.Remove(Key(name)))
            {
                _version++;
            }
        }

        public void ResetAll()
        {
            if (_overrides.Count == 0)
            {
                return;
            }

            _overrides.Clear();
            _version++;
        }

        public string ValueOf(string name)
            => Resolve(Key(name));

        internal string Resolve(string value)
        {
            if (value == null || value.Length == 0 || value[0] != MARKER)
            {
                return value;
            }

            if (_overrides.Count > 0 && _overrides.TryGetValue(value, out string overridden))
            {
                return overridden;
            }

            if (_declared.TryGetValue(value, out string declared))
            {
                return declared;
            }

            return _defaults.TryGetValue(value, out string fallback) ? fallback : value;
        }
    }
}
