using Ixen.Core.Visual.Styles;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ixen.Core.Visual.Classes
{
    internal enum StyleScopeSegmentKind
    {
        Name,
        Class,
        Type
    }

    internal enum StyleScopeRelation
    {
        Descendant,
        Child,
        AdjacentSibling,
        GeneralSibling
    }

    internal readonly struct StyleScopeSegment
    {
        internal StyleScopeSegmentKind Kind { get; }
        internal string Value { get; }
        internal string State { get; }
        internal StyleScopeRelation Relation { get; }
        internal StyleScopeSegment[] Not { get; }

        internal StyleScopeSegment(StyleScopeSegmentKind kind, string value, string state,
            StyleScopeRelation relation, StyleScopeSegment[] not)
        {
            Kind = kind;
            Value = value;
            State = state;
            Relation = relation;
            Not = not;
        }

        internal bool Matches(VisualElement element)
        {
            if (State != null && !element.HasState(State)
                && !StyleStructural.Holds(element, State))
            {
                return false;
            }

            if (Not != null)
            {
                for (int i = 0; i < Not.Length; i++)
                {
                    if (Not[i].Matches(element))
                    {
                        return false;
                    }
                }
            }

            if (Value == null)
            {
                return true;
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
        internal const char ADJACENT = '+';
        internal const char SIBLING = '~';

        internal static bool IsMarker(char c)
            => c == IMMEDIATE || c == ADJACENT || c == SIBLING;

        internal static StyleScopeRelation RelationOf(string selector)
        {
            if (string.IsNullOrEmpty(selector))
            {
                return StyleScopeRelation.Descendant;
            }

            switch (selector[0])
            {
                case IMMEDIATE:
                    return StyleScopeRelation.Child;

                case ADJACENT:
                    return StyleScopeRelation.AdjacentSibling;

                case SIBLING:
                    return StyleScopeRelation.GeneralSibling;

                default:
                    return StyleScopeRelation.Descendant;
            }
        }

        internal static bool IsSibling(string selector)
        {
            StyleScopeRelation relation = RelationOf(selector);

            return relation == StyleScopeRelation.AdjacentSibling
                || relation == StyleScopeRelation.GeneralSibling;
        }

        internal static bool HasSibling(string scope)
        {
            if (string.IsNullOrEmpty(scope))
            {
                return false;
            }

            if (IsSibling(scope))
            {
                return true;
            }

            for (int at = 1; at < scope.Length; at++)
            {
                if (scope[at - 1] == SEPARATOR[0]
                    && (scope[at] == ADJACENT || scope[at] == SIBLING))
                {
                    return true;
                }
            }

            return false;
        }

        internal static string Bare(string selector)
            => !string.IsNullOrEmpty(selector) && IsMarker(selector[0])
                ? selector.Substring(1)
                : selector;

        private static string Marked(string below, string bare)
            => !string.IsNullOrEmpty(below) && IsMarker(below[0]) ? below[0] + bare : bare;

        private static readonly char[] _separators = { '/' };

        internal const char SELECTOR_SEPARATOR = ',';

        private static readonly char[] _selectorSeparators = { SELECTOR_SEPARATOR };

        internal const string NOT_OPEN = ":not(";
        internal const char NOT_CLOSE = ')';
        private const char NOT_GROUP = '(';

        internal static string[] Split(string selector)
        {
            if (selector == null || selector.IndexOf(SELECTOR_SEPARATOR) < 0)
            {
                return new[] { selector };
            }

            List<string> parts = null;
            int depth = 0;
            int start = 0;

            for (int index = 0; index < selector.Length; index++)
            {
                char c = selector[index];

                if (c == '(')
                {
                    depth++;
                    continue;
                }

                if (c == NOT_CLOSE)
                {
                    if (depth > 0)
                    {
                        depth--;
                    }

                    continue;
                }

                if (c != SELECTOR_SEPARATOR || depth > 0)
                {
                    continue;
                }

                if (parts == null)
                {
                    parts = new List<string>();
                }

                if (index > start)
                {
                    parts.Add(selector.Substring(start, index - start));
                }

                start = index + 1;
            }

            if (parts == null)
            {
                return new[] { selector };
            }

            if (start < selector.Length)
            {
                parts.Add(selector.Substring(start));
            }

            return parts.ToArray();
        }

        internal static bool DeclaresNegation(string selector)
            => selector != null && selector.IndexOf(NOT_OPEN, StringComparison.Ordinal) >= 0;

        internal static string SplitNegations(string selector, out string negations)
        {
            negations = null;

            if (!DeclaresNegation(selector))
            {
                return selector;
            }

            var bare = new StringBuilder();
            var taken = new List<string>();
            int at = 0;

            while (at < selector.Length)
            {
                int open = selector.IndexOf(NOT_OPEN, at, StringComparison.Ordinal);

                if (open < 0)
                {
                    bare.Append(selector, at, selector.Length - at);
                    break;
                }

                bare.Append(selector, at, open - at);

                int close = Closing(selector, open + NOT_OPEN.Length);

                if (close < 0)
                {
                    bare.Append(selector, open, selector.Length - open);
                    break;
                }

                taken.Add(selector.Substring(open + NOT_OPEN.Length,
                    close - open - NOT_OPEN.Length));

                at = close + 1;
            }

            if (taken.Count > 0)
            {
                negations = string.Join(SELECTOR_SEPARATOR.ToString(), taken);
            }

            return bare.ToString();
        }

        private static int Closing(string selector, int from)
        {
            int depth = 0;

            for (int at = from; at < selector.Length; at++)
            {
                char c = selector[at];

                if (c == NOT_GROUP)
                {
                    depth++;
                    continue;
                }

                if (c != NOT_CLOSE)
                {
                    continue;
                }

                if (depth == 0)
                {
                    return at;
                }

                depth--;
            }

            return -1;
        }

        internal static StyleScopeSegment[] ParseNegations(string negations)
        {
            if (string.IsNullOrEmpty(negations))
            {
                return null;
            }

            string[] parts = negations.Split(_selectorSeparators, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0)
            {
                return null;
            }

            var segments = new StyleScopeSegment[parts.Length];

            for (int i = 0; i < parts.Length; i++)
            {
                segments[i] = Simple(parts[i], StyleScopeRelation.Descendant);
            }

            return segments;
        }

        private static StyleScopeSegment Simple(string part, StyleScopeRelation relation)
        {
            string bare = SplitNegations(part, out string negations);
            StyleScopeSegment[] not = ParseNegations(negations);
            string value = SplitState(bare, out string state);

            if (value.Length == 0)
            {
                return new StyleScopeSegment(StyleScopeSegmentKind.Name, null, state, relation, not);
            }

            if (value[0] == '.')
            {
                return new StyleScopeSegment(StyleScopeSegmentKind.Class, value.Substring(1),
                    state, relation, not);
            }

            if (value[0] == '#')
            {
                return new StyleScopeSegment(StyleScopeSegmentKind.Type, value.Substring(1),
                    state, relation, not);
            }

            return new StyleScopeSegment(StyleScopeSegmentKind.Name, value, state, relation, not);
        }

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
                segments.Add(Marked(below, Bare(entry)));

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
                segments[i] = Simple(Bare(parts[i]), RelationOf(parts[i]));
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

            VisualElement node = element;

            for (int index = segments.Length - 1; index >= 0; index--)
            {
                node = Step(segments[index], node);

                if (node == null)
                {
                    return false;
                }

                if (index == containerIndex)
                {
                    container = node;
                }
            }

            return true;
        }

        private static VisualElement Step(StyleScopeSegment segment, VisualElement node)
        {
            switch (segment.Relation)
            {
                case StyleScopeRelation.Child:
                    VisualElement parent = node.Parent;

                    return parent != null && segment.Matches(parent) ? parent : null;

                case StyleScopeRelation.AdjacentSibling:
                    VisualElement previous = PreviousSibling(node);

                    return previous != null && segment.Matches(previous) ? previous : null;

                case StyleScopeRelation.GeneralSibling:
                    for (VisualElement sibling = PreviousSibling(node); sibling != null;
                        sibling = PreviousSibling(sibling))
                    {
                        if (segment.Matches(sibling))
                        {
                            return sibling;
                        }
                    }

                    return null;

                default:
                    for (VisualElement ancestor = node.Parent; ancestor != null;
                        ancestor = ancestor.Parent)
                    {
                        if (segment.Matches(ancestor))
                        {
                            return ancestor;
                        }
                    }

                    return null;
            }
        }

        private static VisualElement PreviousSibling(VisualElement element)
        {
            VisualElement parent = element.Parent;

            if (parent == null)
            {
                return null;
            }

            IReadOnlyList<VisualElement> children = parent.ChildElements;
            int at = element.ChildIndex;

            if (at <= 0 || at >= children.Count || children[at] != element)
            {
                return null;
            }

            return children[at - 1];
        }

        internal static bool Holds(StyleScopeSegment[] negations, VisualElement element)
        {
            if (negations == null)
            {
                return true;
            }

            for (int i = 0; i < negations.Length; i++)
            {
                if (negations[i].Matches(element))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
