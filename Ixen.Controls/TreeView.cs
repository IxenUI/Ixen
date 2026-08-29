using Ixen.Core.Accessibility;
using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Ixen.Controls
{
    public class TreeView : VisualElement
    {
        public const string ROWS = "TreeRows";

        private const float DEFAULT_INDENT = 16;

        public event EventHandler<EventArgs> SelectionChanged;

        private readonly VirtualList _list;
        private readonly List<TreeNode> _flat = new();
        private readonly HashSet<object> _open = new();

        private IList _roots;
        private Func<object, IList> _children;
        private Func<VisualElement> _create;
        private Action<VisualElement, TreeNode> _bind;

        private object _selectedItem;
        private int _selected = -1;

        public TreeView()
        {
            TypeName = nameof(TreeView);
            Focusable = true;
            Role = AccessibleRole.Tree;

            Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _list = new VirtualList { TypeName = ROWS };

            AddChild(_list);

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

        public int SelectedIndex => _selected;

        public TreeNode SelectedNode
            => _selected >= 0 && _selected < _flat.Count ? _flat[_selected] : null;

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

        public void Select(int index)
        {
            int clamped = index < 0 || index >= _flat.Count ? -1 : index;

            if (clamped == _selected)
            {
                return;
            }

            _selected = clamped;
            _selectedItem = clamped < 0 ? null : _flat[clamped].Item;

            _list.Refresh();

            if (clamped >= 0)
            {
                _list.ScrollIntoView(clamped);
            }

            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        private void Rebuild()
        {
            _flat.Clear();

            if (_roots != null)
            {
                Flatten(_roots, 0);
            }

            _selected = IndexOf(_selectedItem);

            if (_selected < 0)
            {
                _selectedItem = null;
            }

            _list.Refresh();
        }

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
            var row = new TreeRow();

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
            row.ToggleState(TreeRow.SELECTED, index == _selected);

            _bind(row.Content, node);
        }

        private void OnRowClick(object sender, PointerEventArgs args)
        {
            int index = _list.IndexOfRow(sender as VisualElement);

            if (index < 0)
            {
                return;
            }

            args.Handled = true;

            Select(index);
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

            switch (args.Key)
            {
                case Key.Down:
                    args.Handled = true;
                    Select(_selected + 1);
                    break;

                case Key.Up:
                    args.Handled = true;
                    Select(_selected <= 0 ? 0 : _selected - 1);
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
                        Toggle(_selected);
                    }

                    break;
            }
        }

        private void SelectParent(TreeNode node)
        {
            for (int i = _selected - 1; i >= 0; i--)
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
