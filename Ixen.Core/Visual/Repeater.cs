using System;
using System.Collections.Generic;

namespace Ixen.Core.Visual
{
    public static class Repeater
    {
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
