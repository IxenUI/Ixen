using Ixen.Core.Accessibility;
using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Ixen.Controls
{
    public class TreeView : VisualElement, IAccessibleSelection
    {
        public const string ROWS = "TreeRows";

        private const float DEFAULT_INDENT = 16;

        public event EventHandler<EventArgs> SelectionChanged;
        public event EventHandler<TreeMoveEventArgs> Moved;

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
            set => _list.Reorderable = value;
        }

        public Func<TreeMoveEventArgs, string> MoveAnnouncement { get; set; } = move => move.Describe();

        public bool MoveRow(int from, int gap)
        {
            TreeNode node = NodeAt(from);

            if (node == null || _roots == null || gap < 0 || gap > _flat.Count)
            {
                return false;
            }

            int fromParent = ParentIndexOf(from);
            IList fromList = ListOf(fromParent);
            int fromIndex = IndexIn(fromList, node.Item);

            int toParent;
            IList toList;
            int toIndex;

            if (gap == _flat.Count)
            {
                toParent = -1;
                toList = _roots;
                toIndex = _roots.Count;
            }
            else
            {
                toParent = ParentIndexOf(gap);
                toList = ListOf(toParent);
                toIndex = IndexIn(toList, _flat[gap].Item);
            }

            for (int ancestor = toParent; ancestor >= 0; ancestor = ParentIndexOf(ancestor))
            {
                if (ancestor == from)
                {
                    return false;
                }
            }

            if (ReferenceEquals(fromList, toList) && toIndex > fromIndex)
            {
                toIndex--;
            }

            return Raise(node.Item, fromParent, fromList, fromIndex, toParent, toList, toIndex);
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

            return Raise(node.Item, parent, list, at, parent, list, at + direction);
        }

        private bool Raise(object item, int fromParent, IList fromList, int fromIndex,
            int toParent, IList toList, int toIndex)
        {
            if (fromList == null || toList == null || fromIndex < 0 || toIndex < 0)
            {
                return false;
            }

            bool same = ReferenceEquals(fromList, toList);

            if ((same && (toIndex == fromIndex || toIndex >= fromList.Count)) || (!same && toIndex > toList.Count))
            {
                return false;
            }

            var move = new TreeMoveEventArgs(item, NodeAt(fromParent)?.Item, fromList, fromIndex,
                NodeAt(toParent)?.Item, toList, toIndex);

            string spoken = MoveAnnouncement?.Invoke(move);

            Moved?.Invoke(this, move);

            Rebuild();

            _list.Say(spoken);

            return true;
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
            int from = args.From;

            MoveRow(from, args.To > from ? args.To + 1 : args.To);
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
