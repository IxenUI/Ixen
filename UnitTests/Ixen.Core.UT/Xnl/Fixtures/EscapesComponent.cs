using Ixen.Core.Components;
using Ixen.Views;

namespace Ixen.Core.UT.Xnl.Fixtures
{
    public class EscapesComponent : Component<EscapesView>
    {
        public int Count { get; set; } = 3;

        public string Label(string value) => "[" + value + "]";
    }
}
