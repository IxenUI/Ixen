using Ixen.Core;
using Ixen.Core.Accessibility;
using Ixen.Core.Visual;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Ixen.Platform.Windows.Accessibility
{
    internal sealed class UiaBridge
    {
        private sealed class Snapshot
        {
            internal readonly Dictionary<int, AccessibleNode> Nodes
                = new Dictionary<int, AccessibleNode>();

            internal readonly Dictionary<int, int> Parents = new Dictionary<int, int>();

            internal readonly Dictionary<int, List<int>> ChildIds
                = new Dictionary<int, List<int>>();

            internal int RootId = -1;
        }

        private readonly IxenSurface _surface;
        private readonly Action _invalidate;
        private readonly Func<IntPtr> _handle;

        private readonly ConcurrentDictionary<VisualElement, int> _ids
            = new ConcurrentDictionary<VisualElement, int>();

        private readonly ConcurrentDictionary<int, UiaProvider> _providers
            = new ConcurrentDictionary<int, UiaProvider>();

        private readonly ConcurrentQueue<(int Id, AccessibleActions Action, string Value)> _pending
            = new ConcurrentQueue<(int, AccessibleActions, string)>();

        private Snapshot _snapshot = new Snapshot();
        private IRawElementProviderSimple _host;
        private int _nextId;

        internal UiaBridge(IxenSurface surface, Func<IntPtr> handle, Action invalidate)
        {
            _surface = surface;
            _handle = handle;
            _invalidate = invalidate;
        }

        internal IRawElementProviderFragmentRoot Root
            => ProviderFor(_snapshot.RootId) as IRawElementProviderFragmentRoot;

        internal IRawElementProviderSimple HostProvider
        {
            get
            {
                if (_host == null)
                {
                    UiaNative.UiaHostProviderFromHwnd(_handle(), out _host);
                }

                return _host;
            }
        }

        internal IntPtr Answer(IntPtr wParam, IntPtr lParam)
        {
            if (lParam.ToInt64() != UIA_ROOT_OBJECT_ID)
            {
                return IntPtr.Zero;
            }

            Refresh();

            if (!(Root is IRawElementProviderSimple provider))
            {
                return IntPtr.Zero;
            }

            return UiaNative.UiaReturnRawElementProvider(_handle(), wParam, lParam, provider);
        }

        private const long UIA_ROOT_OBJECT_ID = -25;

        internal void Sync()
        {
            Drain();

            if (!UiaNative.UiaClientsAreListening())
            {
                return;
            }

            Refresh();
        }

        private void Refresh()
        {
            AccessibleNode root = _surface.BuildAccessibilityTree();

            if (root == null)
            {
                return;
            }

            var snapshot = new Snapshot();
            var seen = new HashSet<VisualElement>();

            snapshot.RootId = Walk(root, -1, snapshot, seen);

            foreach (KeyValuePair<VisualElement, int> entry in _ids)
            {
                if (seen.Contains(entry.Key))
                {
                    continue;
                }

                _ids.TryRemove(entry.Key, out _);
                _providers.TryRemove(entry.Value, out _);
            }

            Snapshot previous = _snapshot;

            _snapshot = snapshot;

            if (previous.RootId >= 0)
            {
                Announce(previous, snapshot);
            }
        }

        private void Announce(Snapshot previous, Snapshot current)
        {
            foreach (KeyValuePair<int, AccessibleNode> entry in current.Nodes)
            {
                if (!previous.Nodes.TryGetValue(entry.Key, out AccessibleNode was))
                {
                    continue;
                }

                AccessibleNode now = entry.Value;

                bool spoke = false;

                if (was.Name != now.Name)
                {
                    Raise(entry.Key, UiaProperty.NAME, was.Name, now.Name);
                    spoke = true;
                }

                if (was.Value != now.Value)
                {
                    Raise(entry.Key, UiaProperty.VALUE_VALUE, was.Value, now.Value);
                    spoke = true;
                }

                if (spoke && now.Live != LiveRegionKind.None)
                {
                    Event(entry.Key, UiaEvent.LIVE_REGION_CHANGED);
                }

                bool had = was.HasState(AccessibleStates.Focused);
                bool has = now.HasState(AccessibleStates.Focused);

                if (had == has)
                {
                    continue;
                }

                Raise(entry.Key, UiaProperty.HAS_KEYBOARD_FOCUS, had, has);

                if (has)
                {
                    Event(entry.Key, UiaEvent.FOCUS_CHANGED);
                }
            }

            foreach (KeyValuePair<int, List<int>> entry in current.ChildIds)
            {
                if (!previous.ChildIds.TryGetValue(entry.Key, out List<int> was)
                    || Same(was, entry.Value))
                {
                    continue;
                }

                Structure(entry.Key);
            }
        }

        private static bool Same(List<int> left, List<int> right)
        {
            if (left.Count != right.Count)
            {
                return false;
            }

            for (int index = 0; index < left.Count; index++)
            {
                if (left[index] != right[index])
                {
                    return false;
                }
            }

            return true;
        }

        private void Raise(int id, int property, object was, object now)
        {
            if (ProviderFor(id) is IRawElementProviderSimple provider)
            {
                UiaNative.UiaRaiseAutomationPropertyChangedEvent(provider, property, was, now);
            }
        }

        private void Event(int id, int eventId)
        {
            if (ProviderFor(id) is IRawElementProviderSimple provider)
            {
                UiaNative.UiaRaiseAutomationEvent(provider, eventId);
            }
        }

        private void Structure(int id)
        {
            if (ProviderFor(id) is IRawElementProviderSimple provider)
            {
                UiaNative.UiaRaiseStructureChangedEvent(provider,
                    StructureChangeType.ChildrenInvalidated,
                    new[] { UiaNative.UIA_APPEND_RUNTIME_ID, id },
                    2);
            }
        }

        private int Walk(AccessibleNode node, int parentId, Snapshot snapshot,
            HashSet<VisualElement> seen)
        {
            int id = IdOf(node);

            seen.Add(node.Element);

            snapshot.Nodes[id] = node;
            snapshot.Parents[id] = parentId;

            var children = new List<int>();

            snapshot.ChildIds[id] = children;

            foreach (AccessibleNode child in node.Children)
            {
                children.Add(Walk(child, id, snapshot, seen));
            }

            return id;
        }

        private int IdOf(AccessibleNode node)
        {
            if (node.Element == null)
            {
                return _nextId++;
            }

            return _ids.GetOrAdd(node.Element, _ => ++_nextId);
        }

        internal AccessibleNode NodeOf(int id)
            => _snapshot.Nodes.TryGetValue(id, out AccessibleNode node) ? node : null;

        private UiaProvider ProviderFor(int id)
        {
            if (id < 0)
            {
                return null;
            }

            return _providers.GetOrAdd(id,
                key => key == _snapshot.RootId
                    ? new UiaRootProvider(this, key)
                    : (UiaProvider)new UiaProvider(this, key));
        }

        internal IRawElementProviderFragment Navigate(int id, NavigateDirection direction)
        {
            Snapshot snapshot = _snapshot;

            if (!snapshot.Nodes.ContainsKey(id))
            {
                return null;
            }

            switch (direction)
            {
                case NavigateDirection.Parent:
                    return snapshot.Parents.TryGetValue(id, out int parent) && parent >= 0
                        ? ProviderFor(parent)
                        : null;

                case NavigateDirection.FirstChild:
                    return Child(snapshot, id, 0);

                case NavigateDirection.LastChild:
                    return Child(snapshot, id, -1);

                case NavigateDirection.NextSibling:
                    return Sibling(snapshot, id, 1);

                case NavigateDirection.PreviousSibling:
                    return Sibling(snapshot, id, -1);

                default:
                    return null;
            }
        }

        private IRawElementProviderFragment Child(Snapshot snapshot, int id, int index)
        {
            if (!snapshot.ChildIds.TryGetValue(id, out List<int> children) || children.Count == 0)
            {
                return null;
            }

            return ProviderFor(index < 0 ? children[children.Count - 1] : children[index]);
        }

        private IRawElementProviderFragment Sibling(Snapshot snapshot, int id, int step)
        {
            if (!snapshot.Parents.TryGetValue(id, out int parent) || parent < 0)
            {
                return null;
            }

            if (!snapshot.ChildIds.TryGetValue(parent, out List<int> children))
            {
                return null;
            }

            int at = children.IndexOf(id) + step;

            return at >= 0 && at < children.Count ? ProviderFor(children[at]) : null;
        }

        internal UiaRect RectangleOf(int id)
        {
            AccessibleNode node = NodeOf(id);

            if (node == null)
            {
                return default;
            }

            float scale = _surface.Scale;

            var origin = new UiaNative.Point
            {
                X = (int)Math.Round(node.X * scale),
                Y = (int)Math.Round(node.Y * scale)
            };

            if (!UiaNative.ClientToScreen(_handle(), ref origin))
            {
                return default;
            }

            return new UiaRect
            {
                Left = origin.X,
                Top = origin.Y,
                Width = node.Width * scale,
                Height = node.Height * scale
            };
        }

        internal IRawElementProviderFragment FromPoint(double x, double y)
        {
            Snapshot snapshot = _snapshot;

            if (snapshot.RootId < 0)
            {
                return null;
            }

            var point = new UiaNative.Point { X = 0, Y = 0 };

            if (!UiaNative.ClientToScreen(_handle(), ref point))
            {
                return null;
            }

            float scale = _surface.Scale;
            double localX = (x - point.X) / scale;
            double localY = (y - point.Y) / scale;

            int found = Deepest(snapshot, snapshot.RootId, localX, localY);

            return found >= 0 ? ProviderFor(found) : null;
        }

        private int Deepest(Snapshot snapshot, int id, double x, double y)
        {
            AccessibleNode node = NodeOf(id);

            if (node == null || x < node.X || y < node.Y
                || x >= node.X + node.Width || y >= node.Y + node.Height)
            {
                return -1;
            }

            if (snapshot.ChildIds.TryGetValue(id, out List<int> children))
            {
                for (int index = children.Count - 1; index >= 0; index--)
                {
                    int hit = Deepest(snapshot, children[index], x, y);

                    if (hit >= 0)
                    {
                        return hit;
                    }
                }
            }

            return id;
        }

        internal IRawElementProviderFragment Focused()
        {
            Snapshot snapshot = _snapshot;

            foreach (KeyValuePair<int, AccessibleNode> entry in snapshot.Nodes)
            {
                if (entry.Value.HasState(AccessibleStates.Focused))
                {
                    return ProviderFor(entry.Key);
                }
            }

            return null;
        }

        internal void Post(int id, AccessibleActions action, string value)
        {
            _pending.Enqueue((id, action, value));
            _invalidate();
        }

        private void Drain()
        {
            while (_pending.TryDequeue(out (int Id, AccessibleActions Action, string Value) work))
            {
                AccessibleNode node = NodeOf(work.Id);

                if (node == null)
                {
                    continue;
                }

                _surface.Perform(node, work.Action, work.Value);
            }
        }
    }
}
