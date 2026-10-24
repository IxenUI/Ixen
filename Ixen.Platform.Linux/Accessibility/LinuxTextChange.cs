namespace Ixen.Platform.Linux.Accessibility
{
    internal readonly struct LinuxTextChange
    {
        internal LinuxTextChange(bool inserted, int offset, int length, string text)
        {
            Inserted = inserted;
            Offset = offset;
            Length = length;
            Text = text;
        }

        internal bool Inserted { get; }

        internal int Offset { get; }

        internal int Length { get; }

        internal string Text { get; }
    }
}
