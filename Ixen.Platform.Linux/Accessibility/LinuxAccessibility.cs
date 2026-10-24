using Ixen.Core;
using Ixen.Core.Accessibility;
using Ixen.Platform.Linux.NativeApi;
using System;
using System.Collections.Generic;

namespace Ixen.Platform.Linux.Accessibility
{
    internal sealed class LinuxAccessibility
    {
        private static readonly int[] NOTHING = new int[0];

        private readonly IxenSurface _surface;
        private readonly Func<IntPtr> _window;

        private readonly AccessibilityIds _ids = new AccessibilityIds();
        private readonly List<int> _dropped = new List<int>();
        private readonly List<AccessibilityChange> _changes = new List<AccessibilityChange>();

        private readonly Dictionary<int, LinuxAccessibleNode> _pushed
            = new Dictionary<int, LinuxAccessibleNode>();

        private readonly LinuxApi.OnAccessibilityCallBack _onAction;

        private AccessibilitySnapshot _snapshot = AccessibilitySnapshot.Empty;
        private int[] _order = NOTHING;

        internal LinuxAccessibility(IxenSurface surface, Func<IntPtr> window)
        {
            _surface = surface;
            _window = window;
            _onAction = OnAction;
        }

        internal void Register() => LinuxApi.RegisterAccessibilityCallBack(_window(), _onAction);

        internal void Sync()
        {
            if (LinuxApi.AccessibilityIsActive(_window()) == 0)
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
                bool isRoot = id == snapshot.RootId;

                var node = new LinuxAccessibleNode(snapshot.ParentOf(id), snapshot.NodeOf(id),
                    isRoot, scale);

                if (_pushed.TryGetValue(id, out LinuxAccessibleNode was) && was.SameAs(node))
                {
                    continue;
                }

                _pushed[id] = node;

                LinuxApi.AccessibilityUpdateNode(window, id, node.Parent, node.Role, node.States,
                    node.Actions, node.X, node.Y, node.Width, node.Height,
                    node.Name, node.Description, node.Value, node.Shortcut);

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

            LinuxApi.AccessibilityCommit(window, snapshot.RootId, _order, _order.Length);
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
                        Notify(window, change.Id, LinuxAccessibilityNotice.Name, null);
                        break;

                    case AccessibilityChangeKind.Value:
                        Notify(window, change.Id, LinuxAccessibilityNotice.Value, null);

                        if (change.Current.Role == AccessibleRole.TextField)
                        {
                            foreach (LinuxTextChange edit in TextChanges(change))
                            {
                                LinuxApi.AccessibilityNotifyText(window, change.Id, edit.Inserted ? 1 : 0,
                                    edit.Offset, edit.Length, edit.Text);
                            }
                        }

                        break;

                    case AccessibilityChangeKind.Focus:
                        if (change.TookFocus)
                        {
                            Notify(window, change.Id, LinuxAccessibilityNotice.Focus, null);
                        }

                        break;

                    case AccessibilityChangeKind.Structure:
                        Notify(window, change.Id, LinuxAccessibilityNotice.Structure, null);
                        break;

                    case AccessibilityChangeKind.States:
                        foreach (LinuxAccessibilityNotice notice in StateNotices(change))
                        {
                            Notify(window, change.Id, notice, null);
                        }

                        break;

                    case AccessibilityChangeKind.LiveRegion:
                        Notify(window, change.Id,
                            change.Current.Live == LiveRegionKind.Assertive
                                ? LinuxAccessibilityNotice.AnnounceUrgent
                                : LinuxAccessibilityNotice.Announce,
                            Spoken(change));
                        break;
                }
            }
        }

        internal static IEnumerable<LinuxTextChange> TextChanges(AccessibilityChange change)
        {
            string before = change.Previous.Value ?? string.Empty;
            string after = change.Current.Value ?? string.Empty;
            TextDelta delta = TextDelta.Of(before, after);
            int offset = CodePoints(before, 0, delta.Start);

            if (delta.Removed > 0)
            {
                string removed = before.Substring(delta.Start, delta.Removed);

                yield return new LinuxTextChange(false, offset, CodePoints(removed, 0, removed.Length), removed);
            }

            if (delta.Added > 0)
            {
                string added = after.Substring(delta.Start, delta.Added);

                yield return new LinuxTextChange(true, offset, CodePoints(added, 0, added.Length), added);
            }
        }

        private static int CodePoints(string text, int start, int length)
        {
            int count = 0;

            for (int index = start; index < start + length; index++)
            {
                if (!char.IsLowSurrogate(text[index]))
                {
                    count++;
                }
            }

            return count;
        }

        internal static IEnumerable<LinuxAccessibilityNotice> StateNotices(AccessibilityChange change)
        {
            AccessibleStates was = change.Previous.States;
            AccessibleStates now = change.Current.States;

            if (Changed(was, now, AccessibleStates.Checked))
            {
                yield return LinuxAccessibilityNotice.Checked;
            }

            if (Changed(was, now, AccessibleStates.Expanded) || Changed(was, now, AccessibleStates.Expandable))
            {
                yield return LinuxAccessibilityNotice.Expanded;
            }

            if (Changed(was, now, AccessibleStates.Selected))
            {
                yield return LinuxAccessibilityNotice.Selected;
            }
        }

        private static bool Changed(AccessibleStates was, AccessibleStates now, AccessibleStates state)
            => (was & state) != (now & state);

        private static void Notify(IntPtr window, int id, LinuxAccessibilityNotice notice, string text)
            => LinuxApi.AccessibilityNotify(window, id, (int)notice, text);

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
