using Ixen.Core;
using Ixen.Core.Input;
using System;
using System.Text;

namespace Ixen.Controls
{
    internal sealed class TypeAhead
    {
        internal const int PAUSE = 1000;

        private readonly StringBuilder _typed = new StringBuilder();
        private IDisposable _reset;
        private int _last;
        private bool _matched;

        internal bool Continues(KeyEventArgs args)
        {
            return args.Key == Key.Space && args.Modifiers == KeyModifiers.None && _matched && _typed.Length > 0
                && (_reset != null || unchecked(Environment.TickCount - _last) <= PAUSE);
        }

        internal int Next(string text, int current, int count, Func<int, string> textOf, IScheduler scheduler)
        {
            if (string.IsNullOrEmpty(text) || count <= 0 || textOf == null)
            {
                return -1;
            }

            int now = Environment.TickCount;

            if (scheduler == null && unchecked(now - _last) > PAUSE)
            {
                _typed.Clear();
            }

            _last = now;

            int before = _typed.Length;

            foreach (char c in text)
            {
                if (!char.IsControl(c) && (_typed.Length > 0 || !char.IsWhiteSpace(c)))
                {
                    _typed.Append(c);
                }
            }

            if (_typed.Length == before)
            {
                return -1;
            }

            _reset?.Dispose();
            _reset = scheduler?.Schedule(PAUSE, false, Forget);

            string typed = _typed.ToString();
            bool repeated = typed.Length > 1 && AllSame(typed);
            string wanted = repeated ? typed.Substring(0, 1) : typed;
            int start = typed.Length == 1 || repeated ? current + 1 : Math.Max(current, 0);

            for (int step = 0; step < count; step++)
            {
                int index = ((start + step) % count + count) % count;
                string candidate = textOf(index);

                if (candidate != null && candidate.TrimStart().StartsWith(wanted, StringComparison.CurrentCultureIgnoreCase))
                {
                    _matched = true;
                    return index;
                }
            }

            _matched = false;
            return -1;
        }

        private void Forget()
        {
            _typed.Clear();
            _matched = false;
            _reset = null;
        }

        private static bool AllSame(string typed)
        {
            char first = char.ToUpperInvariant(typed[0]);

            for (int index = 1; index < typed.Length; index++)
            {
                if (char.ToUpperInvariant(typed[index]) != first)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
