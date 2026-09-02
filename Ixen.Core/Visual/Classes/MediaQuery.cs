using System.Globalization;

namespace Ixen.Core.Visual.Classes
{
    public class MediaQuery
    {
        internal const string AND = "and";
        internal const string OR = "or";
        internal const string NOT = "not";
        internal const string MIN_WIDTH = "min-width";
        internal const string MAX_WIDTH = "max-width";
        internal const string MIN_HEIGHT = "min-height";
        internal const string MAX_HEIGHT = "max-height";
        internal const string ORIENTATION = "orientation";
        internal const string PORTRAIT = "portrait";
        internal const string LANDSCAPE = "landscape";

        public string Source { get; private set; }

        private readonly MediaTerm _term;

        private MediaQuery(string source, MediaTerm term)
        {
            Source = source;
            _term = term;
        }

        public static MediaQuery Parse(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                return null;
            }

            MediaTerm term = new Reader(source).ReadAll();

            return term == null ? null : new MediaQuery(source.Trim(), term);
        }

        internal bool Matches(float width, float height)
            => _term.Matches(width, height);

        internal MediaQuery And(MediaQuery other)
        {
            if (other == null)
            {
                return this;
            }

            return new MediaQuery(Source + " " + AND + " " + other.Source,
                new MediaAnd(_term, other._term));
        }

        private class Reader
        {
            private readonly string _content;
            private int _index;

            internal Reader(string content)
            {
                _content = content;
            }

            internal MediaTerm ReadAll()
            {
                MediaTerm term = ReadDisjunction();

                SkipSpace();

                return term != null && _index >= _content.Length ? term : null;
            }

            private MediaTerm ReadDisjunction()
            {
                MediaTerm left = ReadConjunction();

                while (left != null)
                {
                    SkipSpace();

                    if (Peek() == ',')
                    {
                        _index++;
                    }
                    else if (!TryWord(OR))
                    {
                        break;
                    }

                    MediaTerm right = ReadConjunction();

                    left = right == null ? null : new MediaOr(left, right);
                }

                return left;
            }

            private MediaTerm ReadConjunction()
            {
                MediaTerm left = ReadNegation();

                while (left != null)
                {
                    SkipSpace();

                    if (!TryWord(AND))
                    {
                        break;
                    }

                    MediaTerm right = ReadNegation();

                    left = right == null ? null : new MediaAnd(left, right);
                }

                return left;
            }

            private MediaTerm ReadNegation()
            {
                SkipSpace();

                if (!TryWord(NOT))
                {
                    return ReadPrimary();
                }

                MediaTerm inner = ReadNegation();

                return inner == null ? null : new MediaNot(inner);
            }

            private MediaTerm ReadPrimary()
            {
                SkipSpace();

                if (Peek() != '(')
                {
                    return Feature(ReadBareFeature());
                }

                int close = MatchingParenthesis(_index);

                if (close < 0)
                {
                    return null;
                }

                string inner = _content.Substring(_index + 1, close - _index - 1);

                _index = close + 1;

                return HasTopLevelColon(inner)
                    ? Feature(inner)
                    : new Reader(inner).ReadAll();
            }

            private string ReadBareFeature()
            {
                int start = _index;

                while (_index < _content.Length)
                {
                    char c = _content[_index];

                    if (c == ',' || c == '(' || c == ')')
                    {
                        break;
                    }

                    if (IsSeparatorWord())
                    {
                        break;
                    }

                    _index++;
                }

                return _content.Substring(start, _index - start);
            }

            private bool IsSeparatorWord()
            {
                if (_index > 0 && IsNameChar(_content[_index - 1]))
                {
                    return false;
                }

                int end = _index;

                while (end < _content.Length && IsNameChar(_content[end]))
                {
                    end++;
                }

                string word = _content.Substring(_index, end - _index).ToLowerInvariant();

                return word == AND || word == OR;
            }

            private int MatchingParenthesis(int opening)
            {
                int depth = 0;

                for (int at = opening; at < _content.Length; at++)
                {
                    if (_content[at] == '(')
                    {
                        depth++;
                        continue;
                    }

                    if (_content[at] != ')')
                    {
                        continue;
                    }

                    depth--;

                    if (depth == 0)
                    {
                        return at;
                    }
                }

                return -1;
            }

            private static bool HasTopLevelColon(string content)
            {
                int depth = 0;

                foreach (char c in content)
                {
                    if (c == '(')
                    {
                        depth++;
                    }
                    else if (c == ')')
                    {
                        depth--;
                    }
                    else if (c == ':' && depth == 0)
                    {
                        return true;
                    }
                }

                return false;
            }

            private bool TryWord(string word)
            {
                SkipSpace();

                int end = _index;

                while (end < _content.Length && IsNameChar(_content[end]))
                {
                    end++;
                }

                if (end == _index
                    || !string.Equals(_content.Substring(_index, end - _index), word,
                        System.StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                _index = end;

                return true;
            }

            private void SkipSpace()
            {
                while (_index < _content.Length && char.IsWhiteSpace(_content[_index]))
                {
                    _index++;
                }
            }

            private char Peek()
                => _index < _content.Length ? _content[_index] : '\0';

            private static bool IsNameChar(char c)
                => char.IsLetterOrDigit(c) || c == '-';

            private static MediaTerm Feature(string clause)
            {
                if (clause == null)
                {
                    return null;
                }

                int separator = clause.IndexOf(':');

                if (separator < 0)
                {
                    return null;
                }

                string feature = clause.Substring(0, separator).Trim().ToLowerInvariant();
                string value = clause.Substring(separator + 1).Trim();

                if (feature == ORIENTATION)
                {
                    if (value == PORTRAIT)
                    {
                        return new MediaFeature(MediaFeatureKind.Portrait, 0);
                    }

                    return value == LANDSCAPE
                        ? new MediaFeature(MediaFeatureKind.Landscape, 0)
                        : null;
                }

                if (!TryParseLength(value, out float length))
                {
                    return null;
                }

                switch (feature)
                {
                    case MIN_WIDTH:
                        return new MediaFeature(MediaFeatureKind.MinWidth, length);

                    case MAX_WIDTH:
                        return new MediaFeature(MediaFeatureKind.MaxWidth, length);

                    case MIN_HEIGHT:
                        return new MediaFeature(MediaFeatureKind.MinHeight, length);

                    case MAX_HEIGHT:
                        return new MediaFeature(MediaFeatureKind.MaxHeight, length);

                    default:
                        return null;
                }
            }

            private static bool TryParseLength(string value, out float length)
            {
                length = 0;

                if (value.Length == 0)
                {
                    return false;
                }

                if (value.EndsWith("px"))
                {
                    value = value.Substring(0, value.Length - 2).Trim();
                }

                return value.Length > 0
                    && float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture,
                        out length)
                    && length >= 0;
            }
        }
    }
}
