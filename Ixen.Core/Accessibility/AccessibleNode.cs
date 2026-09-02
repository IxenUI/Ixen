using Ixen.Core.Visual;
using System.Collections.Generic;

namespace Ixen.Core.Accessibility
{
    public class AccessibleNode
    {
        private readonly List<AccessibleNode> _children = new List<AccessibleNode>();

        public AccessibleRole Role { get; internal set; }
        public string Name { get; internal set; }
        public string Description { get; internal set; }
        public string Value { get; internal set; }
        public AccessibleStates States { get; internal set; }
        public LiveRegionKind Live { get; internal set; }
        public AccessibleActions Actions { get; internal set; }

        public float X { get; internal set; }
        public float Y { get; internal set; }
        public float Width { get; internal set; }
        public float Height { get; internal set; }

        public VisualElement Element { get; internal set; }

        public IReadOnlyList<AccessibleNode> Children => _children;

        internal List<AccessibleNode> ChildList => _children;

        public bool HasState(AccessibleStates state) => (States & state) == state;

        public bool Supports(AccessibleActions action) => (Actions & action) == action;
    }
}
