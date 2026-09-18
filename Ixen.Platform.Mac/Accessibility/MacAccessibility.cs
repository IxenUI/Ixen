using Ixen.Core;
using Ixen.Core.Accessibility;
using Ixen.Platform.Mac.NativeApi;
using System;
using System.Collections.Generic;

namespace Ixen.Platform.Mac.Accessibility
{
    internal sealed class MacAccessibility
    {
        private static readonly int[] NOTHING = new int[0];

        private readonly IxenSurface _surface;
        private readonly Func<IntPtr> _window;

        private readonly AccessibilityIds _ids = new AccessibilityIds();
        private readonly List<int> _dropped = new List<int>();
        private readonly List<AccessibilityChange> _changes = new List<AccessibilityChange>();

        private readonly Dictionary<int, MacAccessibleNode> _pushed
            = new Dictionary<int, MacAccessibleNode>();

        private readonly MacApi.OnAccessibilityCallBack _onAction;

        private AccessibilitySnapshot _snapshot = AccessibilitySnapshot.Empty;
        private int[] _order = NOTHING;

        internal MacAccessibility(IxenSurface surface, Func<IntPtr> window)
        {
            _surface = surface;
            _window = window;
            _onAction = OnAction;
        }

        internal void Register() => MacApi.RegisterAccessibilityCallBack(_window(), _onAction);

        internal void Sync()
        {
            if (MacApi.AccessibilityIsActive(_window()) == 0)
            {
                return;
            }

            AccessibleNode root = _surface.BuildAccessibilityTree();

            if (root == null)
            {
                return;
            }

            _dropped.Clear();

            AccessibilitySnapshot snapshot = AccessibilitySnapshot.Take(root, _ids, _dropped);
            AccessibilitySnapshot previous = _snapshot;

            _snapshot = snapshot;

            Push(snapshot);

            _changes.Clear();

            AccessibilitySnapshot.Diff(previous, snapshot, _changes);

            Announce();
        }

        private void Push(AccessibilitySnapshot snapshot)
        {
            IntPtr window = _window();
            bool moved = _dropped.Count > 0;

            for (int index = 0; index < _dropped.Count; index++)
            {
                _pushed.Remove(_dropped[index]);
            }

            float scale = _surface.Scale;
            IReadOnlyList<int> order = snapshot.Order;

            for (int index = 0; index < order.Count; index++)
            {
                int id = order[index];
                var node = new MacAccessibleNode(snapshot.ParentOf(id), snapshot.NodeOf(id), scale);

                if (_pushed.TryGetValue(id, out MacAccessibleNode was) && was.SameAs(node))
                {
                    continue;
                }

                _pushed[id] = node;

                MacApi.AccessibilityUpdateNode(window, id, node.Parent, node.Role, node.States,
                    node.Actions, node.Toggle, node.X, node.Y, node.Width, node.Height,
                    node.Name, node.Value, node.Help);

                moved = true;
            }

            if (!moved && Same(order))
            {
                return;
            }

            _order = new int[order.Count];

            for (int index = 0; index < order.Count; index++)
            {
                _order[index] = order[index];
            }

            MacApi.AccessibilityCommit(window, snapshot.RootId, _order, _order.Length);
        }

        private bool Same(IReadOnlyList<int> order)
        {
            if (_order.Length != order.Count)
            {
                return false;
            }

            for (int index = 0; index < order.Count; index++)
            {
                if (_order[index] != order[index])
                {
                    return false;
                }
            }

            return true;
        }

        private void Announce()
        {
            IntPtr window = _window();

            for (int index = 0; index < _changes.Count; index++)
            {
                AccessibilityChange change = _changes[index];

                switch (change.Kind)
                {
                    case AccessibilityChangeKind.Name:
                        Notify(window, change.Id, MacAccessibilityNotice.Title, null);
                        break;

                    case AccessibilityChangeKind.Value:
                        Notify(window, change.Id, MacAccessibilityNotice.Value, null);
                        break;

                    case AccessibilityChangeKind.Focus:
                        if (change.TookFocus)
                        {
                            Notify(window, change.Id, MacAccessibilityNotice.Focus, null);
                        }

                        break;

                    case AccessibilityChangeKind.Structure:
                        Notify(window, change.Id, MacAccessibilityNotice.Layout, null);
                        break;

                    case AccessibilityChangeKind.LiveRegion:
                        Notify(window, change.Id,
                            change.Current.Live == LiveRegionKind.Assertive
                                ? MacAccessibilityNotice.AnnounceUrgent
                                : MacAccessibilityNotice.Announce,
                            Spoken(change));
                        break;
                }
            }
        }

        private static void Notify(IntPtr window, int id, MacAccessibilityNotice notice, string text)
            => MacApi.AccessibilityNotify(window, id, (int)notice, text);

        internal static string Spoken(AccessibilityChange change)
        {
            if (change.Previous.Value != change.Current.Value
                && !string.IsNullOrEmpty(change.Current.Value))
            {
                return change.Current.Value;
            }

            return change.Current.Name;
        }

        private int OnAction(int identifier, int action, string value)
        {
            var wanted = (AccessibleActions)action;
            AccessibleNode node = _snapshot.NodeOf(identifier);

            if (node == null || !node.Supports(wanted))
            {
                return 0;
            }

            _surface.Post(() => Perform(identifier, wanted, value));

            return 1;
        }

        private void Perform(int identifier, AccessibleActions action, string value)
        {
            AccessibleNode node = _snapshot.NodeOf(identifier);

            if (node != null)
            {
                _surface.Perform(node, action, value);
            }
        }
    }
}
