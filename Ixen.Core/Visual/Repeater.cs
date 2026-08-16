using System;
using System.Collections.Generic;

namespace Ixen.Core.Visual
{
    public static class Repeater
    {
        public static void SyncKeyed(VisualElement parent, List<VisualElement> instances, List<object> keys,
            List<object> next, int offset, Func<VisualElement> create)
        {
            if (parent == null || instances == null || keys == null || next == null || create == null)
            {
                return;
            }

            var available = new Dictionary<object, int>();

            for (int i = 0; i < keys.Count && i < instances.Count; i++)
            {
                if (keys[i] != null && !available.ContainsKey(keys[i]))
                {
                    available[keys[i]] = i;
                }
            }

            var ordered = new List<VisualElement>(next.Count);
            var reused = new bool[instances.Count];

            foreach (object key in next)
            {
                if (key != null && available.TryGetValue(key, out int index) && !reused[index])
                {
                    reused[index] = true;
                    ordered.Add(instances[index]);
                    continue;
                }

                VisualElement created = create();

                parent.Adopt(created);
                ordered.Add(created);
            }

            for (int i = 0; i < instances.Count; i++)
            {
                if (!reused[i])
                {
                    parent.Release(instances[i]);
                }
            }

            parent.SpliceChildren(offset, instances.Count, ordered);

            instances.Clear();
            instances.AddRange(ordered);

            keys.Clear();
            keys.AddRange(next);
        }


        public static void Sync(VisualElement parent, List<VisualElement> instances, int offset, int count,
            Func<VisualElement> create)
        {
            if (parent == null || instances == null || create == null)
            {
                return;
            }

            if (count < 0)
            {
                count = 0;
            }

            while (instances.Count > count)
            {
                int last = instances.Count - 1;

                parent.RemoveChildAt(offset + last);
                instances.RemoveAt(last);
            }

            while (instances.Count < count)
            {
                VisualElement element = create();

                parent.InsertChild(offset + instances.Count, element);
                instances.Add(element);
            }
        }
    }
}
