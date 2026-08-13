using System;
using System.Runtime.InteropServices;

namespace Ixen.Platform.Windows.NativeApi
{
    internal static class WindowApi
    {
        const string LIB_NAME = "Ixen.Platform.Windows.Native.dll";

        public delegate void OnPaintCallBack(int width, int height);
        public delegate void OnPointerCallBack(int kind, int x, int y, int button);
        public delegate void OnKeyCallBack(int kind, int keyCode, int modifiers);
        public delegate void OnWheelCallBack(int x, int y, int deltaX, int deltaY);

        [DllImport(LIB_NAME, EntryPoint = "WA_CreateWindow", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CreateWindow([MarshalAs(UnmanagedType.LPWStr)] string title, int width, int height);

        [DllImport(LIB_NAME, EntryPoint = "WA_ShowWindow", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ShowWindow(IntPtr windowPtr);

        [DllImport(LIB_NAME, EntryPoint = "WA_GetWindowTitle", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.LPWStr)]
        public static extern string GetWindowTitle(IntPtr windowPtr);

        [DllImport(LIB_NAME, EntryPoint = "WA_SetWindowTitle", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        public static extern void SetWindowTitle(IntPtr windowPtr, [MarshalAs(UnmanagedType.LPWStr)] string title);

        [DllImport(LIB_NAME, EntryPoint = "WA_SetWindowPixelsBuffer", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        public static extern void SetWindowPixelsBuffer(IntPtr windowPtr, IntPtr pixelsBufferPtr);

        [DllImport(LIB_NAME, EntryPoint = "WA_RegisterPaintCallBack", CallingConvention = CallingConvention.Cdecl)]
        public static extern void RegisterPaintCallBack(IntPtr windowPtr, [MarshalAs(UnmanagedType.FunctionPtr)] OnPaintCallBack callback);

        [DllImport(LIB_NAME, EntryPoint = "WA_RegisterPointerCallBack", CallingConvention = CallingConvention.Cdecl)]
        public static extern void RegisterPointerCallBack(IntPtr windowPtr, [MarshalAs(UnmanagedType.FunctionPtr)] OnPointerCallBack callback);

        [DllImport(LIB_NAME, EntryPoint = "WA_RegisterKeyCallBack", CallingConvention = CallingConvention.Cdecl)]
        public static extern void RegisterKeyCallBack(IntPtr windowPtr, [MarshalAs(UnmanagedType.FunctionPtr)] OnKeyCallBack callback);

        [DllImport(LIB_NAME, EntryPoint = "WA_RegisterWheelCallBack", CallingConvention = CallingConvention.Cdecl)]
        public static extern void RegisterWheelCallBack(IntPtr windowPtr, [MarshalAs(UnmanagedType.FunctionPtr)] OnWheelCallBack callback);

        [DllImport(LIB_NAME, EntryPoint = "WA_InvalidateWindow", CallingConvention = CallingConvention.Cdecl)]
        public static extern void InvalidateWindow(IntPtr windowPtr);

        [DllImport(LIB_NAME, EntryPoint = "WA_GetWindowDpi", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint GetWindowDpi(IntPtr windowPtr);
    }
}
