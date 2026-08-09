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

        internal StyleScopeSegment(StyleScopeSegmentKind kind, string value)
        {
            Kind = kind;
            Value = value;
        }

        internal bool Matches(VisualElement element)
        {
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

        private static readonly char[] _separators = { '/' };

        internal static string Build<TNode>(TNode node, Func<TNode, TNode> parentOf, Func<TNode, string> nameOf)
            where TNode : class
        {
            var names = new List<string>();

            for (TNode current = parentOf(node); current != null; current = parentOf(current))
            {
                string name = nameOf(current);

                if (!string.IsNullOrEmpty(name))
                {
                    names.Add(name);
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
                string part = parts[i];

                if (part[0] == '.')
                {
                    segments[i] = new StyleScopeSegment(StyleScopeSegmentKind.Class, part.Substring(1));
                }
                else if (part[0] == '#')
                {
                    segments[i] = new StyleScopeSegment(StyleScopeSegmentKind.Type, part.Substring(1));
                }
                else
                {
                    segments[i] = new StyleScopeSegment(StyleScopeSegmentKind.Name, part);
                }
            }

            return segments;
        }

        // Descendant matching: the segments must appear in order among the ancestors,
        // but they do not have to be adjacent nor start at the root.
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
                }
            }

            return index < 0;
        }
    }
}
