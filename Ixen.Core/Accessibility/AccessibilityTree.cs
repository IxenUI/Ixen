using Ixen.Core.Input;
using Ixen.Core.Visual;
using System.Collections.Generic;
using System.Text;

namespace Ixen.Core.Accessibility
{
    internal static class AccessibilityTree
    {
        internal static AccessibleNode Build(VisualElement root, VisualElement focused,
            KeyModifiers accelerator = KeyShortcut.DEFAULT_ACCELERATOR)
        {
            if (root == null)
            {
                return null;
            }

            Dictionary<VisualElement, VisualElement> labels = null;

            FindLabels(root, ref labels);

            return NodeFor(root, focused, LiveRegionKind.None, accelerator, labels);
        }

        private static void FindLabels(VisualElement element, ref Dictionary<VisualElement, VisualElement> labels)
        {
            foreach (VisualElement child in element.Children)
            {
                if (child.IsHidden)
                {
                    continue;
                }

                VisualElement target = child.LabelFor == null ? null : child.LabelTarget;

                if (target != null && string.IsNullOrEmpty(target.Label))
                {
                    labels = labels ?? new Dictionary<VisualElement, VisualElement>();

                    if (!labels.ContainsKey(target))
                    {
                        labels.Add(target, child);
                    }
                }

                FindLabels(child, ref labels);
            }
        }

        private static bool IsLabel(VisualElement element, Dictionary<VisualElement, VisualElement> labels)
        {
            if (labels == null || element.LabelFor == null)
            {
                return false;
            }

            foreach (VisualElement label in labels.Values)
            {
                if (label == element)
                {
                    return true;
                }
            }

            return false;
        }

        private static string LabelTextOf(VisualElement element, Dictionary<VisualElement, VisualElement> labels)
        {
            if (labels == null || !labels.TryGetValue(element, out VisualElement label))
            {
                return null;
            }

            if (!string.IsNullOrEmpty(label.Text))
            {
                return label.Text;
            }

            var builder = new StringBuilder();

            AppendDescendantText(label, builder);

            return builder.Length == 0 ? null : builder.ToString();
        }

        private static AccessibleNode NodeFor(VisualElement element, VisualElement focused,
            LiveRegionKind inherited, KeyModifiers accelerator, Dictionary<VisualElement, VisualElement> labels)
        {
            AccessibleRole role = RoleOf(element);
            LiveRegionKind live = element.LiveRegion == LiveRegionKind.None
                ? inherited
                : element.LiveRegion;

            var node = new AccessibleNode
            {
                Element = element,
                Role = role == AccessibleRole.None ? AccessibleRole.Group : role,
                Name = NameOf(element, labels),
                Description = DescriptionOf(element),
                Value = ValueOf(element),
                Shortcut = KeyShortcut.Describe(element.Shortcut, accelerator),
                States = StatesOf(element, focused),
                Live = live,
                Actions = ActionsOf(element, role),
                X = element.X,
                Y = element.Y,
                Width = element.ActualWidth,
                Height = element.ActualHeight
            };

            if (element is IAccessibleRange range && range.HasRange)
            {
                node.HasRange = true;
                node.RangeMinimum = range.Minimum;
                node.RangeMaximum = range.Maximum;
                node.RangeValue = range.Value;
                node.RangeStep = range.Step;
            }

            if (!TakesNameFromContent(node.Role))
            {
                Collect(element, focused, node.ChildList, live, accelerator, labels);
            }
            else
            {
                CollectLayers(element, focused, node.ChildList, live, accelerator, labels);
            }

            return node;
        }

        private static void Collect(VisualElement element, VisualElement focused,
            List<AccessibleNode> into, LiveRegionKind inherited, KeyModifiers accelerator,
            Dictionary<VisualElement, VisualElement> labels)
        {
            foreach (VisualElement child in element.Children)
            {
                if (child.IsHidden || child.Role == AccessibleRole.Presentation)
                {
                    continue;
                }

                LiveRegionKind live = child.LiveRegion == LiveRegionKind.None
                    ? inherited
                    : child.LiveRegion;

                if (IsExposed(child) && !IsLabel(child, labels))
                {
                    into.Add(NodeFor(child, focused, inherited, accelerator, labels));
                }
                else
                {
                    Collect(child, focused, into, live, accelerator, labels);
                }
            }
        }

        private static void CollectLayers(VisualElement element, VisualElement focused,
            List<AccessibleNode> into, LiveRegionKind inherited, KeyModifiers accelerator,
            Dictionary<VisualElement, VisualElement> labels)
        {
            foreach (VisualElement child in element.Children)
            {
                if (child.IsHidden || child.Role == AccessibleRole.Presentation)
                {
                    continue;
                }

                if (!child.IsOverlay)
                {
                    CollectLayers(child, focused, into, inherited, accelerator, labels);
                    continue;
                }

                if (IsExposed(child))
                {
                    into.Add(NodeFor(child, focused, inherited, accelerator, labels));
                }
                else
                {
                    Collect(child, focused, into, inherited, accelerator, labels);
                }
            }
        }

        private static bool IsExposed(VisualElement element)
        {
            return element.Role != AccessibleRole.None
                || element.Command != null
                || element.HasShortcut
                || element.LiveRegion != LiveRegionKind.None
                || element.Focusable
                || element.Scrollable
                || element is TextField
                || element is Image
                || !string.IsNullOrEmpty(element.Label)
                || !string.IsNullOrEmpty(element.Text);
        }

        private static AccessibleRole RoleOf(VisualElement element)
        {
            if (element.Role != AccessibleRole.None)
            {
                return element.Role;
            }

            if (element is TextField)
            {
                return AccessibleRole.TextField;
            }

            if (element is Image)
            {
                return AccessibleRole.Image;
            }

            if (element.Scrollable)
            {
                return AccessibleRole.Group;
            }

            return string.IsNullOrEmpty(element.Text) ? AccessibleRole.None : AccessibleRole.Text;
        }

        private static string NameOf(VisualElement element, Dictionary<VisualElement, VisualElement> labels)
        {
            if (!string.IsNullOrEmpty(element.Label))
            {
                return element.Label;
            }

            string labelled = LabelTextOf(element, labels);

            if (labelled != null)
            {
                return labelled;
            }

            if (element is TextField field)
            {
                return string.IsNullOrEmpty(field.Placeholder) ? null : field.Placeholder;
            }

            AccessibleRole role = RoleOf(element);

            if (!string.IsNullOrEmpty(element.Text) && !TakesValueFromText(role))
            {
                return element.Text;
            }

            if (!TakesNameFromContent(role))
            {
                return null;
            }

            var builder = new StringBuilder();

            AppendDescendantText(element, builder);

            return builder.Length == 0 ? null : builder.ToString();
        }

        private static bool TakesNameFromContent(AccessibleRole role)
        {
            return role == AccessibleRole.Button
                || role == AccessibleRole.Link
                || role == AccessibleRole.CheckBox
                || role == AccessibleRole.RadioButton
                || role == AccessibleRole.Switch
                || role == AccessibleRole.Tab
                || role == AccessibleRole.MenuItem
                || role == AccessibleRole.TreeItem
                || role == AccessibleRole.ColumnHeader
                || role == AccessibleRole.Heading;
        }

        private static void AppendDescendantText(VisualElement element, StringBuilder builder)
        {
            foreach (VisualElement child in element.Children)
            {
                if (child is TextField
                    || child.Role == AccessibleRole.Presentation
                    || child.IsHidden
                    || child.IsOverlay)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(child.Text))
                {
                    if (builder.Length > 0)
                    {
                        builder.Append(' ');
                    }

                    builder.Append(child.Text);
                }

                AppendDescendantText(child, builder);
            }
        }

        private static string ValueOf(VisualElement element)
        {
            if (!string.IsNullOrEmpty(element.AccessibleValue))
            {
                return element.AccessibleValue;
            }

            if (element is TextField field)
            {
                return field.IsMasked ? null : field.Text;
            }

            if (!TakesValueFromText(RoleOf(element)))
            {
                return null;
            }

            if (!string.IsNullOrEmpty(element.Text))
            {
                return element.Text;
            }

            var builder = new StringBuilder();

            AppendDescendantText(element, builder);

            return builder.Length == 0 ? null : builder.ToString();
        }

        private static bool TakesValueFromText(AccessibleRole role)
        {
            return role == AccessibleRole.TextField || role == AccessibleRole.ComboBox;
        }

        private static AccessibleActions ActionsOf(VisualElement element, AccessibleRole role)
        {
            AccessibleActions actions = AccessibleActions.None;

            if (!element.IsEnabled)
            {
                return actions;
            }

            if (IsInvocable(role) || element.Command != null)
            {
                actions |= AccessibleActions.Invoke;
            }

            if (element.Focusable)
            {
                actions |= AccessibleActions.Focus;
            }

            if (element is TextField)
            {
                actions |= AccessibleActions.SetValue;
            }

            if (element is IExpandable expandable && expandable.CanExpand)
            {
                actions |= expandable.IsExpanded
                    ? AccessibleActions.Collapse
                    : AccessibleActions.Expand;
            }

            if (IsSelectable(role))
            {
                actions |= AccessibleActions.Select;
            }

            if (element is IAccessibleRange range && range.HasRange && range.CanSetValue)
            {
                actions |= AccessibleActions.SetRangeValue;
            }

            if (Input.ScrollNavigator.Find(element.Parent, 0, 1) != null
                || Input.ScrollNavigator.Find(element.Parent, 0, -1) != null)
            {
                actions |= AccessibleActions.ScrollIntoView;
            }

            return actions;
        }

        private static bool IsSelectable(AccessibleRole role)
        {
            return role == AccessibleRole.Tab
                || role == AccessibleRole.RadioButton
                || role == AccessibleRole.ListItem
                || role == AccessibleRole.TreeItem
                || role == AccessibleRole.TableRow;
        }

        private static bool IsInvocable(AccessibleRole role)
        {
            return role == AccessibleRole.Button
                || role == AccessibleRole.Link
                || role == AccessibleRole.MenuItem
                || role == AccessibleRole.Tab
                || role == AccessibleRole.CheckBox
                || role == AccessibleRole.RadioButton
                || role == AccessibleRole.Switch
                || role == AccessibleRole.TreeItem
                || role == AccessibleRole.ColumnHeader
                || role == AccessibleRole.ComboBox;
        }

        private static string DescriptionOf(VisualElement element)
        {
            if (!string.IsNullOrEmpty(element.Error))
            {
                return element.Error;
            }

            return string.IsNullOrEmpty(element.Description) ? null : element.Description;
        }

        private static AccessibleStates StatesOf(VisualElement element, VisualElement focused)
        {
            AccessibleStates states = AccessibleStates.None;

            if (element.Focusable)
            {
                states |= AccessibleStates.Focusable;
            }

            if (element == focused)
            {
                states |= AccessibleStates.Focused;
            }

            if (element.Scrollable)
            {
                states |= AccessibleStates.Scrollable;
            }

            if (element is TextField field)
            {
                if (field.Multiline)
                {
                    states |= AccessibleStates.Multiline;
                }

                if (field.IsMasked)
                {
                    states |= AccessibleStates.Protected;
                }
            }

            if (element.HasState(Visual.Styles.StyleStates.CHECKED))
            {
                states |= AccessibleStates.Checked;
            }

            if (element.HasState(Visual.Styles.StyleStates.SELECTED))
            {
                states |= AccessibleStates.Selected;
            }

            if (element is IExpandable expandable)
            {
                if (expandable.CanExpand)
                {
                    states |= AccessibleStates.Expandable;
                }

                if (expandable.IsExpanded)
                {
                    states |= AccessibleStates.Expanded;
                }
            }
            else if (element.HasState(Visual.Styles.StyleStates.EXPANDED))
            {
                states |= AccessibleStates.Expanded;
            }

            if (element.HasState(Visual.Styles.StyleStates.INVALID))
            {
                states |= AccessibleStates.Invalid;
            }

            if (!element.IsEnabled)
            {
                states |= AccessibleStates.Disabled;
            }

            if (element.Clip == null || element.Clip.IsVoidOrInvalid)
            {
                states |= AccessibleStates.Offscreen;
            }

            return states;
        }
    }
}
