using Ixen.Core.Accessibility;
using Ixen.Core.Visual;
using System.Collections.Generic;

namespace Ixen.Controls
{
    public class RadioButton : CheckBox
    {
        private const string DOT = "\u25CF";

        public RadioButton()
        {
            TypeName = nameof(RadioButton);
            Role = AccessibleRole.RadioButton;
        }

        public string Group { get; set; }

        protected override string Glyph => DOT;

        protected override void OnActivated()
        {
            if (Checked)
            {
                return;
            }

            Set(true);

            foreach (RadioButton other in Peers())
            {
                other.Set(false);
            }
        }

        private IEnumerable<RadioButton> Peers()
        {
            if (string.IsNullOrEmpty(Group))
            {
                yield break;
            }

            VisualElement root = this;

            while (root.Parent != null)
            {
                root = root.Parent;
            }

            foreach (RadioButton found in Walk(root))
            {
                if (found != this && found.Group == Group)
                {
                    yield return found;
                }
            }
        }

        private static IEnumerable<RadioButton> Walk(VisualElement element)
        {
            if (element is RadioButton radio)
            {
                yield return radio;
            }

            foreach (VisualElement child in element.ChildElements)
            {
                foreach (RadioButton found in Walk(child))
                {
                    yield return found;
                }
            }
        }
    }
}
