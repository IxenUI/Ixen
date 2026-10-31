using Ixen.Core.Input;
using System;
using System.Collections.Generic;

namespace Ixen.Controls
{
    internal sealed class ItemSelection
    {
        private readonly Func<int> _count;
        private readonly Func<int, object> _itemAt;
        private readonly Action<int> _moved;
        private readonly HashSet<object> _chosen = new HashSet<object>(ReferenceComparer.Instance);

        private object _anchor;
        private object _currentItem;
        private int _current = -1;
        private bool _multiple;

        public ItemSelection(Func<int> count, Func<int, object> itemAt, Action<int> moved)
        {
            _count = count;
            _itemAt = itemAt;
            _moved = moved;
        }

        public int Current => _current;

        public object CurrentItem => _currentItem;

        public bool Multiple
        {
            get => _multiple;
            set
            {
                if (_multiple == value)
                {
                    return;
                }

                _multiple = value;

                if (!value && _chosen.Count > 1)
                {
                    Select(_current);
                }
            }
        }

        public bool Contains(object item) => item != null && _chosen.Contains(item);

        public IReadOnlyList<object> Items
        {
            get
            {
                var items = new List<object>();
                int count = _count();

                for (int i = 0; i < count; i++)
                {
                    object item = _itemAt(i);

                    if (_chosen.Contains(item))
                    {
                        items.Add(item);
                    }
                }

                return items;
            }
        }

        public void Select(int index)
        {
            int clamped = index < 0 || index >= _count() ? -1 : index;
            object item = clamped < 0 ? null : _itemAt(clamped);

            if (clamped == _current && (_chosen.Count == 0 || (_chosen.Count == 1 && _chosen.Contains(item))))
            {
                return;
            }

            _chosen.Clear();

            if (item != null)
            {
                _chosen.Add(item);
            }

            _anchor = item;
            Move(clamped);
        }

        public void Toggle(int index)
        {
            if (!_multiple)
            {
                Select(index);
                return;
            }

            if (index < 0 || index >= _count())
            {
                return;
            }

            object item = _itemAt(index);

            if (!_chosen.Remove(item))
            {
                _chosen.Add(item);
            }

            _anchor = item;
            Move(index);
        }

        public void Range(int index)
        {
            int from = IndexOf(_anchor);

            if (!_multiple || from < 0)
            {
                Select(index);
                return;
            }

            if (index < 0 || index >= _count())
            {
                return;
            }

            int low = Math.Min(from, index);
            int high = Math.Max(from, index);

            _chosen.Clear();

            for (int i = low; i <= high; i++)
            {
                _chosen.Add(_itemAt(i));
            }

            Move(index);
        }

        public void All()
        {
            int count = _count();

            if (!_multiple || count == 0)
            {
                return;
            }

            _chosen.Clear();

            for (int i = 0; i < count; i++)
            {
                _chosen.Add(_itemAt(i));
            }

            Move(_current < 0 ? 0 : _current);
        }

        public bool Add(int index)
        {
            if (!_multiple || index < 0 || index >= _count() || _chosen.Contains(_itemAt(index)))
            {
                return false;
            }

            Toggle(index);
            return true;
        }

        public bool Remove(int index)
        {
            if (!_multiple || index < 0 || index >= _count() || !_chosen.Contains(_itemAt(index)))
            {
                return false;
            }

            Toggle(index);
            return true;
        }

        public void Click(int index, KeyModifiers modifiers, KeyModifiers accelerator)
        {
            if (_multiple && (modifiers & accelerator) != 0)
            {
                Toggle(index);
            }
            else if (_multiple && (modifiers & KeyModifiers.Shift) != 0)
            {
                Range(index);
            }
            else
            {
                Select(index);
            }
        }

        public void Walk(int index, KeyModifiers modifiers)
        {
            if (_multiple && (modifiers & KeyModifiers.Shift) != 0)
            {
                if (_anchor == null)
                {
                    _anchor = _currentItem ?? (index >= 0 && index < _count() ? _itemAt(index) : null);
                }

                Range(index);
                return;
            }

            Select(index);
        }

        public void Rebuilt()
        {
            int count = _count();
            var present = new HashSet<object>(ReferenceComparer.Instance);

            _current = -1;

            for (int i = 0; i < count; i++)
            {
                object item = _itemAt(i);

                present.Add(item);

                if (_current < 0 && ReferenceEquals(item, _currentItem))
                {
                    _current = i;
                }
            }

            if (_current < 0)
            {
                _currentItem = null;
            }

            if (_chosen.Count > 0)
            {
                _chosen.RemoveWhere(item => !present.Contains(item));
            }

            if (_anchor != null && !present.Contains(_anchor))
            {
                _anchor = _currentItem;
            }
        }

        private int IndexOf(object item)
        {
            if (item == null)
            {
                return -1;
            }

            int count = _count();

            for (int i = 0; i < count; i++)
            {
                if (ReferenceEquals(_itemAt(i), item))
                {
                    return i;
                }
            }

            return -1;
        }

        private void Move(int index)
        {
            _current = index;
            _currentItem = index < 0 ? null : _itemAt(index);
            _moved(index);
        }
    }
}
