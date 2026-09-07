using Ixen.Core.Visual;
using System.Collections.Generic;

namespace Ixen.Core.Accessibility
{
    public sealed class AccessibilityIds
    {
        private readonly Dictionary<VisualElement, int> _ids
            = new Dictionary<VisualElement, int>();

        private readonly List<VisualElement> _gone = new List<VisualElement>();

        private int _next;

        public int Count => _ids.Count;

        public int IdOf(VisualElement element)
        {
            if (element == null)
            {
                return _next++;
            }

            if (!_ids.TryGetValue(element, out int id))
            {
                id = _next++;
                _ids[element] = id;
            }

            return id;
        }

        internal void Prune(HashSet<VisualElement> seen, List<int> dropped)
        {
            _gone.Clear();

            foreach (KeyValuePair<VisualElement, int> entry in _ids)
            {
                if (seen.Contains(entry.Key))
                {
                    continue;
                }

                _gone.Add(entry.Key);

                if (dropped != null)
                {
                    dropped.Add(entry.Value);
                }
            }

            for (int index = 0; index < _gone.Count; index++)
            {
                _ids.Remove(_gone[index]);
            }
        }
    }
}
