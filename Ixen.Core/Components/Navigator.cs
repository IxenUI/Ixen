using System;
using System.Collections.Generic;

namespace Ixen.Core.Components
{
    public class Navigator
    {
        public const string ROOT = "/";
        public const int DEFAULT_LIMIT = 100;

        private const char SEPARATOR = '/';
        private const char PARAMETER_OPEN = '{';
        private const char PARAMETER_CLOSE = '}';
        private static readonly char[] _separators = { SEPARATOR };

        private readonly List<string> _stack = new List<string>();
        private Dictionary<string, string> _parameters;
        private int _limit = DEFAULT_LIMIT;

        public Navigator()
            : this(ROOT)
        { }

        public Navigator(string path)
        {
            _stack.Add(Normalized(path));
        }

        public event EventHandler Changed;

        public string Path => _stack[_stack.Count - 1];

        public string Previous => CanGoBack ? _stack[_stack.Count - 2] : null;

        public int Depth => _stack.Count;

        public bool CanGoBack => _stack.Count > 1;

        public int Limit
        {
            get => _limit;
            set
            {
                _limit = value < 1 ? 1 : value;
                Trim();
            }
        }

        public void Navigate(string path)
        {
            _stack.Add(Normalized(path));
            Trim();
            Raise();
        }

        public void Replace(string path)
        {
            string wanted = Normalized(path);

            if (wanted == Path)
            {
                return;
            }

            _stack[_stack.Count - 1] = wanted;
            Raise();
        }

        public bool Back()
        {
            if (!CanGoBack)
            {
                return false;
            }

            _stack.RemoveAt(_stack.Count - 1);
            Raise();

            return true;
        }

        public void Reset(string path)
        {
            string wanted = Normalized(path);

            if (_stack.Count == 1 && wanted == Path)
            {
                return;
            }

            _stack.Clear();
            _stack.Add(wanted);
            Raise();
        }

        public bool Is(string pattern)
        {
            string[] wanted = Segments(pattern);
            string[] actual = Segments(Path);

            if (wanted.Length != actual.Length)
            {
                return false;
            }

            Dictionary<string, string> captured = null;

            for (int index = 0; index < wanted.Length; index++)
            {
                string name = NameOf(wanted[index]);

                if (name == null)
                {
                    if (!string.Equals(wanted[index], actual[index],
                        StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }

                    continue;
                }

                if (captured == null)
                {
                    captured = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                }

                captured[name] = actual[index];
            }

            _parameters = captured;

            return true;
        }

        public string Get(string name)
        {
            if (_parameters == null || name == null)
            {
                return null;
            }

            return _parameters.TryGetValue(name, out string value) ? value : null;
        }

        public static string Normalized(string path)
        {
            string trimmed = path?.Trim();

            if (string.IsNullOrEmpty(trimmed))
            {
                return ROOT;
            }

            if (trimmed[0] != SEPARATOR)
            {
                trimmed = SEPARATOR + trimmed;
            }

            while (trimmed.Length > 1 && trimmed[trimmed.Length - 1] == SEPARATOR)
            {
                trimmed = trimmed.Substring(0, trimmed.Length - 1);
            }

            return trimmed;
        }

        private static string[] Segments(string path)
        {
            return Normalized(path).Split(_separators, StringSplitOptions.RemoveEmptyEntries);
        }

        private static string NameOf(string segment)
        {
            if (segment.Length <= 2
                || segment[0] != PARAMETER_OPEN
                || segment[segment.Length - 1] != PARAMETER_CLOSE)
            {
                return null;
            }

            return segment.Substring(1, segment.Length - 2);
        }

        private void Trim()
        {
            while (_stack.Count > _limit)
            {
                _stack.RemoveAt(0);
            }
        }

        private void Raise()
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}
