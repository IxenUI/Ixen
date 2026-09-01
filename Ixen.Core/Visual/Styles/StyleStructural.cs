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
        Even = 32
    }

    internal static class StyleStructural
    {
        internal const string FIRST_CHILD = "first-child";
        internal const string LAST_CHILD = "last-child";
        internal const string ONLY_CHILD = "only-child";
        internal const string NTH_CHILD = "nth-child";
        internal const string ODD = "odd";
        internal const string EVEN = "even";

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

            if (!pseudo.StartsWith(NTH_OPEN, StringComparison.Ordinal)
                || pseudo[pseudo.Length - 1] != ')')
            {
                return false;
            }

            string argument = pseudo.Substring(NTH_OPEN.Length,
                pseudo.Length - NTH_OPEN.Length - 1);

            switch (argument)
            {
                case ODD:
                    return Holds(StructuralKinds.Odd, index, count);

                case EVEN:
                    return Holds(StructuralKinds.Even, index, count);
            }

            return int.TryParse(argument, NumberStyles.Integer, CultureInfo.InvariantCulture,
                out int wanted) && wanted == index + 1;
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

        internal static StructuralKinds KindsOf(string selector)
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

            for (int at = 0; (at = selector.IndexOf(NTH_OPEN, at, StringComparison.Ordinal)) >= 0;)
            {
                int open = at + NTH_OPEN.Length;
                int close = selector.IndexOf(')', open);

                if (close < 0)
                {
                    break;
                }

                string argument = selector.Substring(open, close - open);

                if (argument == ODD)
                {
                    kinds |= StructuralKinds.Odd;
                }
                else if (argument == EVEN)
                {
                    kinds |= StructuralKinds.Even;
                }
                else
                {
                    kinds |= StructuralKinds.Nth;
                }

                at = close + 1;
            }

            return kinds;
        }

        private static bool Mentions(string selector, string pseudo)
            => selector.IndexOf(":" + pseudo, StringComparison.Ordinal) >= 0;
    }
}
