using Ixen.Core.Accessibility;

namespace Ixen.Platform.Linux.NativeApi
{
    internal static class LinuxRoles
    {
        internal const int FRAME = 23;

        private const int UNKNOWN = 67;
        private const int PASSWORD_TEXT = 40;

        private const int STATE_ACTIVE = 1;
        private const int STATE_CHECKED = 4;
        private const int STATE_EDITABLE = 7;
        private const int STATE_ENABLED = 8;
        private const int STATE_EXPANDABLE = 9;
        private const int STATE_EXPANDED = 10;
        private const int STATE_FOCUSABLE = 11;
        private const int STATE_FOCUSED = 12;
        private const int STATE_MULTI_LINE = 17;
        private const int STATE_SELECTABLE = 22;
        private const int STATE_SELECTED = 23;
        private const int STATE_SENSITIVE = 24;
        private const int STATE_SHOWING = 25;
        private const int STATE_SINGLE_LINE = 26;
        private const int STATE_VISIBLE = 30;
        private const int STATE_INVALID_ENTRY = 36;

        internal static int ToNative(AccessibleRole role)
        {
            switch (role)
            {
                case AccessibleRole.Group: return 39;
                case AccessibleRole.Text: return 29;
                case AccessibleRole.Heading: return 83;
                case AccessibleRole.Image: return 27;
                case AccessibleRole.Button: return 43;
                case AccessibleRole.Link: return 88;
                case AccessibleRole.CheckBox: return 7;
                case AccessibleRole.RadioButton: return 44;
                case AccessibleRole.Switch: return 62;
                case AccessibleRole.TextField: return 79;
                case AccessibleRole.Slider: return 51;
                case AccessibleRole.ProgressBar: return 42;
                case AccessibleRole.List: return 31;
                case AccessibleRole.ListItem: return 32;
                case AccessibleRole.Tree: return 65;
                case AccessibleRole.TreeItem: return 91;
                case AccessibleRole.Table: return 55;
                case AccessibleRole.TableRow: return 90;
                case AccessibleRole.TableCell: return 56;
                case AccessibleRole.ColumnHeader: return 10;
                case AccessibleRole.Tab: return 37;
                case AccessibleRole.TabList: return 38;
                case AccessibleRole.Menu: return 33;
                case AccessibleRole.MenuItem: return 35;
                case AccessibleRole.ComboBox: return 11;
                case AccessibleRole.Dialog: return 16;
                case AccessibleRole.ScrollBar: return 48;
                case AccessibleRole.Presentation: return 20;
                case AccessibleRole.None: return UNKNOWN;
                default: return UNKNOWN;
            }
        }

        internal static int RoleOf(AccessibleNode node, bool isRoot)
        {
            if (isRoot)
            {
                return FRAME;
            }

            if ((node.States & AccessibleStates.Protected) != 0)
            {
                return PASSWORD_TEXT;
            }

            return ToNative(node.Role);
        }

        internal static long StatesOf(AccessibleNode node)
        {
            AccessibleStates states = node.States;
            long mask = Bit(STATE_VISIBLE);

            if ((states & AccessibleStates.Offscreen) == 0)
            {
                mask |= Bit(STATE_SHOWING);
            }

            if ((states & AccessibleStates.Disabled) == 0)
            {
                mask |= Bit(STATE_ENABLED) | Bit(STATE_SENSITIVE);
            }

            if ((states & AccessibleStates.Focusable) != 0)
            {
                mask |= Bit(STATE_FOCUSABLE);
            }

            if ((states & AccessibleStates.Focused) != 0)
            {
                mask |= Bit(STATE_FOCUSED) | Bit(STATE_ACTIVE);
            }

            if ((states & AccessibleStates.Checked) != 0)
            {
                mask |= Bit(STATE_CHECKED);
            }

            if ((states & AccessibleStates.Selected) != 0)
            {
                mask |= Bit(STATE_SELECTED) | Bit(STATE_SELECTABLE);
            }

            if ((states & AccessibleStates.Expanded) != 0)
            {
                mask |= Bit(STATE_EXPANDED) | Bit(STATE_EXPANDABLE);
            }

            if ((states & AccessibleStates.Invalid) != 0)
            {
                mask |= Bit(STATE_INVALID_ENTRY);
            }

            if (node.Supports(AccessibleActions.SetValue))
            {
                mask |= Bit(STATE_EDITABLE);

                mask |= (states & AccessibleStates.Multiline) != 0
                    ? Bit(STATE_MULTI_LINE)
                    : Bit(STATE_SINGLE_LINE);
            }

            return mask;
        }

        private static long Bit(int state) => 1L << state;
    }
}
