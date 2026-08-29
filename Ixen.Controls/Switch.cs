using Ixen.Core.Accessibility;

namespace Ixen.Controls
{
    public class Switch : CheckBox
    {
        public Switch()
        {
            TypeName = nameof(Switch);
            Role = AccessibleRole.Switch;
        }

        protected override string MarkTypeName => "SwitchKnob";
    }
}
