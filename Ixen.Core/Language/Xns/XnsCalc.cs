using Ixen.Core.Language.Base;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Ixen.Core.Language.Xns
{
    internal static class XnsCalc
    {
        internal const string KEYWORD = "calc";

        private const string PIXELS = "px";
        private const string PERCENTS = "%";

        private struct Term
        {
            internal float Pixels;
            internal float Percents;
            internal bool Unitless;

            internal static Term Plain(float number)
                => new Term { Pixels = number, Unitless = true };

            internal static Term Of(float number, string unit)
            {
                if (unit == PERCENTS)
                {
                    return new Term { Percents = number };
                }

                return unit == PIXELS ? new Term { Pixels = number } : Plain(number);
            }

            internal Term Scaled(float factor)
                => new Term
                {
                    Pixels = Pixels * factor,
                    Percents = Percents * factor,
                    Unitless = Unitless
                };

            internal bool IsZero => Pixels == 0 && Percents == 0;

            internal string Describe()
                => Unitless ? "a plain number" : Percents == 0 ? PIXELS : PERCENTS;
        }

        private class Reader
        {
            internal string Text;
            internal int Position;
            internal string Failure;

            internal char Current => Position < Text.Length ? Text[Position] : '\0';

            internal void SkipSpaces()
            {
                while (Position < Text.Length && char.IsWhiteSpace(Text[Position]))
                {
                    Position++;
                }
            }

            internal void Fail(string message)
            {
                if (Failure == null)
                {
                    Failure = message;
                }
            }
        }

        internal static string Evaluate(string text, int index, List<LanguageError> errors)
        {
            if (text == null || text.IndexOf(KEYWORD) < 0)
            {
                return text;
            }

            var result = new StringBuilder();
            int position = 0;

            while (position < text.Length)
            {
                int start = IndexOfCall(text, position);

                if (start < 0)
                {
                    result.Append(text, position, text.Length - position);
                    break;
                }

                int open = start + KEYWORD.Length;
                int close = MatchingParenthesis(text, open);

                if (close < 0)
                {
                    Report(errors, index + start, text.Length - start,
                        $"'{KEYWORD}' is missing its closing parenthesis.");

                    return text;
                }

                result.Append(text, position, start - position);

                string inner = text.Substring(open + 1, close - open - 1);
                string value = EvaluateExpression(inner, out string failure);

                if (value == null)
                {
                    Report(errors, index + start, close - start + 1, failure);
                    return text;
                }

                result.Append(value);
                position = close + 1;
            }

            return result.ToString();
        }

        private static int IndexOfCall(string text, int from)
        {
            for (int index = from; index + KEYWORD.Length < text.Length; index++)
            {
                if (string.CompareOrdinal(text, index, KEYWORD, 0, KEYWORD.Length) != 0)
                {
                    continue;
                }

                if (index > 0 && (char.IsLetterOrDigit(text[index - 1]) || text[index - 1] == '_'))
                {
                    continue;
                }

                int after = index + KEYWORD.Length;

                while (after < text.Length && char.IsWhiteSpace(text[after]))
                {
                    after++;
                }

                if (after < text.Length && text[after] == '(')
                {
                    return index;
                }
            }

            return -1;
        }

        private static int MatchingParenthesis(string text, int from)
        {
            int depth = 0;

            for (int index = from; index < text.Length; index++)
            {
                if (text[index] == '(')
                {
                    depth++;
                    continue;
                }

                if (text[index] == ')' && --depth == 0)
                {
                    return index;
                }
            }

            return -1;
        }

        private static string EvaluateExpression(string text, out string failure)
        {
            var reader = new Reader { Text = text };

            Term? term = ReadSum(reader);

            reader.SkipSpaces();

            if (term == null || reader.Failure != null)
            {
                failure = reader.Failure ?? $"'{text.Trim()}' is not an expression.";
                return null;
            }

            if (reader.Position < text.Length)
            {
                failure = $"'{text.Trim()}' has trailing characters.";
                return null;
            }

            Term value = term.Value;

            bool mixed = value.Percents != 0 && value.Pixels != 0;

            if (!mixed && (value.Pixels < 0 || value.Percents < 0))
            {
                failure = $"'{text.Trim()}' is negative, and no XNS value may be.";
                return null;
            }

            failure = null;

            if (!mixed)
            {
                float single = value.Percents != 0 ? value.Percents : value.Pixels;

                return Number(single)
                    + (value.Unitless ? string.Empty
                        : value.Percents != 0 ? PERCENTS : PIXELS);
            }

            return Number(value.Percents) + PERCENTS
                + (value.Pixels < 0 ? "-" : "+") + Number(Abs(value.Pixels)) + PIXELS;
        }

        private static float Abs(float value) => value < 0 ? -value : value;

        private static string Number(float value)
            => value.ToString("0.####", CultureInfo.InvariantCulture);

        private static Term? ReadSum(Reader reader)
        {
            Term? left = ReadProduct(reader);

            while (left != null)
            {
                reader.SkipSpaces();

                char op = reader.Current;

                if (op != '+' && op != '-')
                {
                    break;
                }

                reader.Position++;

                Term? right = ReadProduct(reader);

                if (right == null)
                {
                    return null;
                }

                Term a = left.Value;
                Term b = right.Value;

                if (a.Unitless != b.Unitless && !a.IsZero && !b.IsZero)
                {
                    reader.Fail($"{a.Describe()} and {b.Describe()} cannot be added: one of them"
                        + " carries no unit.");

                    return null;
                }

                float sign = op == '+' ? 1f : -1f;

                left = new Term
                {
                    Pixels = a.Pixels + b.Pixels * sign,
                    Percents = a.Percents + b.Percents * sign,
                    Unitless = a.Unitless && b.Unitless
                };
            }

            return left;
        }

        private static Term? ReadProduct(Reader reader)
        {
            Term? left = ReadFactor(reader);

            while (left != null)
            {
                reader.SkipSpaces();

                char op = reader.Current;

                if (op != '*' && op != '/')
                {
                    break;
                }

                reader.Position++;

                Term? right = ReadFactor(reader);

                if (right == null)
                {
                    return null;
                }

                Term a = left.Value;
                Term b = right.Value;

                if (op == '*')
                {
                    if (!a.Unitless && !b.Unitless)
                    {
                        reader.Fail($"{a.Describe()} cannot multiply {b.Describe()}: one side"
                            + " must be a plain number.");

                        return null;
                    }

                    left = a.Unitless ? b.Scaled(a.Pixels) : a.Scaled(b.Pixels);

                    continue;
                }

                if (!b.Unitless)
                {
                    reader.Fail($"a division by {b.Describe()} is not a size: the divisor must"
                        + " be a plain number.");

                    return null;
                }

                if (b.Pixels == 0)
                {
                    reader.Fail("a division by zero.");
                    return null;
                }

                left = a.Scaled(1f / b.Pixels);
            }

            return left;
        }

        private static Term? ReadFactor(Reader reader)
        {
            reader.SkipSpaces();

            if (reader.Current == '-')
            {
                reader.Position++;

                Term? inner = ReadFactor(reader);

                if (inner == null)
                {
                    return null;
                }

                return inner.Value.Scaled(-1f);
            }

            if (reader.Current == '(')
            {
                reader.Position++;

                Term? inner = ReadSum(reader);

                reader.SkipSpaces();

                if (inner == null)
                {
                    return null;
                }

                if (reader.Current != ')')
                {
                    reader.Fail("a parenthesis is not closed.");
                    return null;
                }

                reader.Position++;

                return inner;
            }

            if (SkipKeyword(reader))
            {
                return ReadFactor(reader);
            }

            return ReadNumber(reader);
        }

        private static bool SkipKeyword(Reader reader)
        {
            if (reader.Position + KEYWORD.Length > reader.Text.Length
                || string.CompareOrdinal(reader.Text, reader.Position, KEYWORD, 0, KEYWORD.Length) != 0)
            {
                return false;
            }

            int after = reader.Position + KEYWORD.Length;

            while (after < reader.Text.Length && char.IsWhiteSpace(reader.Text[after]))
            {
                after++;
            }

            if (after >= reader.Text.Length || reader.Text[after] != '(')
            {
                return false;
            }

            reader.Position = after;

            return true;
        }

        private static Term? ReadNumber(Reader reader)
        {
            reader.SkipSpaces();

            int start = reader.Position;

            while (reader.Position < reader.Text.Length
                && (char.IsDigit(reader.Text[reader.Position]) || reader.Text[reader.Position] == '.'))
            {
                reader.Position++;
            }

            if (reader.Position == start)
            {
                reader.Fail($"'{reader.Text.Trim()}' is not an expression.");
                return null;
            }

            string digits = reader.Text.Substring(start, reader.Position - start);

            if (!float.TryParse(digits, NumberStyles.Float, CultureInfo.InvariantCulture, out float number))
            {
                reader.Fail($"'{digits}' is not a number.");
                return null;
            }

            string unit = string.Empty;

            if (reader.Position < reader.Text.Length && reader.Text[reader.Position] == '%')
            {
                unit = PERCENTS;
                reader.Position++;
            }
            else if (reader.Position + 1 < reader.Text.Length
                && string.CompareOrdinal(reader.Text, reader.Position, PIXELS, 0, PIXELS.Length) == 0)
            {
                unit = PIXELS;
                reader.Position += PIXELS.Length;
            }

            return Term.Of(number, unit);
        }

        private static void Report(List<LanguageError> errors, int index, int length, string message)
            => errors.Add(new LanguageError(LanguageErrorCode.INVALID_STYLE_VALUE, message, index, length));
    }
}
