using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

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

    [GeneratedComInterface]
    [Guid("d6dd68d1-86fd-4332-8666-9abedea2d24c")]
    internal unsafe partial interface IRawElementProviderSimple
    {
        ProviderOptions get_ProviderOptions();

        nint GetPatternProvider(int patternId);

        [PreserveSig]
        int GetPropertyValue(int propertyId, ComVariant* value);

        nint get_HostRawElementProvider();
    }

    [GeneratedComInterface]
    [Guid("f7063da8-8359-439c-9297-bbc5299a7d87")]
    internal partial interface IRawElementProviderFragment
    {
        IRawElementProviderFragment Navigate(NavigateDirection direction);

        nint GetRuntimeId();

        UiaRect get_BoundingRectangle();

        nint GetEmbeddedFragmentRoots();

        void SetFocus();

        IRawElementProviderFragmentRoot get_FragmentRoot();
    }

    [GeneratedComInterface]
    [Guid("620ce2a5-ab8f-40a9-86cb-de3c75599b58")]
    internal partial interface IRawElementProviderFragmentRoot
    {
        IRawElementProviderFragment ElementProviderFromPoint(double x, double y);

        IRawElementProviderFragment GetFocus();
    }

    [GeneratedComInterface]
    [Guid("54fcb24b-e18e-47a2-b4d3-eccbe77599a2")]
    internal partial interface IInvokeProvider
    {
        void Invoke();
    }

    [GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16)]
    [Guid("c7935180-6fb3-4201-b174-7df73adbf64a")]
    internal partial interface IValueProvider
    {
        void SetValue(string value);

        [return: MarshalUsing(typeof(BStrStringMarshaller))]
        string get_Value();

        [return: MarshalAs(UnmanagedType.Bool)]
        bool get_IsReadOnly();
    }

    [GeneratedComInterface]
    [Guid("2360c714-4bf1-4b26-ba65-9b21316127eb")]
    internal partial interface IScrollItemProvider
    {
        void ScrollIntoView();
    }

    internal enum ToggleState
    {
        Off = 0,
        On = 1,
        Indeterminate = 2
    }

    internal enum ExpandCollapseState
    {
        Collapsed = 0,
        Expanded = 1,
        PartiallyExpanded = 2,
        LeafNode = 3
    }

    [GeneratedComInterface]
    [Guid("56d00bd0-c4f4-433c-a836-1a52a57e0892")]
    internal partial interface IToggleProvider
    {
        void Toggle();

        ToggleState get_ToggleState();
    }

    [GeneratedComInterface]
    [Guid("d847d3a5-cab0-4a98-8c32-ecb45c59ad24")]
    internal partial interface IExpandCollapseProvider
    {
        void Expand();

        void Collapse();

        ExpandCollapseState get_ExpandCollapseState();
    }

    [GeneratedComInterface]
    [Guid("2acad808-b2d4-452d-a407-91ff1ad167b2")]
    internal partial interface ISelectionItemProvider
    {
        void Select();

        void AddToSelection();

        void RemoveFromSelection();

        [return: MarshalAs(UnmanagedType.Bool)]
        bool get_IsSelected();

        IRawElementProviderSimple get_SelectionContainer();
    }

    [GeneratedComInterface]
    [Guid("36dc7aef-33e6-4691-afe1-2be7274b3d33")]
    internal partial interface IRangeValueProvider
    {
        void SetValue(double value);

        double get_Value();

        [return: MarshalAs(UnmanagedType.Bool)]
        bool get_IsReadOnly();

        double get_Maximum();

        double get_Minimum();

        double get_LargeChange();

        double get_SmallChange();
    }

    [GeneratedComInterface]
    [Guid("fb8b03af-3bdf-48d4-bd36-1a65793be168")]
    internal partial interface ISelectionProvider
    {
        nint GetSelection();

        [return: MarshalAs(UnmanagedType.Bool)]
        bool get_CanSelectMultiple();

        [return: MarshalAs(UnmanagedType.Bool)]
        bool get_IsSelectionRequired();
    }

    internal static class UiaPattern
    {
        internal const int INVOKE = 10000;
        internal const int SELECTION = 10001;
        internal const int VALUE = 10002;
        internal const int RANGE_VALUE = 10003;
        internal const int EXPAND_COLLAPSE = 10005;
        internal const int SELECTION_ITEM = 10010;
        internal const int TOGGLE = 10015;
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
        internal const int ACCELERATOR_KEY = 30006;
        internal const int IS_PASSWORD = 30019;
        internal const int IS_OFFSCREEN = 30022;
        internal const int IS_CONTROL_ELEMENT = 30016;
        internal const int IS_CONTENT_ELEMENT = 30017;
        internal const int VALUE_VALUE = 30045;
        internal const int VALUE_IS_READ_ONLY = 30046;
        internal const int RANGE_VALUE_VALUE = 30047;
        internal const int LIVE_SETTING = 30135;
        internal const int SELECTION_CAN_SELECT_MULTIPLE = 30060;
        internal const int SELECTION_IS_SELECTION_REQUIRED = 30061;
        internal const int EXPAND_COLLAPSE_STATE = 30070;
        internal const int SELECTION_ITEM_IS_SELECTED = 30079;
        internal const int TOGGLE_STATE = 30086;
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
        internal const int ELEMENT_SELECTED = 20012;
        internal const int LIVE_REGION_CHANGED = 20024;
    }

    internal static partial class UiaNative
    {
        private const string LIB_NAME = "uiautomationcore.dll";
        private const string OLE_NAME = "oleaut32.dll";

        private const ushort VT_I4 = 3;
        private const ushort VT_UNKNOWN = 13;

        internal const int UIA_APPEND_RUNTIME_ID = 3;

        private static readonly StrategyBasedComWrappers _wrappers = new StrategyBasedComWrappers();

        [LibraryImport(LIB_NAME)]
        internal static partial nint UiaReturnRawElementProvider(nint hwnd, nint wParam,
            nint lParam, nint provider);

        [LibraryImport(LIB_NAME)]
        internal static partial int UiaHostProviderFromHwnd(nint hwnd, out nint provider);

        [LibraryImport(LIB_NAME)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool UiaClientsAreListening();

        [LibraryImport(LIB_NAME)]
        internal static partial int UiaRaiseAutomationEvent(nint provider, int id);

        [LibraryImport(LIB_NAME)]
        internal static partial int UiaRaiseAutomationPropertyChangedEvent(nint provider, int id,
            RawVariant oldValue, RawVariant newValue);

        [LibraryImport(LIB_NAME)]
        internal static partial int UiaRaiseStructureChangedEvent(nint provider,
            StructureChangeType change, int[] runtimeId, int runtimeIdLength);

        [LibraryImport(OLE_NAME)]
        private static partial nint SafeArrayCreateVector(ushort type, int lowerBound, uint count);

        [LibraryImport(OLE_NAME)]
        private static partial int SafeArrayPutElement(nint array, ref int index, ref int value);

        [LibraryImport(OLE_NAME, EntryPoint = "SafeArrayPutElement")]
        private static partial int SafeArrayPutPointer(nint array, ref int index, nint value);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool ClientToScreen(nint hwnd, ref Point point);

        [StructLayout(LayoutKind.Sequential)]
        internal struct RawVariant
        {
            public long Head;
            public long Body;
            public long Tail;
        }

        internal static bool CanPassVariants => Unsafe.SizeOf<ComVariant>() == Unsafe.SizeOf<RawVariant>();

        internal static RawVariant Raw(ComVariant variant) => Unsafe.As<ComVariant, RawVariant>(ref variant);

        [StructLayout(LayoutKind.Sequential)]
        internal struct Point
        {
            public int X;
            public int Y;
        }

        private static readonly Guid _simpleProvider = new Guid("d6dd68d1-86fd-4332-8666-9abedea2d24c");

        internal static nint ProviderPointer(IRawElementProviderSimple provider)
        {
            nint unknown = _wrappers.GetOrCreateComInterfaceForObject(provider, CreateComInterfaceFlags.None);

            if (unknown == 0)
            {
                return 0;
            }

            int result = Marshal.QueryInterface(unknown, in _simpleProvider, out nint simple);

            Marshal.Release(unknown);

            return result == 0 ? simple : 0;
        }

        internal static void Release(nint pointer)
        {
            if (pointer != 0)
            {
                Marshal.Release(pointer);
            }
        }

        internal static nint UnknownOf(object instance)
            => instance == null
                ? 0
                : _wrappers.GetOrCreateComInterfaceForObject(instance, CreateComInterfaceFlags.None);

        internal static nint IntArray(int[] values)
        {
            nint array = SafeArrayCreateVector(VT_I4, 0, (uint)values.Length);

            if (array == 0)
            {
                return 0;
            }

            for (int index = 0; index < values.Length; index++)
            {
                int at = index;
                int value = values[index];

                SafeArrayPutElement(array, ref at, ref value);
            }

            return array;
        }

        internal static nint ProviderArray(IRawElementProviderSimple[] providers)
        {
            nint array = SafeArrayCreateVector(VT_UNKNOWN, 0, (uint)providers.Length);

            if (array == 0)
            {
                return 0;
            }

            for (int index = 0; index < providers.Length; index++)
            {
                int at = index;
                nint pointer = ProviderPointer(providers[index]);

                SafeArrayPutPointer(array, ref at, pointer);
                Release(pointer);
            }

            return array;
        }

        internal static ComVariant Variant(object value)
        {
            switch (value)
            {
                case null:
                    return default;

                case string text:
                    return ComVariant.Create(text);

                case bool flag:
                    return ComVariant.Create(flag);

                case int number:
                    return ComVariant.Create(number);

                case float real:
                    return ComVariant.Create((double)real);

                case double precise:
                    return ComVariant.Create(precise);

                default:
                    return default;
            }
        }
    }
}
