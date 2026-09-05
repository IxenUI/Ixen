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

        internal const char SELECTOR_SEPARATOR = ',';

        private static readonly char[] _selectorSeparators = { SELECTOR_SEPARATOR };

        internal static bool IsList(string selector)
            => selector != null && selector.IndexOf(SELECTOR_SEPARATOR) >= 0;

        internal static string[] Split(string selector)
            => IsList(selector)
                ? selector.Split(_selectorSeparators, StringSplitOptions.RemoveEmptyEntries)
                : new[] { selector };

        internal static List<string> BuildAll<TNode>(TNode node, string selector,
            Func<TNode, TNode> parentOf, Func<TNode, string> nameOf)
            where TNode : class
        {
            var levels = new List<string[]>();

            for (TNode current = parentOf(node); current != null; current = parentOf(current))
            {
                string name = nameOf(current);

                if (!string.IsNullOrEmpty(name))
                {
                    levels.Add(Split(name));
                }
            }

            if (levels.Count == 0)
            {
                return null;
            }

            var scopes = new List<string>();

            Combine(levels, 0, selector, new List<string>(), scopes);

            return scopes;
        }

        private static void Combine(List<string[]> levels, int level, string below,
            List<string> segments, List<string> scopes)
        {
            if (level == levels.Count)
            {
                var ordered = new string[segments.Count];

                for (int i = 0; i < segments.Count; i++)
                {
                    ordered[i] = segments[segments.Count - 1 - i];
                }

                scopes.Add(string.Join(SEPARATOR, ordered));

                return;
            }

            foreach (string entry in levels[level])
            {
                segments.Add(IsImmediate(below) ? IMMEDIATE + Bare(entry) : Bare(entry));

                Combine(levels, level + 1, entry, segments, scopes);

                segments.RemoveAt(segments.Count - 1);
            }
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
            => Matches(segments, element, -1, out _);

        internal static bool Matches(StyleScopeSegment[] segments, VisualElement element,
            int containerIndex, out VisualElement container)
        {
            container = null;

            if (segments == null || segments.Length == 0)
            {
                return containerIndex < 0;
            }

            int index = segments.Length - 1;

            for (VisualElement ancestor = element.Parent; ancestor != null && index >= 0; ancestor = ancestor.Parent)
            {
                if (segments[index].Matches(ancestor))
                {
                    if (index == containerIndex)
                    {
                        container = ancestor;
                    }

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
