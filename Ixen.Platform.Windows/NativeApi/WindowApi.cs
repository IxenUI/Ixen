using System;
using System.Runtime.InteropServices;

namespace Ixen.Platform.Windows.NativeApi
{
    internal static class WindowApi
    {
        const string LIB_NAME = "Ixen.Platform.Windows.Native.dll";

        public delegate void OnPaintCallBack(int width, int height);

        [DllImport(LIB_NAME, EntryPoint = "WA_CreateWindow", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CreateWindow();

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
    }
}
