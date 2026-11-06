using Ixen.Core.Components;
using System;
using System.Collections.Generic;

namespace Ixen.Controls
{
    public class ReorderEventArgs : EventArgs
    {
        public ReorderEventArgs(int from, int to)
            : this(new[] { from }, to)
        { }

        public ReorderEventArgs(IReadOnlyList<int> indices, int to)
        {
            var sorted = new List<int>(indices);

            sorted.Sort();

            Indices = sorted;
            To = to;
        }

        public IReadOnlyList<int> Indices { get; }

        public int From => Indices[0];

        public int To { get; }

        public int Count => Indices.Count;

        internal static int Landing(IReadOnlyList<int> sorted, int gap)
        {
            int before = 0;

            foreach (int index in sorted)
            {
                if (index < gap)
                {
                    before++;
                }
            }

            return gap - before;
        }

        internal static bool Changes(IReadOnlyList<int> sorted, int to)
        {
            for (int i = 0; i < sorted.Count; i++)
            {
                if (sorted[i] != to + i)
                {
                    return true;
                }
            }

            return false;
        }

        public void Apply<T>(IList<T> items)
        {
            if (items is ObservableList<T> observable)
            {
                observable.Move(Indices, To);

                return;
            }

            var moved = new List<T>(Indices.Count);

            foreach (int index in Indices)
            {
                moved.Add(items[index]);
            }

            for (int i = Indices.Count - 1; i >= 0; i--)
            {
                items.RemoveAt(Indices[i]);
            }

            for (int i = 0; i < moved.Count; i++)
            {
                items.Insert(To + i, moved[i]);
            }
        }
    }
}
