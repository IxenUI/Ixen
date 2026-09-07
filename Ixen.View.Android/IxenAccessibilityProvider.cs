using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Views.Accessibility;
using Ixen.Core;
using Ixen.Core.Accessibility;
using System.Collections.Generic;

namespace Ixen.View.Android
{
    internal class IxenAccessibilityProvider : AccessibilityNodeProvider
    {
        private const int HOST = -1;

        private readonly IxenView _view;
        private readonly IxenSurface _surface;

        private readonly AccessibilityIds _ids = new AccessibilityIds();
        private readonly List<AccessibilityChange> _changes = new List<AccessibilityChange>();

        private AccessibilitySnapshot _snapshot = AccessibilitySnapshot.Empty;
        private AccessibilitySnapshot _spoken = AccessibilitySnapshot.Empty;
        private AccessibilityManager _manager;
        private int _focused = HOST;

        internal IxenAccessibilityProvider(IxenView view, IxenSurface surface)
        {
            _view = view;
            _surface = surface;
        }

        private bool Listening
        {
            get
            {
                if (_manager == null)
                {
                    _manager = _view.Context?.GetSystemService(Context.AccessibilityService)
                        as AccessibilityManager;
                }

                return _manager != null && _manager.IsEnabled;
            }
        }

        private void Refresh()
        {
            _snapshot = AccessibilitySnapshot.Take(_surface.BuildAccessibilityTree(), _ids, null);
        }

        internal void Sync()
        {
            if (!Listening)
            {
                return;
            }

            Refresh();

            _changes.Clear();

            AccessibilitySnapshot.Diff(_spoken, _snapshot, _changes);

            _spoken = _snapshot;

            Announce();
        }

        private void Announce()
        {
            for (int index = 0; index < _changes.Count; index++)
            {
                AccessibilityChange change = _changes[index];

                switch (change.Kind)
                {
                    case AccessibilityChangeKind.Name:
                        Send(change.Id, EventTypes.WindowContentChanged,
                            ContentChangeTypes.ContentDescription);
                        break;

                    case AccessibilityChangeKind.Value:
                        Send(change.Id, EventTypes.WindowContentChanged, ContentChangeTypes.Text);
                        break;

                    case AccessibilityChangeKind.Focus:
                        if (change.TookFocus)
                        {
                            Send(change.Id, EventTypes.ViewFocused);
                        }

                        break;

                    case AccessibilityChangeKind.Structure:
                        Send(change.Id, EventTypes.WindowContentChanged,
                            ContentChangeTypes.Subtree);
                        break;
                }
            }
        }

        public override AccessibilityNodeInfo CreateAccessibilityNodeInfo(int virtualViewId)
        {
            Refresh();

            if (virtualViewId == HOST)
            {
                return Host();
            }

            AccessibleNode node = _snapshot.NodeOf(virtualViewId);

            return node == null ? null : Info(virtualViewId, node);
        }

        private AccessibilityNodeInfo Host()
        {
            AccessibilityNodeInfo info = Fresh(HOST);

            _view.OnInitializeAccessibilityNodeInfo(info);

            if (_snapshot.RootId != HOST)
            {
                info.AddChild(_view, _snapshot.RootId);
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

            info.SetParent(_view, _snapshot.ParentOf(id));

            IReadOnlyList<int> children = _snapshot.ChildrenOf(id);

            for (int index = 0; index < children.Count; index++)
            {
                info.AddChild(_view, children[index]);
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

            AccessibleNode node = _snapshot.NodeOf(virtualViewId);

            if (node == null)
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
                    if (!_surface.Perform(node, AccessibleActions.Focus))
                    {
                        return false;
                    }

                    _view.SyncSoftKeyboard();

                    return true;

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

        public override AccessibilityNodeInfo FindFocus(NodeFocus focus)
        {
            Refresh();

            if (focus == NodeFocus.Accessibility)
            {
                return _focused == HOST ? Host() : CreateAccessibilityNodeInfo(_focused);
            }

            int id = _snapshot.FocusedId();

            return id == HOST ? null : Info(id, _snapshot.NodeOf(id));
        }

        internal void Send(int virtualViewId, EventTypes type)
            => Send(virtualViewId, type, ContentChangeTypes.Undefined);

        private void Send(int virtualViewId, EventTypes type, ContentChangeTypes changes)
        {
            if (!_view.IsShown)
            {
                return;
            }

            AccessibilityEvent args = FreshEvent(type);

            args.PackageName = _view.Context?.PackageName;
            args.ContentChangeTypes = changes;
            args.SetSource(_view, virtualViewId);

            _view.Parent?.RequestSendAccessibilityEvent(_view, args);
        }
    }
}
