using Ixen.Core.Components;
using Ixen.Views;
using System.Collections.Generic;

namespace Ixen.Core.UT.Components.Fixtures
{
    public class ArrayLoopComponent : Component<ArrayLoopView>
    {
        public string[] Names { get; set; } = { "swans", "lake" };

        public IReadOnlyList<ListItem> Items { get; set; } = new[] { new ListItem { Id = 1, Name = "one" } };

        public int ShoutedReads { get; private set; }

        public IEnumerable<string> Shouted
        {
            get
            {
                if (Names == null)
                {
                    yield break;
                }

                ShoutedReads++;

                foreach (string name in Names)
                {
                    yield return name.ToUpperInvariant();
                }
            }
        }

        internal void Update(System.Action change) => SetState(change);
    }
}
