using System;
using System.Runtime.InteropServices;

namespace Ixen.Platform.Mac.NativeApi
{
    internal static class MacApi
    {
        const string LIB_NAME = "libixen";

        public delegate void OnPaintCallBack(int width, int height);
        public delegate void OnPointerCallBack(int kind, int x, int y, int button);
        public delegate void OnKeyCallBack(int kind, int keyCode, int modifiers, int repeat);
        public delegate void OnWheelCallBack(int x, int y, int deltaX, int deltaY, int modifiers);
        public delegate void OnTimerCallBack(long id);

        [DllImport(LIB_NAME, EntryPoint = "WA_CreateWindow", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CreateWindow([MarshalAs(UnmanagedType.LPUTF8Str)] string title, int width, int height);

        [DllImport(LIB_NAME, EntryPoint = "WA_ShowWindow", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ShowWindow(IntPtr windowPtr);

        [DllImport(LIB_NAME, EntryPoint = "WA_DestroyWindow", CallingConvention = CallingConvention.Cdecl)]
        public static extern void DestroyWindow(IntPtr windowPtr);

        [DllImport(LIB_NAME, EntryPoint = "WA_SetWindowTitle", CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetWindowTitle(IntPtr windowPtr, [MarshalAs(UnmanagedType.LPUTF8Str)] string title);

        [DllImport(LIB_NAME, EntryPoint = "WA_SetWindowPixelsBuffer", CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetWindowPixelsBuffer(IntPtr windowPtr, IntPtr pixelsBufferPtr, int width, int height, int rowBytes);

        [DllImport(LIB_NAME, EntryPoint = "WA_InvalidateWindow", CallingConvention = CallingConvention.Cdecl)]
        public static extern void InvalidateWindow(IntPtr windowPtr);

        [DllImport(LIB_NAME, EntryPoint = "WA_GetWindowDpi", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint GetWindowDpi(IntPtr windowPtr);

        [DllImport(LIB_NAME, EntryPoint = "WA_IsWindowPresentable", CallingConvention = CallingConvention.Cdecl)]
        public static extern int IsWindowPresentable(IntPtr windowPtr);

        [DllImport(LIB_NAME, EntryPoint = "WA_SetWindowCursor", CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetWindowCursor(IntPtr windowPtr, int kind);

        [DllImport(LIB_NAME, EntryPoint = "WA_RegisterPaintCallBack", CallingConvention = CallingConvention.Cdecl)]
        public static extern void RegisterPaintCallBack(IntPtr windowPtr, [MarshalAs(UnmanagedType.FunctionPtr)] OnPaintCallBack callback);

        [DllImport(LIB_NAME, EntryPoint = "WA_RegisterPointerCallBack", CallingConvention = CallingConvention.Cdecl)]
        public static extern void RegisterPointerCallBack(IntPtr windowPtr, [MarshalAs(UnmanagedType.FunctionPtr)] OnPointerCallBack callback);

        [DllImport(LIB_NAME, EntryPoint = "WA_RegisterKeyCallBack", CallingConvention = CallingConvention.Cdecl)]
        public static extern void RegisterKeyCallBack(IntPtr windowPtr, [MarshalAs(UnmanagedType.FunctionPtr)] OnKeyCallBack callback);

        [DllImport(LIB_NAME, EntryPoint = "WA_RegisterWheelCallBack", CallingConvention = CallingConvention.Cdecl)]
        public static extern void RegisterWheelCallBack(IntPtr windowPtr, [MarshalAs(UnmanagedType.FunctionPtr)] OnWheelCallBack callback);

        [DllImport(LIB_NAME, EntryPoint = "WA_GetClipboardText", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetClipboardText();

        [DllImport(LIB_NAME, EntryPoint = "WA_SetClipboardText", CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetClipboardText([MarshalAs(UnmanagedType.LPUTF8Str)] string text);

        [DllImport(LIB_NAME, EntryPoint = "WA_Schedule", CallingConvention = CallingConvention.Cdecl)]
        public static extern long Schedule(int delayMilliseconds, int repeat, [MarshalAs(UnmanagedType.FunctionPtr)] OnTimerCallBack callback);

        [DllImport(LIB_NAME, EntryPoint = "WA_Cancel", CallingConvention = CallingConvention.Cdecl)]
        public static extern void Cancel(long id);

        [DllImport(LIB_NAME, EntryPoint = "WA_PrefersReducedMotion", CallingConvention = CallingConvention.Cdecl)]
        public static extern int PrefersReducedMotion();
    }
}
