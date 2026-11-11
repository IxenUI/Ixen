using Ixen.Core.Components;
using Ixen.Views;
using System.Collections.Generic;

namespace Ixen.Core.UT.Components.Fixtures
{
    public class ShelfComponent : Component<ShelfView>
    {
        public string Featured { get; set; } = "Blue";

        public string Tint { get; set; } = "warm";

        public bool ShowPicked { get; set; }

        public string Picked { get; set; } = "Green";

        public List<string> Albums { get; } = new List<string> { "Red", "Gold" };

        internal void Update(System.Action change) => SetState(change);
    }

    public class FeaturedShelfComponent : ShelfComponent
    {
        public FeaturedShelfComponent()
        {
            Featured = "Silver";
        }
    }

    public class ShelfCardComponent : Component<ShelfCardView>
    {
        public string Album { get; set; }

        public string SeenWhenInitialized { get; private set; }

        public int Renders { get; private set; }

        protected override void OnInitialized() => SeenWhenInitialized = Album;

        protected override void Render() => Renders++;
    }
}
