using Ixen.Core.Input;
using System;
using System.Runtime.InteropServices;

namespace Ixen.Platform.Mac
{
    internal static class MacModifiers
    {
        private const string OBJC = "/usr/lib/libobjc.A.dylib";

        private const ulong SHIFT = 1UL << 17;
        private const ulong CONTROL = 1UL << 18;
        private const ulong OPTION = 1UL << 19;
        private const ulong COMMAND = 1UL << 20;

        [DllImport(OBJC)]
        private static extern IntPtr objc_getClass(string name);

        [DllImport(OBJC)]
        private static extern IntPtr sel_registerName(string name);

        [DllImport(OBJC, EntryPoint = "objc_msgSend")]
        private static extern ulong SendUInt64(IntPtr receiver, IntPtr selector);

        private static IntPtr _event;
        private static IntPtr _modifierFlags;

        internal static KeyModifiers Held()
        {
            if (_event == IntPtr.Zero)
            {
                _event = objc_getClass("NSEvent");
                _modifierFlags = sel_registerName("modifierFlags");
            }

            if (_event == IntPtr.Zero || _modifierFlags == IntPtr.Zero)
            {
                return KeyModifiers.None;
            }

            ulong flags = SendUInt64(_event, _modifierFlags);
            KeyModifiers modifiers = KeyModifiers.None;

            if ((flags & SHIFT) != 0)
            {
                modifiers |= KeyModifiers.Shift;
            }

            if ((flags & CONTROL) != 0)
            {
                modifiers |= KeyModifiers.Control;
            }

            if ((flags & OPTION) != 0)
            {
                modifiers |= KeyModifiers.Alt;
            }

            if ((flags & COMMAND) != 0)
            {
                modifiers |= KeyModifiers.Meta;
            }

            return modifiers;
        }
    }
}
