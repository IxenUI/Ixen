using Ixen.Core.Visual;
using System.Collections.Generic;

namespace Ixen.Core.Accessibility
{
    public sealed class AccessibilitySnapshot
    {
        public const int NONE = -1;

        private static readonly List<int> NO_CHILDREN = new List<int>();

        private readonly Dictionary<int, AccessibleNode> _nodes
            = new Dictionary<int, AccessibleNode>();

        private readonly Dictionary<int, int> _parents = new Dictionary<int, int>();

        private readonly Dictionary<int, List<int>> _children
            = new Dictionary<int, List<int>>();

        private readonly List<int> _order = new List<int>();

        public static AccessibilitySnapshot Empty { get; } = new AccessibilitySnapshot();

        public int RootId { get; private set; } = NONE;

        public int Count => _order.Count;

        public IReadOnlyList<int> Order => _order;

        public static AccessibilitySnapshot Take(AccessibleNode root, AccessibilityIds ids,
            List<int> dropped)
        {
            var snapshot = new AccessibilitySnapshot();

            if (root == null || ids == null)
            {
                return snapshot;
            }

            var seen = new HashSet<VisualElement>();

            snapshot.RootId = snapshot.Walk(root, NONE, ids, seen);

            ids.Prune(seen, dropped);

            return snapshot;
        }

        private int Walk(AccessibleNode node, int parentId, AccessibilityIds ids,
            HashSet<VisualElement> seen)
        {
            int id = ids.IdOf(node.Element);

            if (node.Element != null)
            {
                seen.Add(node.Element);
            }

            _nodes[id] = node;
            _parents[id] = parentId;
            _order.Add(id);

            var children = new List<int>();

            _children[id] = children;

            foreach (AccessibleNode child in node.Children)
            {
                children.Add(Walk(child, id, ids, seen));
            }

            return id;
        }

        public bool Contains(int id) => _nodes.ContainsKey(id);

        public AccessibleNode NodeOf(int id)
            => _nodes.TryGetValue(id, out AccessibleNode node) ? node : null;

        public int ParentOf(int id)
            => _parents.TryGetValue(id, out int parent) ? parent : NONE;

        public IReadOnlyList<int> ChildrenOf(int id)
            => _children.TryGetValue(id, out List<int> children) ? children : NO_CHILDREN;

        public int ChildOf(int id, int index)
        {
            if (!_children.TryGetValue(id, out List<int> children) || children.Count == 0)
            {
                return NONE;
            }

            if (index < 0)
            {
                return children[children.Count - 1];
            }

            return index < children.Count ? children[index] : NONE;
        }

        public int SiblingOf(int id, int step)
        {
            int parent = ParentOf(id);

            if (parent == NONE || !_children.TryGetValue(parent, out List<int> children))
            {
                return NONE;
            }

            int at = children.IndexOf(id) + step;

            return at >= 0 && at < children.Count ? children[at] : NONE;
        }

        public int FocusedId()
        {
            for (int index = 0; index < _order.Count; index++)
            {
                AccessibleNode node = _nodes[_order[index]];

                if (node.HasState(AccessibleStates.Focused))
                {
                    return _order[index];
                }
            }

            return NONE;
        }

        public int FindAt(double x, double y) => RootId == NONE ? NONE : Deepest(RootId, x, y);

        private int Deepest(int id, double x, double y)
        {
            AccessibleNode node = NodeOf(id);

            if (node == null || x < node.X || y < node.Y
                || x >= node.X + node.Width || y >= node.Y + node.Height)
            {
                return NONE;
            }

            IReadOnlyList<int> children = ChildrenOf(id);

            for (int index = children.Count - 1; index >= 0; index--)
            {
                int hit = Deepest(children[index], x, y);

                if (hit != NONE)
                {
                    return hit;
                }
            }

            return id;
        }

        public static void Diff(AccessibilitySnapshot was, AccessibilitySnapshot now,
            List<AccessibilityChange> into)
        {
            if (was == null || now == null || into == null || was.RootId == NONE)
            {
                return;
            }

            for (int index = 0; index < now._order.Count; index++)
            {
                int id = now._order[index];

                if (!was._nodes.TryGetValue(id, out AccessibleNode before))
                {
                    continue;
                }

                Compare(id, before, now._nodes[id], into);
            }

            for (int index = 0; index < now._order.Count; index++)
            {
                int id = now._order[index];

                if (!was._children.TryGetValue(id, out List<int> children)
                    || Same(children, now._children[id]))
                {
                    continue;
                }

                into.Add(new AccessibilityChange
                {
                    Kind = AccessibilityChangeKind.Structure,
                    Id = id,
                    Previous = was._nodes[id],
                    Current = now._nodes[id]
                });
            }
        }

        private static void Compare(int id, AccessibleNode before, AccessibleNode after,
            List<AccessibilityChange> into)
        {
            bool spoke = false;

            if (before.Name != after.Name)
            {
                Add(AccessibilityChangeKind.Name, id, before, after, into);
                spoke = true;
            }

            if (before.Value != after.Value)
            {
                Add(AccessibilityChangeKind.Value, id, before, after, into);
                spoke = true;
            }

            if (spoke && after.Live != LiveRegionKind.None)
            {
                Add(AccessibilityChangeKind.LiveRegion, id, before, after, into);
            }

            if (before.HasState(AccessibleStates.Focused)
                != after.HasState(AccessibleStates.Focused))
            {
                Add(AccessibilityChangeKind.Focus, id, before, after, into);
            }
        }

        private static void Add(AccessibilityChangeKind kind, int id, AccessibleNode before,
            AccessibleNode after, List<AccessibilityChange> into)
        {
            into.Add(new AccessibilityChange
            {
                Kind = kind,
                Id = id,
                Previous = before,
                Current = after
            });
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
    }
}
