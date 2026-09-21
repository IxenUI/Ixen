using Ixen.Core.Components;
using Ixen.Views;
using System.Collections.Generic;

namespace Ixen.Core.UT.Components.Fixtures
{
    public class VirtualComponent : Component<VirtualView>
    {
        public List<string> Words { get; set; } = new List<string>();
        public List<List<string>> Groups { get; set; } = new List<List<string>>();
        public string Picked { get; private set; }
        public bool Flag { get; set; }

        public void Pick(string word) => SetState(() => Picked = word);

        internal void Refresh() => SetState();
    }
}
