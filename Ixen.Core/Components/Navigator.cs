using System;
using System.Collections.Generic;

namespace Ixen.Core.Components
{
    public class Navigator : IObservableState
    {
        public const string ROOT = "/";
        public const int DEFAULT_LIMIT = 100;

        private const char SEPARATOR = '/';
        private const char PARAMETER_OPEN = '{';
        private const char PARAMETER_CLOSE = '}';
        private const char QUERY = '?';
        private const char PAIR = '&';
        private const char ASSIGN = '=';

        private static readonly char[] _separators = { SEPARATOR };
        private static readonly char[] _pairs = { PAIR };

        private readonly List<string> _stack = new List<string>();
        private readonly List<string> _forward = new List<string>();
        private Dictionary<string, string> _parameters;
        private Dictionary<string, string> _query;
        private string _queried;
        private int _limit = DEFAULT_LIMIT;

        public Navigator()
            : this(ROOT)
        { }

        public Navigator(string path)
        {
            _stack.Add(Normalized(path));
        }

        public event EventHandler Changed;

        public string Location => _stack[_stack.Count - 1];

        public string Path => PathOf(Location);

        public string QueryString => QueryOf(Location);

        public string Previous => CanGoBack ? _stack[_stack.Count - 2] : null;

        public string Next => CanGoForward ? _forward[_forward.Count - 1] : null;

        public int Depth => _stack.Count;

        public bool CanGoBack => _stack.Count > 1;

        public bool CanGoForward => _forward.Count > 0;

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
            _forward.Clear();
            _stack.Add(Normalized(path));
            Trim();
            Raise();
        }

        public void Replace(string path)
        {
            string wanted = Normalized(path);

            if (wanted == Location)
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

            _forward.Add(_stack[_stack.Count - 1]);
            _stack.RemoveAt(_stack.Count - 1);
            Trim();
            Raise();

            return true;
        }

        public bool Forward()
        {
            if (!CanGoForward)
            {
                return false;
            }

            _stack.Add(_forward[_forward.Count - 1]);
            _forward.RemoveAt(_forward.Count - 1);
            Trim();
            Raise();

            return true;
        }

        public void Reset(string path)
        {
            string wanted = Normalized(path);

            if (_stack.Count == 1 && _forward.Count == 0 && wanted == Location)
            {
                return;
            }

            _stack.Clear();
            _stack.Add(wanted);
            _forward.Clear();
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

        public string Query(string name)
        {
            if (name == null)
            {
                return null;
            }

            return Parameters().TryGetValue(name, out string value) ? value : null;
        }

        public static string Normalized(string path)
        {
            string trimmed = path?.Trim();

            if (string.IsNullOrEmpty(trimmed))
            {
                return ROOT;
            }

            string query = null;
            int mark = trimmed.IndexOf(QUERY);

            if (mark >= 0)
            {
                query = trimmed.Substring(mark + 1);
                trimmed = trimmed.Substring(0, mark);
            }

            if (trimmed.Length == 0 || trimmed[0] != SEPARATOR)
            {
                trimmed = SEPARATOR + trimmed;
            }

            while (trimmed.Length > 1 && trimmed[trimmed.Length - 1] == SEPARATOR)
            {
                trimmed = trimmed.Substring(0, trimmed.Length - 1);
            }

            return string.IsNullOrEmpty(query) ? trimmed : trimmed + QUERY + query;
        }

        private static string PathOf(string location)
        {
            int mark = location.IndexOf(QUERY);

            return mark < 0 ? location : location.Substring(0, mark);
        }

        private static string QueryOf(string location)
        {
            int mark = location.IndexOf(QUERY);

            return mark < 0 ? null : location.Substring(mark + 1);
        }

        private Dictionary<string, string> Parameters()
        {
            string location = Location;

            if (!string.Equals(_queried, location, StringComparison.Ordinal))
            {
                _queried = location;
                _query = Parsed(QueryOf(location));
            }

            return _query;
        }

        private static Dictionary<string, string> Parsed(string query)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrEmpty(query))
            {
                return values;
            }

            foreach (string entry in query.Split(_pairs, StringSplitOptions.RemoveEmptyEntries))
            {
                int assign = entry.IndexOf(ASSIGN);
                string name = assign < 0 ? entry : entry.Substring(0, assign);

                if (name.Length == 0)
                {
                    continue;
                }

                values[Uri.UnescapeDataString(name)] =
                    assign < 0 ? string.Empty : Uri.UnescapeDataString(entry.Substring(assign + 1));
            }

            return values;
        }

        private static string[] Segments(string path)
        {
            return PathOf(Normalized(path)).Split(_separators, StringSplitOptions.RemoveEmptyEntries);
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

            while (_forward.Count > _limit)
            {
                _forward.RemoveAt(0);
            }
        }

        private void Raise()
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}
