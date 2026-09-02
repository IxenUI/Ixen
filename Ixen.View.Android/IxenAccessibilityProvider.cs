using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Views.Accessibility;
using Ixen.Core;
using Ixen.Core.Accessibility;
using Ixen.Core.Visual;
using System.Collections.Generic;

namespace Ixen.View.Android
{
    internal class IxenAccessibilityProvider : AccessibilityNodeProvider
    {
        private const int HOST = -1;

        private readonly IxenView _view;
        private readonly IxenSurface _surface;

        private readonly Dictionary<VisualElement, int> _ids
            = new Dictionary<VisualElement, int>();

        private readonly Dictionary<int, AccessibleNode> _nodes
            = new Dictionary<int, AccessibleNode>();

        private readonly Dictionary<int, int> _parents = new Dictionary<int, int>();
        private readonly Dictionary<int, List<int>> _children = new Dictionary<int, List<int>>();

        private int _nextId;
        private int _rootId = HOST;

        internal IxenAccessibilityProvider(IxenView view, IxenSurface surface)
        {
            _view = view;
            _surface = surface;
        }

        private void Refresh()
        {
            AccessibleNode root = _surface.BuildAccessibilityTree();

            _nodes.Clear();
            _parents.Clear();
            _children.Clear();

            _rootId = root == null ? HOST : Walk(root, HOST);
        }

        private int Walk(AccessibleNode node, int parentId)
        {
            int id = IdOf(node.Element);

            _nodes[id] = node;
            _parents[id] = parentId;

            var children = new List<int>();

            _children[id] = children;

            foreach (AccessibleNode child in node.Children)
            {
                children.Add(Walk(child, id));
            }

            return id;
        }

        private int IdOf(VisualElement element)
        {
            if (element == null)
            {
                return _nextId++;
            }

            if (!_ids.TryGetValue(element, out int id))
            {
                id = _nextId++;
                _ids[element] = id;
            }

            return id;
        }

        public override AccessibilityNodeInfo CreateAccessibilityNodeInfo(int virtualViewId)
        {
            Refresh();

            if (virtualViewId == HOST)
            {
                return Host();
            }

            if (!_nodes.TryGetValue(virtualViewId, out AccessibleNode node))
            {
                return null;
            }

            return Info(virtualViewId, node);
        }

        private AccessibilityNodeInfo Host()
        {
            AccessibilityNodeInfo info = Fresh(HOST);

            _view.OnInitializeAccessibilityNodeInfo(info);

            if (_rootId != HOST)
            {
                info.AddChild(_view, _rootId);
            }

            return info;
        }

        private AccessibilityNodeInfo Info(int id, AccessibleNode node)
        {
            AccessibilityNodeInfo info = Fresh(id);

            info.PackageName = _view.Context?.PackageName;
            info.ClassName = ClassNameOf(node.Role);
            info.ContentDescription = node.Name;

            if (node.Value != null)
            {
                info.Text = node.Value;
            }

            info.SetParent(_view, _parents.TryGetValue(id, out int parent) ? parent : HOST);

            if (_children.TryGetValue(id, out List<int> children))
            {
                foreach (int child in children)
                {
                    info.AddChild(_view, child);
                }
            }

            info.Focusable = node.HasState(AccessibleStates.Focusable);
            info.Enabled = !node.HasState(AccessibleStates.Disabled);
            info.Password = node.HasState(AccessibleStates.Protected);
            info.VisibleToUser = !node.HasState(AccessibleStates.Offscreen);
            info.Focused = node.HasState(AccessibleStates.Focused);
            info.Checkable = Checkable(node.Role);

            if (info.Checkable)
            {
                SetChecked(info, node.HasState(AccessibleStates.Checked));
            }

            info.Selected = node.HasState(AccessibleStates.Selected);
            info.LiveRegion = LiveOf(node.Live);

            info.AddAction(AccessibilityNodeInfo.AccessibilityAction.ActionAccessibilityFocus);
            info.AddAction(AccessibilityNodeInfo.AccessibilityAction.ActionClearAccessibilityFocus);

            if (node.Supports(AccessibleActions.Invoke))
            {
                info.AddAction(AccessibilityNodeInfo.AccessibilityAction.ActionClick);
                info.Clickable = true;
            }

            if (node.Supports(AccessibleActions.Focus))
            {
                info.AddAction(AccessibilityNodeInfo.AccessibilityAction.ActionFocus);
            }

            if (node.Supports(AccessibleActions.SetValue))
            {
                info.AddAction(AccessibilityNodeInfo.AccessibilityAction.ActionSetText);
                info.Editable = true;
            }

            if (node.Supports(AccessibleActions.ScrollIntoView)
                && System.OperatingSystem.IsAndroidVersionAtLeast(23))
            {
                info.AddAction(AccessibilityNodeInfo.AccessibilityAction.ActionShowOnScreen);
            }


            info.SetBoundsInScreen(BoundsOf(node));

            return info;
        }

        private static void SetChecked(AccessibilityNodeInfo info, bool value)
        {
            if (System.OperatingSystem.IsAndroidVersionAtLeast(36))
            {
                info.CheckedState = value
                    ? CheckedState.True
                    : CheckedState.False;

                return;
            }

            info.Checked = value;
        }

        private AccessibilityNodeInfo Fresh(int id)
        {
            if (System.OperatingSystem.IsAndroidVersionAtLeast(30))
            {
                return id == HOST
                    ? new AccessibilityNodeInfo(_view)
                    : new AccessibilityNodeInfo(_view, id);
            }

            return id == HOST
                ? AccessibilityNodeInfo.Obtain(_view)
                : AccessibilityNodeInfo.Obtain(_view, id);
        }

        private static AccessibilityEvent FreshEvent(EventTypes type)
        {
            return System.OperatingSystem.IsAndroidVersionAtLeast(30)
                ? new AccessibilityEvent { EventType = type }
                : AccessibilityEvent.Obtain(type);
        }

        private Rect BoundsOf(AccessibleNode node)
        {
            float scale = _surface.Scale;
            var offset = new int[2];

            _view.GetLocationOnScreen(offset);

            int left = offset[0] + (int)(node.X * scale);
            int top = offset[1] + (int)(node.Y * scale);

            return new Rect(left, top,
                left + (int)(node.Width * scale),
                top + (int)(node.Height * scale));
        }

        private static AccessibilityLiveRegion LiveOf(LiveRegionKind live)
        {
            switch (live)
            {
                case LiveRegionKind.Polite:
                    return AccessibilityLiveRegion.Polite;

                case LiveRegionKind.Assertive:
                    return AccessibilityLiveRegion.Assertive;

                default:
                    return AccessibilityLiveRegion.None;
            }
        }

        private static bool Checkable(AccessibleRole role)
            => role == AccessibleRole.CheckBox
                || role == AccessibleRole.RadioButton
                || role == AccessibleRole.Switch;

        private static string ClassNameOf(AccessibleRole role)
        {
            switch (role)
            {
                case AccessibleRole.Button: return "android.widget.Button";
                case AccessibleRole.Link: return "android.widget.TextView";
                case AccessibleRole.CheckBox: return "android.widget.CheckBox";
                case AccessibleRole.RadioButton: return "android.widget.RadioButton";
                case AccessibleRole.Switch: return "android.widget.Switch";
                case AccessibleRole.TextField: return "android.widget.EditText";
                case AccessibleRole.Slider: return "android.widget.SeekBar";
                case AccessibleRole.ProgressBar: return "android.widget.ProgressBar";
                case AccessibleRole.List: return "android.widget.ListView";
                case AccessibleRole.ListItem: return "android.view.View";
                case AccessibleRole.Tree: return "android.widget.ListView";
                case AccessibleRole.TreeItem: return "android.view.View";
                case AccessibleRole.Table: return "android.widget.GridView";
                case AccessibleRole.ColumnHeader: return "android.view.View";
                case AccessibleRole.Tab: return "android.widget.TabWidget";
                case AccessibleRole.TabList: return "android.widget.TabHost";
                case AccessibleRole.Menu: return "android.widget.ListView";
                case AccessibleRole.MenuItem: return "android.view.View";
                case AccessibleRole.ComboBox: return "android.widget.Spinner";
                case AccessibleRole.Dialog: return "android.view.View";
                case AccessibleRole.ScrollBar: return "android.widget.ScrollView";
                case AccessibleRole.Image: return "android.widget.ImageView";
                case AccessibleRole.Text: return "android.widget.TextView";
                case AccessibleRole.Heading: return "android.widget.TextView";
                default: return "android.view.View";
            }
        }

        public override bool PerformAction(int virtualViewId, Action action, Bundle arguments)
        {
            if (virtualViewId == HOST)
            {
                return base.PerformAction(virtualViewId, action, arguments);
            }

            Refresh();

            if (!_nodes.TryGetValue(virtualViewId, out AccessibleNode node))
            {
                return false;
            }

            if ((int)action == global::Android.Resource.Id.AccessibilityActionShowOnScreen)
            {
                return _surface.Perform(node, AccessibleActions.ScrollIntoView);
            }

            switch (action)
            {
                case Action.Click:
                    return _surface.Perform(node, AccessibleActions.Invoke);

                case Action.Focus:
                    return _surface.Perform(node, AccessibleActions.Focus);

                case Action.SetText:
                    return _surface.Perform(node, AccessibleActions.SetValue,
                        arguments?.GetCharSequence(
                            AccessibilityNodeInfo.ActionArgumentSetTextCharsequence));

                case Action.AccessibilityFocus:
                    _focused = virtualViewId;
                    Send(virtualViewId, EventTypes.ViewAccessibilityFocused);
                    return true;

                case Action.ClearAccessibilityFocus:
                    if (_focused == virtualViewId)
                    {
                        _focused = HOST;
                    }

                    Send(virtualViewId, EventTypes.ViewAccessibilityFocusCleared);
                    return true;

                default:
                    return false;
            }
        }

        private int _focused = HOST;

        public override AccessibilityNodeInfo FindFocus(NodeFocus focus)
        {
            Refresh();

            if (focus == NodeFocus.Accessibility)
            {
                return _focused == HOST ? Host() : CreateAccessibilityNodeInfo(_focused);
            }

            foreach (KeyValuePair<int, AccessibleNode> entry in _nodes)
            {
                if (entry.Value.HasState(AccessibleStates.Focused))
                {
                    return Info(entry.Key, entry.Value);
                }
            }

            return null;
        }

        internal void Send(int virtualViewId, EventTypes type)
        {
            if (!_view.IsShown)
            {
                return;
            }

            AccessibilityEvent args = FreshEvent(type);

            args.PackageName = _view.Context?.PackageName;
            args.SetSource(_view, virtualViewId);

            _view.Parent?.RequestSendAccessibilityEvent(_view, args);
        }
    }
}
