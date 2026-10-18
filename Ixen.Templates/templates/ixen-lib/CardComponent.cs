using Ixen.Core.Components;
using Ixen.Views;

namespace IxenLib
{
    public class CardComponent : Component<CardView>
    {
        public string Title { get; set; } = "Card";

        public string Body { get; set; } = "";
    }
}
