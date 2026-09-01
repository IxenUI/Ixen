using Ixen.Core.Visual.Styles;
using System;
using System.Collections.Generic;

namespace Ixen.Core.Visual.Classes
{
    internal enum StyleScopeSegmentKind
    {
        Name,
        Class,
        Type
    }

    internal readonly struct StyleScopeSegment
    {
        internal StyleScopeSegmentKind Kind { get; }
        internal string Value { get; }
        internal string State { get; }
        internal bool Immediate { get; }

        internal StyleScopeSegment(StyleScopeSegmentKind kind, string value, string state,
            bool immediate)
        {
            Kind = kind;
            Value = value;
            State = state;
            Immediate = immediate;
        }

        internal bool Matches(VisualElement element)
        {
            if (State != null && !element.HasState(State)
                && !StyleStructural.Holds(element, State))
            {
                return false;
            }

            switch (Kind)
            {
                case StyleScopeSegmentKind.Class:
                    return element.Classes != null && element.Classes.Contains(Value);

                case StyleScopeSegmentKind.Type:
                    return string.Equals(element.TypeName, Value, StringComparison.Ordinal);

                default:
                    return string.Equals(element.Name, Value, StringComparison.Ordinal);
            }
        }
    }

    internal static class StyleScope
    {
        internal const string SEPARATOR = "/";
        internal const char IMMEDIATE = '>';

        internal static bool IsImmediate(string selector)
            => !string.IsNullOrEmpty(selector) && selector[0] == IMMEDIATE;

        internal static string Bare(string selector)
            => IsImmediate(selector) ? selector.Substring(1) : selector;

        private static readonly char[] _separators = { '/' };

        internal static string Build<TNode>(TNode node, Func<TNode, TNode> parentOf, Func<TNode, string> nameOf)
            where TNode : class
        {
            var names = new List<string>();
            TNode below = node;

            for (TNode current = parentOf(node); current != null; current = parentOf(current))
            {
                string name = nameOf(current);

                if (!string.IsNullOrEmpty(name))
                {
                    names.Add(IsImmediate(nameOf(below)) ? IMMEDIATE + Bare(name) : Bare(name));
                    below = current;
                }
            }

            if (names.Count == 0)
            {
                return null;
            }

            names.Reverse();

            return string.Join(SEPARATOR, names);
        }

        internal static StyleScopeSegment[] Parse(string scope)
        {
            if (string.IsNullOrEmpty(scope))
            {
                return Array.Empty<StyleScopeSegment>();
            }

            string[] parts = scope.Split(_separators, StringSplitOptions.RemoveEmptyEntries);
            var segments = new StyleScopeSegment[parts.Length];

            for (int i = 0; i < parts.Length; i++)
            {
                bool immediate = IsImmediate(parts[i]);
                string part = SplitState(Bare(parts[i]), out string state);

                if (part[0] == '.')
                {
                    segments[i] = new StyleScopeSegment(StyleScopeSegmentKind.Class, part.Substring(1), state, immediate);
                }
                else if (part[0] == '#')
                {
                    segments[i] = new StyleScopeSegment(StyleScopeSegmentKind.Type, part.Substring(1), state, immediate);
                }
                else
                {
                    segments[i] = new StyleScopeSegment(StyleScopeSegmentKind.Name, part, state, immediate);
                }
            }

            return segments;
        }

        internal const char STATE_SEPARATOR = ':';

        internal static string SplitState(string selector, out string state)
        {
            int separator = selector.IndexOf(STATE_SEPARATOR);

            if (separator < 0)
            {
                state = null;
                return selector;
            }

            state = selector.Substring(separator + 1);

            return selector.Substring(0, separator);
        }

        internal static bool Matches(StyleScopeSegment[] segments, VisualElement element)
        {
            if (segments == null || segments.Length == 0)
            {
                return true;
            }

            int index = segments.Length - 1;

            for (VisualElement ancestor = element.Parent; ancestor != null && index >= 0; ancestor = ancestor.Parent)
            {
                if (segments[index].Matches(ancestor))
                {
                    index--;
                    continue;
                }

                if (segments[index].Immediate)
                {
                    return false;
                }
            }

            return index < 0;
        }
    }
}
