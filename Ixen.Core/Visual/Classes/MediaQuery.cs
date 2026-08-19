using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Ixen.Core.Visual.Classes
{
    public class MediaQuery
    {
        internal const string AND = "and";
        internal const string MIN_WIDTH = "min-width";
        internal const string MAX_WIDTH = "max-width";
        internal const string MIN_HEIGHT = "min-height";
        internal const string MAX_HEIGHT = "max-height";
        internal const string ORIENTATION = "orientation";
        internal const string PORTRAIT = "portrait";
        internal const string LANDSCAPE = "landscape";

        private const float UNSET = -1f;

        public string Source { get; private set; }

        internal float MinWidth { get; private set; } = UNSET;
        internal float MaxWidth { get; private set; } = UNSET;
        internal float MinHeight { get; private set; } = UNSET;
        internal float MaxHeight { get; private set; } = UNSET;
        internal bool Portrait { get; private set; }
        internal bool Landscape { get; private set; }

        private MediaQuery(string source)
        {
            Source = source;
        }

        public static MediaQuery Parse(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                return null;
            }

            var query = new MediaQuery(source.Trim());

            foreach (string clause in Clauses(source))
            {
                if (!query.Apply(clause))
                {
                    return null;
                }
            }

            if (query.Portrait && query.Landscape)
            {
                return null;
            }

            return query;
        }

        private static readonly Regex _and = new Regex(@"\band\b", RegexOptions.IgnoreCase);

        private static List<string> Clauses(string source)
        {
            var flat = new System.Text.StringBuilder(source.Length);

            foreach (char c in source)
            {
                flat.Append(c == '(' || c == ')' ? ' ' : c);
            }

            var clauses = new List<string>();

            foreach (string part in _and.Split(flat.ToString()))
            {
                string clause = part.Trim();

                if (clause.Length > 0)
                {
                    clauses.Add(clause);
                }
            }

            return clauses;
        }

        private bool Apply(string clause)
        {
            int separator = clause.IndexOf(':');

            if (separator < 0)
            {
                return false;
            }

            string feature = clause.Substring(0, separator).Trim().ToLowerInvariant();
            string value = clause.Substring(separator + 1).Trim();

            if (feature == ORIENTATION)
            {
                if (value == PORTRAIT)
                {
                    Portrait = true;
                    return true;
                }

                if (value == LANDSCAPE)
                {
                    Landscape = true;
                    return true;
                }

                return false;
            }

            if (!TryParseLength(value, out float length))
            {
                return false;
            }

            switch (feature)
            {
                case MIN_WIDTH:
                    MinWidth = length;
                    return true;

                case MAX_WIDTH:
                    MaxWidth = length;
                    return true;

                case MIN_HEIGHT:
                    MinHeight = length;
                    return true;

                case MAX_HEIGHT:
                    MaxHeight = length;
                    return true;

                default:
                    return false;
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
                && float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out length)
                && length >= 0;
        }

        internal bool Matches(float width, float height)
        {
            if (MinWidth >= 0 && width < MinWidth)
            {
                return false;
            }

            if (MaxWidth >= 0 && width > MaxWidth)
            {
                return false;
            }

            if (MinHeight >= 0 && height < MinHeight)
            {
                return false;
            }

            if (MaxHeight >= 0 && height > MaxHeight)
            {
                return false;
            }

            if (Portrait && width > height)
            {
                return false;
            }

            if (Landscape && height > width)
            {
                return false;
            }

            return true;
        }

        internal MediaQuery And(MediaQuery other)
        {
            if (other == null)
            {
                return this;
            }

            var combined = new MediaQuery(Source + " " + AND + " " + other.Source)
            {
                MinWidth = Max(MinWidth, other.MinWidth),
                MaxWidth = Min(MaxWidth, other.MaxWidth),
                MinHeight = Max(MinHeight, other.MinHeight),
                MaxHeight = Min(MaxHeight, other.MaxHeight),
                Portrait = Portrait || other.Portrait,
                Landscape = Landscape || other.Landscape
            };

            return combined;
        }

        private static float Max(float a, float b)
            => a < 0 ? b : b < 0 ? a : (a > b ? a : b);

        private static float Min(float a, float b)
            => a < 0 ? b : b < 0 ? a : (a < b ? a : b);
    }
}
