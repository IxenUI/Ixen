using System;
using System.Collections;
using System.Collections.Generic;

namespace Ixen.Core.Components
{
    public class ObservableList<T> : IList<T>, IObservableState
    {
        private readonly List<T> _items;

        public ObservableList()
        {
            _items = new List<T>();
        }

        public ObservableList(IEnumerable<T> items)
        {
            _items = items == null ? new List<T>() : new List<T>(items);
        }

        public event EventHandler Changed;

        public int Count => _items.Count;

        public bool IsReadOnly => false;

        public T this[int index]
        {
            get
            {
                return _items[index];
            }

            set
            {
                if (EqualityComparer<T>.Default.Equals(_items[index], value))
                {
                    return;
                }

                _items[index] = value;
                Announce();
            }
        }

        public void Add(T item)
        {
            _items.Add(item);
            Announce();
        }

        public void AddRange(IEnumerable<T> items)
        {
            if (items == null)
            {
                return;
            }

            int before = _items.Count;

            _items.AddRange(items);

            if (_items.Count == before)
            {
                return;
            }

            Announce();
        }

        public void Insert(int index, T item)
        {
            _items.Insert(index, item);
            Announce();
        }

        public bool Remove(T item)
        {
            if (!_items.Remove(item))
            {
                return false;
            }

            Announce();

            return true;
        }

        public void RemoveAt(int index)
        {
            _items.RemoveAt(index);
            Announce();
        }

        public void Clear()
        {
            if (_items.Count == 0)
            {
                return;
            }

            _items.Clear();
            Announce();
        }

        public bool Contains(T item)
        {
            return _items.Contains(item);
        }

        public int IndexOf(T item)
        {
            return _items.IndexOf(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _items.CopyTo(array, arrayIndex);
        }

        public List<T>.Enumerator GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        private void Announce()
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}
