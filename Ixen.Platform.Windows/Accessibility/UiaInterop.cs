using System;
using System.Runtime.InteropServices;

namespace Ixen.Platform.Windows.Accessibility
{
    internal enum ProviderOptions
    {
        ClientSideProvider = 1,
        ServerSideProvider = 2,
        NonClientAreaProvider = 4,
        OverrideProvider = 8,
        ProviderOwnsSetFocus = 16,
        UseComThreading = 32
    }

    internal enum NavigateDirection
    {
        Parent = 0,
        NextSibling = 1,
        PreviousSibling = 2,
        FirstChild = 3,
        LastChild = 4
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct UiaRect
    {
        public double Left;
        public double Top;
        public double Width;
        public double Height;
    }

    [ComVisible(true)]
    [ComImport]
    [Guid("d6dd68d1-86fd-4332-8666-9abedea2d24c")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IRawElementProviderSimple
    {
        ProviderOptions ProviderOptions { get; }

        [return: MarshalAs(UnmanagedType.IUnknown)]
        object GetPatternProvider(int patternId);

        [return: MarshalAs(UnmanagedType.Struct)]
        object GetPropertyValue(int propertyId);

        IRawElementProviderSimple HostRawElementProvider { get; }
    }

    [ComVisible(true)]
    [ComImport]
    [Guid("f7063da8-8359-439c-9297-bbc5299a7d87")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IRawElementProviderFragment : IRawElementProviderSimple
    {
        IRawElementProviderFragment Navigate(NavigateDirection direction);

        [return: MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_I4)]
        int[] GetRuntimeId();

        UiaRect BoundingRectangle { get; }

        [return: MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_UNKNOWN)]
        IRawElementProviderSimple[] GetEmbeddedFragmentRoots();

        void SetFocus();

        IRawElementProviderFragmentRoot FragmentRoot { get; }
    }

    [ComVisible(true)]
    [ComImport]
    [Guid("620ce2a5-ab8f-40a9-86cb-de3c75599b58")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IRawElementProviderFragmentRoot : IRawElementProviderFragment
    {
        IRawElementProviderFragment ElementProviderFromPoint(double x, double y);

        IRawElementProviderFragment GetFocus();
    }

    [ComVisible(true)]
    [ComImport]
    [Guid("54fcb24b-e18e-47a2-b4d3-eccbe77599a2")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IInvokeProvider
    {
        void Invoke();
    }

    [ComVisible(true)]
    [ComImport]
    [Guid("c7935180-6fb3-4201-b174-7df73adbf64a")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IValueProvider
    {
        void SetValue([MarshalAs(UnmanagedType.LPWStr)] string value);

        [return: MarshalAs(UnmanagedType.BStr)]
        string GetValue();

        bool IsReadOnly { get; }
    }

    [ComVisible(true)]
    [ComImport]
    [Guid("2360c714-4bf1-4b26-ba65-9b21316127eb")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IScrollItemProvider
    {
        void ScrollIntoView();
    }

    internal static class UiaPattern
    {
        internal const int INVOKE = 10000;
        internal const int VALUE = 10002;
        internal const int SCROLL_ITEM = 10017;
    }

    internal static class UiaProperty
    {
        internal const int CONTROL_TYPE = 30003;
        internal const int NAME = 30005;
        internal const int HAS_KEYBOARD_FOCUS = 30008;
        internal const int IS_KEYBOARD_FOCUSABLE = 30009;
        internal const int IS_ENABLED = 30010;
        internal const int AUTOMATION_ID = 30011;
        internal const int CLASS_NAME = 30012;
        internal const int HELP_TEXT = 30013;
        internal const int IS_PASSWORD = 30019;
        internal const int IS_OFFSCREEN = 30022;
        internal const int IS_CONTROL_ELEMENT = 30016;
        internal const int IS_CONTENT_ELEMENT = 30017;
        internal const int VALUE_VALUE = 30045;
        internal const int VALUE_IS_READ_ONLY = 30046;
        internal const int LIVE_SETTING = 30135;
    }

    internal static class UiaControlType
    {
        internal const int BUTTON = 50000;
        internal const int CHECK_BOX = 50002;
        internal const int COMBO_BOX = 50003;
        internal const int EDIT = 50004;
        internal const int HYPERLINK = 50005;
        internal const int IMAGE = 50006;
        internal const int LIST_ITEM = 50007;
        internal const int LIST = 50008;
        internal const int MENU = 50009;
        internal const int MENU_ITEM = 50011;
        internal const int PROGRESS_BAR = 50012;
        internal const int RADIO_BUTTON = 50013;
        internal const int SCROLL_BAR = 50014;
        internal const int SLIDER = 50015;
        internal const int TAB = 50018;
        internal const int TAB_ITEM = 50019;
        internal const int TEXT = 50020;
        internal const int TREE = 50023;
        internal const int TREE_ITEM = 50024;
        internal const int GROUP = 50026;
        internal const int DATA_GRID = 50028;
        internal const int WINDOW = 50032;
        internal const int PANE = 50033;
        internal const int HEADER_ITEM = 50035;
        internal const int TABLE = 50036;
        internal const int CUSTOM = 50025;
    }

    internal enum StructureChangeType
    {
        ChildAdded = 0,
        ChildRemoved = 1,
        ChildrenInvalidated = 2,
        ChildrenBulkAdded = 3,
        ChildrenBulkRemoved = 4,
        ChildrenReordered = 5
    }

    internal static class UiaEvent
    {
        internal const int FOCUS_CHANGED = 20005;
        internal const int LIVE_REGION_CHANGED = 20024;
    }

    internal static class UiaNative
    {
        private const string LIB_NAME = "uiautomationcore.dll";

        internal const int UIA_APPEND_RUNTIME_ID = 3;

        [DllImport(LIB_NAME, ExactSpelling = true)]
        internal static extern IntPtr UiaReturnRawElementProvider(IntPtr hwnd, IntPtr wParam,
            IntPtr lParam, [MarshalAs(UnmanagedType.Interface)] IRawElementProviderSimple provider);

        [DllImport(LIB_NAME, ExactSpelling = true)]
        internal static extern int UiaHostProviderFromHwnd(IntPtr hwnd,
            [MarshalAs(UnmanagedType.Interface)] out IRawElementProviderSimple provider);

        [DllImport(LIB_NAME, ExactSpelling = true)]
        internal static extern bool UiaClientsAreListening();

        [DllImport(LIB_NAME, ExactSpelling = true)]
        internal static extern int UiaRaiseAutomationEvent(
            [MarshalAs(UnmanagedType.Interface)] IRawElementProviderSimple provider, int id);

        [DllImport(LIB_NAME, ExactSpelling = true)]
        internal static extern int UiaRaiseAutomationPropertyChangedEvent(
            [MarshalAs(UnmanagedType.Interface)] IRawElementProviderSimple provider, int id,
            [MarshalAs(UnmanagedType.Struct)] object oldValue,
            [MarshalAs(UnmanagedType.Struct)] object newValue);

        [DllImport(LIB_NAME, ExactSpelling = true)]
        internal static extern int UiaRaiseStructureChangedEvent(
            [MarshalAs(UnmanagedType.Interface)] IRawElementProviderSimple provider,
            StructureChangeType change, int[] runtimeId, int runtimeIdLength);

        [DllImport("user32.dll")]
        internal static extern bool ClientToScreen(IntPtr hwnd, ref Point point);

        [StructLayout(LayoutKind.Sequential)]
        internal struct Point
        {
            public int X;
            public int Y;
        }
    }
}
