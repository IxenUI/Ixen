using Ixen.Core.Accessibility;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Controls
{
    public class Spinner : VisualElement
    {
        public const string DOT = "SpinnerDot";

        public Spinner()
        {
            TypeName = nameof(Spinner);
            Role = AccessibleRole.ProgressBar;
            Label = "Busy";

            Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Absolute };

            AddChild(new VisualElement
            {
                TypeName = DOT,
                Role = AccessibleRole.Presentation
            });
        }
    }
}
