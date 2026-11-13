namespace Ixen.Platform.Windows.NativeApi
{
    internal enum NativePointerKind
    {
        Move = 0,
        Down = 1,
        Up = 2,
        Leave = 3,
        CaptureLost = 4
    }

    internal enum NativeGlStatus
    {
        None = 0,
        Current = 1,
        Recreated = 2
    }

    internal enum NativeSystemChange
    {
        Preferences = 1,
        Dpi = 2
    }

    internal enum NativeDragKind
    {
        Over = 0,
        Leave = 1
    }

    internal enum NativePickKind
    {
        File = 0,
        Files = 1,
        Folder = 2,
        Save = 3
    }

    internal enum NativePointerButton
    {
        None = 0,
        Left = 1,
        Middle = 2,
        Right = 3
    }
}
