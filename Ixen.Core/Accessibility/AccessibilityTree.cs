using Ixen.Core.Visual;
using System.Collections.Generic;
using System.Text;

namespace Ixen.Core.Accessibility
{
    internal static class AccessibilityTree
    {
        internal static AccessibleNode Build(VisualElement root, VisualElement focused)
        {
            return root == null ? null : NodeFor(root, focused);
        }

        private static AccessibleNode NodeFor(VisualElement element, VisualElement focused)
        {
            AccessibleRole role = RoleOf(element);

            var node = new AccessibleNode
            {
                Element = element,
                Role = role == AccessibleRole.None ? AccessibleRole.Group : role,
                Name = NameOf(element),
                Description = string.IsNullOrEmpty(element.Description) ? null : element.Description,
                Value = ValueOf(element),
                States = StatesOf(element, focused),
                Actions = ActionsOf(element, role),
                X = element.X,
                Y = element.Y,
                Width = element.ActualWidth,
                Height = element.ActualHeight
            };

            if (!TakesNameFromContent(node.Role))
            {
                Collect(element, focused, node.ChildList);
            }

            return node;
        }

        private static void Collect(VisualElement element, VisualElement focused,
            List<AccessibleNode> into)
        {
            foreach (VisualElement child in element.Children)
            {
                if (child.IsHidden || child.Role == AccessibleRole.Presentation)
                {
                    continue;
                }

                if (IsExposed(child))
                {
                    into.Add(NodeFor(child, focused));
                }
                else
                {
                    Collect(child, focused, into);
                }
            }
        }

        private static bool IsExposed(VisualElement element)
        {
            return element.Role != AccessibleRole.None
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

        private static string NameOf(VisualElement element)
        {
            if (!string.IsNullOrEmpty(element.Label))
            {
                return element.Label;
            }

            if (element is TextField field)
            {
                return string.IsNullOrEmpty(field.Placeholder) ? null : field.Placeholder;
            }

            AccessibleRole role = RoleOf(element);

            if (!string.IsNullOrEmpty(element.Text) && !TakesValueFromText(role)
                && !TextIsAMark(role))
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

        private static bool TextIsAMark(AccessibleRole role)
        {
            return role == AccessibleRole.CheckBox
                || role == AccessibleRole.RadioButton
                || role == AccessibleRole.Switch;
        }

        private static AccessibleActions ActionsOf(VisualElement element, AccessibleRole role)
        {
            AccessibleActions actions = AccessibleActions.None;

            if (!element.IsEnabled)
            {
                return actions;
            }

            if (IsInvocable(role))
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

            if (Input.ScrollNavigator.Find(element.Parent, 0, 1) != null
                || Input.ScrollNavigator.Find(element.Parent, 0, -1) != null)
            {
                actions |= AccessibleActions.ScrollIntoView;
            }

            return actions;
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
                || role == AccessibleRole.ComboBox;
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
