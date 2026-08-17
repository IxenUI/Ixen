using System;
using System.Collections.Generic;

namespace Ixen.Core.Visual
{
    public static class Repeater
    {
        public static void Sync<TRow>(VisualElement parent, List<VisualElement> instances, List<TRow> rows,
            int offset, int count, Func<int, TRow> create)
            where TRow : IRegionRow
        {
            Trim(parent, instances, rows, offset, count);
            Ensure(parent, instances, rows, offset, count, create);
        }

        public static void Ensure<TRow>(VisualElement parent, List<VisualElement> instances, List<TRow> rows,
            int offset, int count, Func<int, TRow> create)
            where TRow : IRegionRow
        {
            if (parent == null || instances == null || rows == null || create == null || count < 0)
            {
                return;
            }

            while (rows.Count < count)
            {
                TRow row = create(rows.Count);

                if (row == null)
                {
                    return;
                }

                rows.Add(row);

                for (int k = 0; k < row.ElementCount; k++)
                {
                    VisualElement element = row.ElementAt(k);

                    parent.InsertChild(offset + instances.Count, element);
                    instances.Add(element);
                }
            }
        }

        public static void Trim<TRow>(VisualElement parent, List<VisualElement> instances, List<TRow> rows,
            int offset, int count)
            where TRow : IRegionRow
        {
            if (parent == null || instances == null || rows == null)
            {
                return;
            }

            int target = count < 0 ? 0 : count;

            while (rows.Count > target)
            {
                int last = rows.Count - 1;

                for (int k = 0; k < rows[last].ElementCount; k++)
                {
                    int index = instances.Count - 1;

                    parent.RemoveChildAt(offset + index);
                    instances.RemoveAt(index);
                }

                rows.RemoveAt(last);
            }
        }

        public static void SyncKeyed<TRow>(VisualElement parent, List<VisualElement> instances, List<TRow> rows,
            List<object> keys, List<object> next, int offset, Func<int, TRow> create)
            where TRow : IRegionRow
        {
            if (parent == null || instances == null || rows == null || keys == null || next == null
                || create == null)
            {
                return;
            }

            var available = new Dictionary<object, int>();

            for (int i = 0; i < keys.Count && i < rows.Count; i++)
            {
                if (keys[i] != null && !available.ContainsKey(keys[i]))
                {
                    available[keys[i]] = i;
                }
            }

            var orderedRows = new List<TRow>(next.Count);
            var ordered = new List<VisualElement>(instances.Count);
            var reused = new bool[rows.Count];

            for (int i = 0; i < next.Count; i++)
            {
                object key = next[i];

                if (key != null && available.TryGetValue(key, out int index) && !reused[index])
                {
                    reused[index] = true;
                    orderedRows.Add(rows[index]);

                    for (int k = 0; k < rows[index].ElementCount; k++)
                    {
                        ordered.Add(rows[index].ElementAt(k));
                    }

                    continue;
                }

                TRow row = create(i);

                if (row == null)
                {
                    continue;
                }

                orderedRows.Add(row);

                for (int k = 0; k < row.ElementCount; k++)
                {
                    VisualElement created = row.ElementAt(k);

                    parent.Adopt(created);
                    ordered.Add(created);
                }
            }

            for (int i = 0; i < reused.Length; i++)
            {
                if (reused[i])
                {
                    continue;
                }

                for (int k = 0; k < rows[i].ElementCount; k++)
                {
                    parent.Release(rows[i].ElementAt(k));
                }
            }

            parent.SpliceChildren(offset, instances.Count, ordered);

            instances.Clear();
            instances.AddRange(ordered);

            rows.Clear();
            rows.AddRange(orderedRows);

            keys.Clear();
            keys.AddRange(next);
        }
    }
}
