using Ixen.Core.Accessibility;
using Ixen.Core.Visual;

namespace Ixen.Controls
{
    public class Spinner : VisualElement
    {
        public Spinner()
        {
            TypeName = nameof(Spinner);
            Role = AccessibleRole.ProgressBar;
            Label = "Busy";
        }
    }
}
