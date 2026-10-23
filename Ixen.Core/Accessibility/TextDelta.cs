namespace Ixen.Core.Accessibility
{
    internal readonly struct TextDelta
    {
        private TextDelta(int start, int removed, int added)
        {
            Start = start;
            Removed = removed;
            Added = added;
        }

        internal int Start { get; }

        internal int Removed { get; }

        internal int Added { get; }

        internal bool IsEmpty => Removed == 0 && Added == 0;

        internal static TextDelta Of(string before, string after)
        {
            before = before ?? string.Empty;
            after = after ?? string.Empty;

            int prefix = 0;
            int shortest = before.Length < after.Length ? before.Length : after.Length;

            while (prefix < shortest && before[prefix] == after[prefix])
            {
                prefix++;
            }

            if (prefix > 0 && prefix < shortest && char.IsHighSurrogate(before[prefix - 1]))
            {
                prefix--;
            }

            int suffix = 0;

            while (suffix < shortest - prefix
                && before[before.Length - 1 - suffix] == after[after.Length - 1 - suffix])
            {
                suffix++;
            }

            if (suffix > 0 && suffix < shortest - prefix && char.IsLowSurrogate(before[before.Length - suffix]))
            {
                suffix--;
            }

            return new TextDelta(prefix, before.Length - prefix - suffix, after.Length - prefix - suffix);
        }
    }
}
