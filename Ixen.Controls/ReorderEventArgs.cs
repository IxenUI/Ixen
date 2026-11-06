using Ixen.Core.Components;
using System;
using System.Collections.Generic;

namespace Ixen.Controls
{
    public class ReorderEventArgs : EventArgs
    {
        public ReorderEventArgs(int from, int to)
        {
            From = from;
            To = to;
        }

        public int From { get; }

        public int To { get; }

        public void Apply<T>(IList<T> items)
        {
            if (items is ObservableList<T> observable)
            {
                observable.Move(From, To);

                return;
            }

            T item = items[From];

            items.RemoveAt(From);
            items.Insert(To, item);
        }
    }
}
