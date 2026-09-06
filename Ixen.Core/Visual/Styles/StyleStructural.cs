using System;
using System.Collections.Generic;
using System.Globalization;

namespace Ixen.Core.Visual.Styles
{
    [Flags]
    internal enum StructuralKinds
    {
        None = 0,
        First = 1,
        Last = 2,
        Only = 4,
        Nth = 8,
        Odd = 16,
        Even = 32,
        Formula = 64
    }

    internal readonly struct NthFormula
    {
        internal string Argument { get; }
        internal int Step { get; }
        internal int Offset { get; }

        internal NthFormula(string argument, int step, int offset)
        {
            Argument = argument;
            Step = step;
            Offset = offset;
        }

        internal bool Matches(int position) => StyleStructural.Matches(Step, Offset, position);
    }

    internal static class StyleStructural
    {
        internal const string FIRST_CHILD = "first-child";
        internal const string LAST_CHILD = "last-child";
        internal const string ONLY_CHILD = "only-child";
        internal const string NTH_CHILD = "nth-child";
        internal const string ODD = "odd";
        internal const string EVEN = "even";

        private const char STEP_MARKER = 'n';
        private const char PLUS = '+';
        private const char MINUS = '-';
        private const char CLOSE = ')';
        private const string NTH_OPEN = NTH_CHILD + "(";

        internal static readonly string[] All =
        {
            FIRST_CHILD, LAST_CHILD, ONLY_CHILD, NTH_CHILD
        };

        internal static bool Position(VisualElement element, out int index, out int count)
        {
            index = 0;
            count = 0;

            VisualElement parent = element?.Parent;

            if (parent == null)
            {
                return false;
            }

            IReadOnlyList<VisualElement> children = parent.ChildElements;
            int at = element.ChildIndex;

            if (at < 0 || at >= children.Count || children[at] != element)
            {
                return false;
            }

            index = at;
            count = children.Count;

            return true;
        }

        internal static bool Holds(VisualElement element, string pseudo)
        {
            if (pseudo == null || !Position(element, out int index, out int count))
            {
                return false;
            }

            switch (pseudo)
            {
                case FIRST_CHILD:
                    return Holds(StructuralKinds.First, index, count);

                case LAST_CHILD:
                    return Holds(StructuralKinds.Last, index, count);

                case ONLY_CHILD:
                    return Holds(StructuralKinds.Only, index, count);
            }

            if (!IsNth(pseudo, out string argument))
            {
                return false;
            }

            switch (argument)
            {
                case ODD:
                    return Holds(StructuralKinds.Odd, index, count);

                case EVEN:
                    return Holds(StructuralKinds.Even, index, count);
            }

            if (TryWhole(argument, out int wanted))
            {
                return wanted == index + 1;
            }

            return TryFormula(argument, out int step, out int offset)
                && Matches(step, offset, index + 1);
        }

        internal static bool Holds(StructuralKinds kind, int index, int count)
        {
            switch (kind)
            {
                case StructuralKinds.First:
                    return index == 0;

                case StructuralKinds.Last:
                    return index == count - 1;

                case StructuralKinds.Only:
                    return count == 1;

                case StructuralKinds.Odd:
                    return (index + 1) % 2 == 1;

                case StructuralKinds.Even:
                    return (index + 1) % 2 == 0;
            }

            return false;
        }

        internal static bool Matches(int step, int offset, int position)
        {
            if (step == 0)
            {
                return position == offset;
            }

            int reach = position - offset;

            if (reach % step != 0)
            {
                return false;
            }

            return step > 0 ? reach >= 0 : reach <= 0;
        }

        internal static bool IsArgumentValid(string argument)
            => argument == ODD || argument == EVEN
                || TryWhole(argument, out _)
                || TryFormula(argument, out _, out _);

        internal static bool TryFormula(string argument, out int step, out int offset)
        {
            step = 0;
            offset = 0;

            if (string.IsNullOrEmpty(argument))
            {
                return false;
            }

            int marker = argument.IndexOf(STEP_MARKER);

            if (marker < 0)
            {
                return false;
            }

            string head = argument.Substring(0, marker);
            string tail = argument.Substring(marker + 1);

            if (head.Length == 0 || head == "+")
            {
                step = 1;
            }
            else if (head == "-")
            {
                step = -1;
            }
            else if (!TryWhole(head, out step))
            {
                return false;
            }

            if (tail.Length == 0)
            {
                return true;
            }

            return (tail[0] == PLUS || tail[0] == MINUS) && TryWhole(tail, out offset);
        }

        internal static bool NextArgument(string selector, ref int at, out string argument)
        {
            argument = null;

            if (selector == null || at < 0)
            {
                return false;
            }

            int open = selector.IndexOf(NTH_OPEN, at, StringComparison.Ordinal);

            if (open < 0)
            {
                return false;
            }

            open += NTH_OPEN.Length;

            int close = selector.IndexOf(CLOSE, open);

            if (close < 0)
            {
                return false;
            }

            argument = selector.Substring(open, close - open);
            at = close + 1;

            return true;
        }

        internal static StructuralKinds Scan(string selector, List<NthFormula> formulas)
        {
            if (selector == null || selector.IndexOf(':') < 0)
            {
                return StructuralKinds.None;
            }

            StructuralKinds kinds = StructuralKinds.None;

            if (Mentions(selector, FIRST_CHILD))
            {
                kinds |= StructuralKinds.First;
            }

            if (Mentions(selector, LAST_CHILD))
            {
                kinds |= StructuralKinds.Last;
            }

            if (Mentions(selector, ONLY_CHILD))
            {
                kinds |= StructuralKinds.Only;
            }

            int at = 0;

            while (NextArgument(selector, ref at, out string argument))
            {
                if (argument == ODD)
                {
                    kinds |= StructuralKinds.Odd;
                }
                else if (argument == EVEN)
                {
                    kinds |= StructuralKinds.Even;
                }
                else if (TryWhole(argument, out _))
                {
                    kinds |= StructuralKinds.Nth;
                }
                else if (TryFormula(argument, out int step, out int offset))
                {
                    kinds |= StructuralKinds.Formula;
                    Remember(formulas, argument, step, offset);
                }
            }

            return kinds;
        }

        private static void Remember(List<NthFormula> formulas, string argument, int step, int offset)
        {
            if (formulas == null)
            {
                return;
            }

            for (int i = 0; i < formulas.Count; i++)
            {
                if (formulas[i].Argument == argument)
                {
                    return;
                }
            }

            formulas.Add(new NthFormula(argument, step, offset));
        }

        private static bool IsNth(string pseudo, out string argument)
        {
            argument = null;

            if (!pseudo.StartsWith(NTH_OPEN, StringComparison.Ordinal)
                || pseudo[pseudo.Length - 1] != CLOSE)
            {
                return false;
            }

            argument = pseudo.Substring(NTH_OPEN.Length, pseudo.Length - NTH_OPEN.Length - 1);

            return true;
        }

        private static bool TryWhole(string text, out int value)
            => int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);

        private static bool Mentions(string selector, string pseudo)
            => selector.IndexOf(":" + pseudo, StringComparison.Ordinal) >= 0;
    }
}
