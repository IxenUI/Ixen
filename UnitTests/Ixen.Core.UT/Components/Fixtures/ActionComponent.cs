using Ixen.Core.Components;
using Ixen.Views;

namespace Ixen.Core.UT.Components.Fixtures
{
    public class ActionComponent : Component<ActionView>
    {
        public string Caption { get; set; } = "click me";
        public int Count { get; set; }
        public float LastX { get; set; }
        public float LastY { get; set; }
        public bool Captured { get; set; }

        public void Increment() => SetState(() => Count++);

        public void Add(int amount) => SetState(() => Count += amount);

        public void Reset() => SetState(() => Count = 0);

        public void Track(float x, float y)
        {
            LastX = x;
            LastY = y;
        }

        public void Capture() => Captured = true;
    }
}
