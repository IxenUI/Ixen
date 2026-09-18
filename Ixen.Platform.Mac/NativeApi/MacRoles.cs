using Ixen.Core.Accessibility;

namespace Ixen.Platform.Mac.NativeApi
{
    internal static class MacRoles
    {
        internal const string UNKNOWN = "AXUnknown";

        internal const int NOT_A_TOGGLE = -1;

        internal static string ToNative(AccessibleRole role)
        {
            switch (role)
            {
                case AccessibleRole.Group: return "AXGroup";
                case AccessibleRole.Text: return "AXStaticText";
                case AccessibleRole.Heading: return "AXHeading";
                case AccessibleRole.Image: return "AXImage";
                case AccessibleRole.Button: return "AXButton";
                case AccessibleRole.Link: return "AXLink";
                case AccessibleRole.CheckBox: return "AXCheckBox";
                case AccessibleRole.RadioButton: return "AXRadioButton";
                case AccessibleRole.Switch: return "AXCheckBox";
                case AccessibleRole.TextField: return "AXTextField";
                case AccessibleRole.Slider: return "AXSlider";
                case AccessibleRole.ProgressBar: return "AXProgressIndicator";
                case AccessibleRole.List: return "AXList";
                case AccessibleRole.ListItem: return "AXRow";
                case AccessibleRole.Tree: return "AXOutline";
                case AccessibleRole.TreeItem: return "AXRow";
                case AccessibleRole.Table: return "AXTable";
                case AccessibleRole.TableRow: return "AXRow";
                case AccessibleRole.TableCell: return "AXCell";
                case AccessibleRole.ColumnHeader: return "AXButton";
                case AccessibleRole.Tab: return "AXRadioButton";
                case AccessibleRole.TabList: return "AXTabGroup";
                case AccessibleRole.Menu: return "AXMenu";
                case AccessibleRole.MenuItem: return "AXMenuItem";
                case AccessibleRole.ComboBox: return "AXComboBox";
                case AccessibleRole.Dialog: return "AXGroup";
                case AccessibleRole.ScrollBar: return "AXScrollBar";
                case AccessibleRole.Presentation: return UNKNOWN;
                case AccessibleRole.None: return UNKNOWN;
                default: return UNKNOWN;
            }
        }

        internal static int ToggleOf(AccessibleNode node)
        {
            switch (node.Role)
            {
                case AccessibleRole.CheckBox:
                case AccessibleRole.RadioButton:
                case AccessibleRole.Switch:
                    return node.HasState(AccessibleStates.Checked) ? 1 : 0;

                case AccessibleRole.Tab:
                    return node.HasState(AccessibleStates.Selected) ? 1 : 0;

                default:
                    return NOT_A_TOGGLE;
            }
        }

        internal static string HelpOf(AccessibleNode node)
        {
            if (string.IsNullOrEmpty(node.Shortcut))
            {
                return node.Description;
            }

            if (string.IsNullOrEmpty(node.Description))
            {
                return node.Shortcut;
            }

            return node.Description + " (" + node.Shortcut + ")";
        }
    }
}
