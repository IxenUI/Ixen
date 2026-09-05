using Ixen.Core.Accessibility;
using System;
using System.Runtime.InteropServices;

namespace Ixen.Platform.Windows.Accessibility
{
    [ComVisible(true)]
    internal class UiaProvider : IRawElementProviderFragment, IInvokeProvider, IValueProvider,
        IScrollItemProvider
    {
        private readonly UiaBridge _bridge;
        private readonly int _id;

        internal UiaProvider(UiaBridge bridge, int id)
        {
            _bridge = bridge;
            _id = id;
        }

        internal int Id => _id;

        private AccessibleNode Node => _bridge.NodeOf(_id);

        public ProviderOptions ProviderOptions => ProviderOptions.ServerSideProvider;

        public virtual IRawElementProviderSimple HostRawElementProvider => null;

        public object GetPatternProvider(int patternId)
        {
            AccessibleNode node = Node;

            if (node == null)
            {
                return null;
            }

            switch (patternId)
            {
                case UiaPattern.INVOKE:
                    return node.Supports(AccessibleActions.Invoke) ? this : null;

                case UiaPattern.VALUE:
                    return node.Supports(AccessibleActions.SetValue) || node.Value != null
                        ? this
                        : null;

                case UiaPattern.SCROLL_ITEM:
                    return node.Supports(AccessibleActions.ScrollIntoView) ? this : null;

                default:
                    return null;
            }
        }

        public virtual object GetPropertyValue(int propertyId)
        {
            AccessibleNode node = Node;

            if (node == null)
            {
                return null;
            }

            switch (propertyId)
            {
                case UiaProperty.CONTROL_TYPE:
                    return ControlTypeOf(node.Role);

                case UiaProperty.NAME:
                    return node.Name;

                case UiaProperty.HELP_TEXT:
                    return node.Description;

                case UiaProperty.AUTOMATION_ID:
                    return node.Element?.Name;

                case UiaProperty.CLASS_NAME:
                    return node.Element?.TypeName;

                case UiaProperty.IS_KEYBOARD_FOCUSABLE:
                    return node.HasState(AccessibleStates.Focusable);

                case UiaProperty.HAS_KEYBOARD_FOCUS:
                    return node.HasState(AccessibleStates.Focused);

                case UiaProperty.IS_ENABLED:
                    return !node.HasState(AccessibleStates.Disabled);

                case UiaProperty.IS_OFFSCREEN:
                    return node.HasState(AccessibleStates.Offscreen);

                case UiaProperty.IS_PASSWORD:
                    return node.HasState(AccessibleStates.Protected);

                case UiaProperty.IS_CONTROL_ELEMENT:
                case UiaProperty.IS_CONTENT_ELEMENT:
                    return true;

                case UiaProperty.VALUE_VALUE:
                    return node.Value;

                case UiaProperty.VALUE_IS_READ_ONLY:
                    return !node.Supports(AccessibleActions.SetValue);

                case UiaProperty.ACCELERATOR_KEY:
                    return node.Shortcut;

                case UiaProperty.LIVE_SETTING:
                    return (int)node.Live;

                default:
                    return null;
            }
        }

        internal static int ControlTypeOf(AccessibleRole role)
        {
            switch (role)
            {
                case AccessibleRole.Button: return UiaControlType.BUTTON;
                case AccessibleRole.Link: return UiaControlType.HYPERLINK;
                case AccessibleRole.CheckBox: return UiaControlType.CHECK_BOX;
                case AccessibleRole.RadioButton: return UiaControlType.RADIO_BUTTON;
                case AccessibleRole.Switch: return UiaControlType.CHECK_BOX;
                case AccessibleRole.TextField: return UiaControlType.EDIT;
                case AccessibleRole.Slider: return UiaControlType.SLIDER;
                case AccessibleRole.ProgressBar: return UiaControlType.PROGRESS_BAR;
                case AccessibleRole.List: return UiaControlType.LIST;
                case AccessibleRole.ListItem: return UiaControlType.LIST_ITEM;
                case AccessibleRole.Tree: return UiaControlType.TREE;
                case AccessibleRole.TreeItem: return UiaControlType.TREE_ITEM;
                case AccessibleRole.Table: return UiaControlType.TABLE;
                case AccessibleRole.TableRow: return UiaControlType.CUSTOM;
                case AccessibleRole.TableCell: return UiaControlType.CUSTOM;
                case AccessibleRole.ColumnHeader: return UiaControlType.HEADER_ITEM;
                case AccessibleRole.Tab: return UiaControlType.TAB_ITEM;
                case AccessibleRole.TabList: return UiaControlType.TAB;
                case AccessibleRole.Menu: return UiaControlType.MENU;
                case AccessibleRole.MenuItem: return UiaControlType.MENU_ITEM;
                case AccessibleRole.ComboBox: return UiaControlType.COMBO_BOX;
                case AccessibleRole.Dialog: return UiaControlType.WINDOW;
                case AccessibleRole.ScrollBar: return UiaControlType.SCROLL_BAR;
                case AccessibleRole.Image: return UiaControlType.IMAGE;
                case AccessibleRole.Text: return UiaControlType.TEXT;
                case AccessibleRole.Heading: return UiaControlType.TEXT;
                default: return UiaControlType.GROUP;
            }
        }

        public IRawElementProviderFragment Navigate(NavigateDirection direction)
            => _bridge.Navigate(_id, direction);

        public int[] GetRuntimeId()
            => new[] { UiaNative.UIA_APPEND_RUNTIME_ID, _id };

        public UiaRect BoundingRectangle => _bridge.RectangleOf(_id);

        public IRawElementProviderSimple[] GetEmbeddedFragmentRoots() => null;

        public void SetFocus() => _bridge.Post(_id, AccessibleActions.Focus, null);

        public IRawElementProviderFragmentRoot FragmentRoot => _bridge.Root;

        public void Invoke() => _bridge.Post(_id, AccessibleActions.Invoke, null);

        public void SetValue(string value) => _bridge.Post(_id, AccessibleActions.SetValue, value);

        public string GetValue() => Node?.Value ?? string.Empty;

        public bool IsReadOnly
        {
            get
            {
                AccessibleNode node = Node;

                return node == null || !node.Supports(AccessibleActions.SetValue);
            }
        }

        public void ScrollIntoView() => _bridge.Post(_id, AccessibleActions.ScrollIntoView, null);
    }

    [ComVisible(true)]
    internal sealed class UiaRootProvider : UiaProvider, IRawElementProviderFragmentRoot
    {
        private readonly UiaBridge _bridge;

        internal UiaRootProvider(UiaBridge bridge, int id)
            : base(bridge, id)
        {
            _bridge = bridge;
        }

        public override IRawElementProviderSimple HostRawElementProvider
            => _bridge.HostProvider;

        public override object GetPropertyValue(int propertyId)
        {
            if (propertyId == UiaProperty.CONTROL_TYPE || propertyId == UiaProperty.NAME)
            {
                return null;
            }

            return base.GetPropertyValue(propertyId);
        }

        public IRawElementProviderFragment ElementProviderFromPoint(double x, double y)
            => _bridge.FromPoint(x, y);

        public IRawElementProviderFragment GetFocus() => _bridge.Focused();
    }
}
