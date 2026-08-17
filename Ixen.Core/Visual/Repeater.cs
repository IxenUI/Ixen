using System;
using System.Collections.Generic;

namespace Ixen.Core.Visual
{
    public static class Repeater
    {
        public static void SyncKeyed(VisualElement parent, List<VisualElement> instances, List<object> keys,
            List<object> next, int offset, int groupSize, Func<int, VisualElement> create)
        {
            if (parent == null || instances == null || keys == null || next == null || create == null
                || groupSize < 1)
            {
                return;
            }

            var available = new Dictionary<object, int>();

            for (int i = 0; i < keys.Count && (i + 1) * groupSize <= instances.Count; i++)
            {
                if (keys[i] != null && !available.ContainsKey(keys[i]))
                {
                    available[keys[i]] = i;
                }
            }

            var ordered = new List<VisualElement>(next.Count * groupSize);
            var reused = new bool[keys.Count];

            foreach (object key in next)
            {
                if (key != null && available.TryGetValue(key, out int group) && !reused[group])
                {
                    reused[group] = true;

                    for (int k = 0; k < groupSize; k++)
                    {
                        ordered.Add(instances[group * groupSize + k]);
                    }

                    continue;
                }

                for (int k = 0; k < groupSize; k++)
                {
                    VisualElement created = create(k);

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

                for (int k = 0; k < groupSize; k++)
                {
                    int index = i * groupSize + k;

                    if (index < instances.Count)
                    {
                        parent.Release(instances[index]);
                    }
                }
            }

            parent.SpliceChildren(offset, instances.Count, ordered);

            instances.Clear();
            instances.AddRange(ordered);

            keys.Clear();
            keys.AddRange(next);
        }

        public static void Sync(VisualElement parent, List<VisualElement> instances, int offset, int count,
            int groupSize, Func<int, VisualElement> create)
        {
            Trim(parent, instances, offset, count, groupSize);
            Ensure(parent, instances, offset, count, groupSize, create);
        }

        public static void Ensure(VisualElement parent, List<VisualElement> instances, int offset, int count,
            int groupSize, Func<int, VisualElement> create)
        {
            if (parent == null || instances == null || create == null || groupSize < 1 || count < 0)
            {
                return;
            }

            int target = count * groupSize;

            while (instances.Count < target)
            {
                VisualElement element = create(instances.Count % groupSize);

                parent.InsertChild(offset + instances.Count, element);
                instances.Add(element);
            }
        }

        public static void Trim(VisualElement parent, List<VisualElement> instances, int offset, int count,
            int groupSize)
        {
            if (parent == null || instances == null || groupSize < 1)
            {
                return;
            }

            int target = count < 0 ? 0 : count * groupSize;

            while (instances.Count > target)
            {
                int last = instances.Count - 1;

                parent.RemoveChildAt(offset + last);
                instances.RemoveAt(last);
            }
        }
    }
}
