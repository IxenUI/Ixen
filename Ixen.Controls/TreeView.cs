using Ixen.Core.Accessibility;
using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Ixen.Controls
{
    public class TreeView : VisualElement, IAccessibleSelection, IAccessibleInvoker
    {
        public const string ROWS = "TreeRows";

        private const float DEFAULT_INDENT = 16;

        public event EventHandler<EventArgs> SelectionChanged;
        public event EventHandler<TreeMoveEventArgs> Moved;
        public event EventHandler<TreeDropEventArgs> DroppedOnto;
        public event EventHandler<ItemInvokedEventArgs> ItemInvoked;

        private bool Invoke(int index)
        {
            TreeNode node = NodeAt(index);

            if (ItemInvoked == null || node == null || node.HasChildren || !IsEnabled)
            {
                return false;
            }

            ItemInvoked(this, new ItemInvokedEventArgs(node.Item, index));

            return true;
        }

        bool IAccessibleInvoker.CanInvoke(VisualElement item)
        {
            if (ItemInvoked == null || !(item is TreeRow row))
            {
                return false;
            }

            TreeNode node = NodeAt(IndexOf(row.Item));

            return node != null && !node.HasChildren;
        }

        bool IAccessibleInvoker.Invoke(VisualElement item)
            => item is TreeRow row && Invoke(IndexOf(row.Item));

        private Func<object, object, bool> _acceptsOnto;

        private readonly VirtualList _list;
        private readonly List<TreeNode> _flat = new();
        private readonly HashSet<object> _open = new();

        private IList _roots;
        private Func<object, IList> _children;
        private Func<VisualElement> _create;
        private Action<VisualElement, TreeNode> _bind;

        private readonly ItemSelection _selection;

        public TreeView()
        {
            TypeName = nameof(TreeView);
            Focusable = true;
            Role = AccessibleRole.Tree;

            Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _list = new VirtualList { TypeName = ROWS, Role = AccessibleRole.None, AnnouncesMoves = false };
            _selection = new ItemSelection(() => _flat.Count, index => _flat[index].Item, OnSelectionMoved);

            AddChild(_list);

            _list.Reordered += OnRowsReordered;
            _list.DroppedOnto += OnRowsDroppedOnto;
            _list.ReorderGroup = GroupOf;

            KeyDown += OnKeyDown;
        }

        public VirtualList Rows => _list;

        public float ItemHeight
        {
            get => _list.ItemHeight;
            set => _list.ItemHeight = value;
        }

        public float Indent { get; set; } = DEFAULT_INDENT;

        public int Count => _flat.Count;

        public int SelectedIndex => _selection.Current;

        public TreeNode SelectedNode => NodeAt(_selection.Current);

        public bool MultiSelect
        {
            get => _selection.Multiple;
            set => _selection.Multiple = value;
        }

        public IReadOnlyList<object> SelectedItems => _selection.Items;

        public bool IsSelected(object item) => _selection.Contains(item);

        public void ToggleSelection(int index) => _selection.Toggle(index);

        public void SelectRange(int index) => _selection.Range(index);

        public void SelectAll() => _selection.All();

        public TreeNode NodeAt(int index)
            => index >= 0 && index < _flat.Count ? _flat[index] : null;

        public void SetRoots(IList roots, Func<object, IList> children,
            Func<VisualElement> create, Action<VisualElement, TreeNode> bind)
        {
            _roots = roots;
            _children = children;
            _create = create;
            _bind = bind;

            _list.SetItems(_flat, CreateRow, BindRow);

            Rebuild();
        }

        public void Refresh() => Rebuild();

        public bool IsExpanded(object item) => _open.Contains(item);

        public void Expand(object item)
        {
            if (item == null || !_open.Add(item))
            {
                return;
            }

            Rebuild();
        }

        public void Collapse(object item)
        {
            if (item == null || !_open.Remove(item))
            {
                return;
            }

            Rebuild();
        }

        internal void SetExpanded(TreeRow row, bool expanded)
        {
            TreeNode node = NodeAt(_list.IndexOfRow(row));

            if (node == null || !node.HasChildren)
            {
                return;
            }

            if (expanded)
            {
                Expand(node.Item);
            }
            else
            {
                Collapse(node.Item);
            }

            row.SetBranch(true, IsExpanded(node.Item));
        }

        public void Toggle(int index)
        {
            TreeNode node = NodeAt(index);

            if (node == null || !node.HasChildren)
            {
                return;
            }

            if (node.Expanded)
            {
                Collapse(node.Item);
            }
            else
            {
                Expand(node.Item);
            }
        }

        public void Select(int index) => _selection.Select(index);

        public bool Reorderable
        {
            get => _list.Reorderable;
            set
            {
                _list.Reorderable = value;
                SyncOnto();
            }
        }

        public Func<object, object, bool> AcceptsOnto
        {
            get => _acceptsOnto;
            set
            {
                _acceptsOnto = value;
                SyncOnto();
            }
        }

        private void SyncOnto()
        {
            _list.AcceptsOnto = Reorderable || _acceptsOnto != null ? OntoRow : (Func<object, int, bool>)null;
        }

        private bool OntoRow(object data, int index)
        {
            TreeNode node = NodeAt(index);

            if (node == null)
            {
                return false;
            }

            if (_list.IsOwnDrag(data))
            {
                return CanNest(Normalize(_list.DraggedRows), index);
            }

            return _acceptsOnto != null && _acceptsOnto(data, node.Item);
        }

        private bool CanNest(List<int> group, int into)
        {
            return group != null && into >= 0 && into < _flat.Count
                && _children?.Invoke(_flat[into].Item) != null && !Inside(into, group);
        }

        public bool MoveInto(int from, int into) => MoveRowsInto(new[] { from }, into);

        public bool MoveRowsInto(IReadOnlyList<int> rows, int into)
        {
            List<int> group = Normalize(rows);

            if (!CanNest(group, into))
            {
                return false;
            }

            return RaiseBlock(group, into, _children(_flat[into].Item), null);
        }

        private void OnRowsDroppedOnto(object sender, ListDropEventArgs args)
        {
            if (_list.DroppedRows != null)
            {
                MoveRowsInto(_list.DroppedRows, args.Index);

                return;
            }

            TreeNode node = NodeAt(args.Index);

            if (node != null)
            {
                DroppedOnto?.Invoke(this, new TreeDropEventArgs(args.Data, node.Item));
            }
        }

        public Func<TreeMoveEventArgs, string> MoveAnnouncement { get; set; } = move => move.Describe();

        public bool MoveRow(int from, int gap) => MoveRows(new[] { from }, gap);

        public bool MoveRows(IReadOnlyList<int> rows, int gap)
        {
            List<int> group = Normalize(rows);

            if (group == null || _roots == null || gap < 0 || gap > _flat.Count)
            {
                return false;
            }

            if (gap == _flat.Count)
            {
                return RaiseBlock(group, -1, _roots, null);
            }

            int toParent = ParentIndexOf(gap);

            if (Inside(toParent, group))
            {
                return false;
            }

            return RaiseBlock(group, toParent, ListOf(toParent), _flat[gap].Item);
        }

        private List<int> Normalize(IReadOnlyList<int> rows)
        {
            if (rows == null)
            {
                return null;
            }

            var group = new List<int>();

            foreach (int row in rows)
            {
                if (row >= 0 && row < _flat.Count && !group.Contains(row))
                {
                    group.Add(row);
                }
            }

            group.Sort();
            group.RemoveAll(row => Inside(ParentIndexOf(row), group));

            return group.Count == 0 ? null : group;
        }

        private bool Inside(int row, List<int> group)
        {
            for (int ancestor = row; ancestor >= 0; ancestor = ParentIndexOf(ancestor))
            {
                if (group.BinarySearch(ancestor) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private bool MoveAmongSiblings(int index, int direction)
        {
            TreeNode node = NodeAt(index);

            if (node == null || !Reorderable)
            {
                return false;
            }

            int parent = ParentIndexOf(index);
            IList list = ListOf(parent);
            int at = IndexIn(list, node.Item);

            if (at < 0 || at + direction < 0 || at + direction >= list.Count)
            {
                return false;
            }

            object anchor = direction < 0 ? list[at - 1] : (at + 2 < list.Count ? list[at + 2] : null);

            return RaiseBlock(new List<int> { index }, parent, list, anchor);
        }

        private bool RaiseBlock(List<int> group, int toParent, IList toList, object anchor)
        {
            if (toList == null)
            {
                return false;
            }

            int count = group.Count;
            var items = new object[count];
            var fromParents = new object[count];
            var fromLists = new IList[count];
            var fromIndices = new int[count];

            for (int i = 0; i < count; i++)
            {
                int parent = ParentIndexOf(group[i]);

                items[i] = _flat[group[i]].Item;
                fromParents[i] = NodeAt(parent)?.Item;
                fromLists[i] = ListOf(parent);
                fromIndices[i] = IndexIn(fromLists[i], items[i]);

                if (fromLists[i] == null || fromIndices[i] < 0)
                {
                    return false;
                }
            }

            int toIndex = 0;
            bool found = anchor == null;
            var kept = new List<object>();

            foreach (object sibling in toList)
            {
                if (anchor != null && ReferenceEquals(sibling, anchor))
                {
                    found = true;
                    toIndex = kept.Count;
                }

                if (!Holds(items, sibling))
                {
                    kept.Add(sibling);
                }
            }

            if (!found)
            {
                return false;
            }

            if (anchor == null)
            {
                toIndex = kept.Count;
            }

            if (Unchanged(toList, kept, items, toIndex))
            {
                return false;
            }

            var move = new TreeMoveEventArgs(items, fromParents, fromLists, fromIndices,
                NodeAt(toParent)?.Item, toList, toIndex);

            string spoken = MoveAnnouncement?.Invoke(move);

            Moved?.Invoke(this, move);

            Rebuild();

            _list.Say(spoken);

            return true;
        }

        private static bool Holds(object[] items, object item)
        {
            foreach (object held in items)
            {
                if (ReferenceEquals(held, item))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool Unchanged(IList list, List<object> kept, object[] items, int at)
        {
            if (kept.Count + items.Length != list.Count)
            {
                return false;
            }

            for (int i = 0; i < list.Count; i++)
            {
                object after = i < at ? kept[i] : (i < at + items.Length ? items[i - at] : kept[i - items.Length]);

                if (!ReferenceEquals(list[i], after))
                {
                    return false;
                }
            }

            return true;
        }

        private IReadOnlyList<int> GroupOf(int index)
        {
            TreeNode node = NodeAt(index);

            if (node == null || !_selection.Contains(node.Item))
            {
                return new[] { index };
            }

            var group = new List<int>();

            for (int i = 0; i < _flat.Count; i++)
            {
                if (_selection.Contains(_flat[i].Item))
                {
                    group.Add(i);
                }
            }

            return group;
        }

        private int ParentIndexOf(int index)
        {
            TreeNode node = NodeAt(index);

            if (node == null)
            {
                return -1;
            }

            for (int i = index - 1; i >= 0; i--)
            {
                if (_flat[i].Depth < node.Depth)
                {
                    return i;
                }
            }

            return -1;
        }

        private IList ListOf(int parent) => parent < 0 ? _roots : _children?.Invoke(_flat[parent].Item);

        private static int IndexIn(IList list, object item)
        {
            if (list == null)
            {
                return -1;
            }

            for (int i = 0; i < list.Count; i++)
            {
                if (ReferenceEquals(list[i], item))
                {
                    return i;
                }
            }

            return -1;
        }

        private void OnRowsReordered(object sender, ReorderEventArgs args)
        {
            if (_list.DroppedGap >= 0)
            {
                MoveRows(args.Indices, _list.DroppedGap);
            }
        }

        private void OnSelectionMoved(int index)
        {
            _list.Refresh();

            if (index >= 0)
            {
                _list.ScrollIntoView(index);
            }

            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        bool IAccessibleSelection.CanSelectMultiple => _selection.Multiple;

        bool IAccessibleSelection.IsSelectable(VisualElement item) => item is TreeRow;

        bool IAccessibleSelection.AddToSelection(VisualElement item)
            => _selection.Add(IndexOf((item as TreeRow)?.Item));

        bool IAccessibleSelection.RemoveFromSelection(VisualElement item)
            => _selection.Remove(IndexOf((item as TreeRow)?.Item));

        private int IndexOf(object item)
        {
            if (item == null)
            {
                return -1;
            }

            for (int i = 0; i < _flat.Count; i++)
            {
                if (ReferenceEquals(_flat[i].Item, item))
                {
                    return i;
                }
            }

            return -1;
        }

        private void Rebuild()
        {
            _flat.Clear();

            if (_roots != null)
            {
                Flatten(_roots, 0);
            }

            _selection.Rebuilt();

            _list.Refresh();
        }

        private void Flatten(IList items, int depth)
        {
            foreach (object item in items)
            {
                IList children = _children?.Invoke(item);
                bool has = children != null && children.Count > 0;
                bool expanded = has && _open.Contains(item);

                _flat.Add(new TreeNode
                {
                    Item = item,
                    Depth = depth,
                    HasChildren = has,
                    Expanded = expanded
                });

                if (expanded)
                {
                    Flatten(children, depth + 1);
                }
            }
        }

        private VisualElement CreateRow()
        {
            var row = new TreeRow { Owner = this };

            row.SetContent(_create());

            row.PointerClick += OnRowClick;
            row.PointerDoubleClick += OnRowDoubleClick;
            row.Expander.PointerClick += OnExpanderClick;

            return row;
        }

        private void BindRow(VisualElement element, int index)
        {
            var row = (TreeRow)element;
            TreeNode node = _flat[index];

            row.SetDepth(node.Depth * Indent);
            row.SetBranch(node.HasChildren, node.Expanded);
            row.Item = node.Item;
            row.ToggleState(TreeRow.SELECTED, _selection.Contains(node.Item));

            _bind(row.Content, node);
        }

        private void OnRowDoubleClick(object sender, PointerEventArgs args)
        {
            if (args.Source is VisualElement source && sender is TreeRow row && IsExpander(source, row))
            {
                return;
            }

            if (Invoke(IndexOf((sender as TreeRow)?.Item)))
            {
                args.Handled = true;
            }
        }

        private static bool IsExpander(VisualElement source, TreeRow row)
        {
            for (VisualElement element = source; element != null && element != row; element = element.Parent)
            {
                if (element == row.Expander)
                {
                    return true;
                }
            }

            return false;
        }

        private void OnRowClick(object sender, PointerEventArgs args)
        {
            int index = IndexOf((sender as TreeRow)?.Item);

            if (index < 0)
            {
                return;
            }

            args.Handled = true;

            _selection.Click(index, args.Modifiers, Host?.AcceleratorModifier ?? KeyModifiers.Control);
            Focus();
        }

        private void OnExpanderClick(object sender, PointerEventArgs args)
        {
            var expander = sender as VisualElement;
            int index = _list.IndexOfRow(expander?.Parent);

            if (index < 0)
            {
                return;
            }

            args.Handled = true;

            Toggle(index);
            Focus();
        }

        private void OnKeyDown(object sender, KeyEventArgs args)
        {
            TreeNode node = SelectedNode;

            if ((args.Key == Key.Up || args.Key == Key.Down) && (args.Modifiers & KeyModifiers.Alt) != 0)
            {
                if (Reorderable)
                {
                    args.Handled = true;
                    MoveAmongSiblings(_selection.Current, args.Key == Key.Up ? -1 : 1);
                }

                return;
            }

            switch (args.Key)
            {
                case Key.Down:
                    args.Handled = true;
                    _selection.Walk(Math.Min(_selection.Current + 1, _flat.Count - 1), args.Modifiers);
                    break;

                case Key.Up:
                    args.Handled = true;
                    _selection.Walk(_selection.Current <= 0 ? 0 : _selection.Current - 1, args.Modifiers);
                    break;

                case Key.Home:
                    args.Handled = true;
                    _selection.Walk(0, args.Modifiers);
                    break;

                case Key.End:
                    args.Handled = true;
                    _selection.Walk(_flat.Count - 1, args.Modifiers);
                    break;

                case Key.A:
                    if (_selection.Multiple && (args.Modifiers & (Host?.AcceleratorModifier ?? KeyModifiers.Control)) != 0)
                    {
                        args.Handled = true;
                        SelectAll();
                    }

                    break;

                case Key.Right:
                    if (node != null && node.HasChildren && !node.Expanded)
                    {
                        args.Handled = true;
                        Expand(node.Item);
                    }

                    break;

                case Key.Left:
                    if (node == null)
                    {
                        break;
                    }

                    args.Handled = true;

                    if (node.Expanded)
                    {
                        Collapse(node.Item);
                    }
                    else
                    {
                        SelectParent(node);
                    }

                    break;

                case Key.Enter:
                case Key.Space:
                    if (args.Key == Key.Enter && args.Modifiers == KeyModifiers.None && Invoke(_selection.Current))
                    {
                        args.Handled = true;
                        break;
                    }

                    if (node != null && node.HasChildren)
                    {
                        args.Handled = true;
                        Toggle(_selection.Current);
                    }

                    break;
            }
        }

        private void SelectParent(TreeNode node)
        {
            for (int i = _selection.Current - 1; i >= 0; i--)
            {
                if (_flat[i].Depth < node.Depth)
                {
                    Select(i);

                    return;
                }
            }
        }
    }
}
