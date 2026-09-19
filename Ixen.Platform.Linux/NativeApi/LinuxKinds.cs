using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Platform.Linux.NativeApi
{
    internal enum LinuxPointerKind
    {
        Move = 0,
        Down = 1,
        Up = 2,
        Leave = 3,
        CaptureLost = 4
    }

    internal enum LinuxPointerButton
    {
        None = 0,
        Left = 1,
        Middle = 2,
        Right = 3
    }

    internal enum LinuxKeyKind
    {
        Down = 0,
        Up = 1
    }

    internal static class LinuxCursors
    {
        internal static int ToNative(CursorKind kind)
        {
            switch (kind)
            {
                case CursorKind.Hand: return 1;
                case CursorKind.Text: return 2;
                case CursorKind.Wait: return 3;
                case CursorKind.Crosshair: return 4;
                case CursorKind.ResizeHorizontal: return 5;
                case CursorKind.ResizeVertical: return 6;
                case CursorKind.ResizeDiagonalUp: return 7;
                case CursorKind.ResizeDiagonalDown: return 8;
                case CursorKind.Move: return 9;
                case CursorKind.NotAllowed: return 10;
                case CursorKind.Help: return 11;
                case CursorKind.Progress: return 12;
                case CursorKind.Hidden: return 13;
                default: return 0;
            }
        }
    }
}
