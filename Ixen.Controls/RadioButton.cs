using Ixen.Core.Accessibility;
using Ixen.Core.Input;
using Ixen.Core.Visual;
using System.Collections.Generic;

namespace Ixen.Controls
{
    public class RadioButton : CheckBox
    {
        public RadioButton()
        {
            TypeName = nameof(RadioButton);
            Role = AccessibleRole.RadioButton;

            KeyDown += OnArrow;
        }

        private string _group;

        public string Group
        {
            get => _group;
            set
            {
                if (_group == value)
                {
                    return;
                }

                _group = value;

                Rove();
            }
        }

        protected override string MarkTypeName => "RadioButtonMark";

        protected override string MarkData => null;

        protected override void OnActivated()
        {
            if (Checked)
            {
                return;
            }

            Set(true);

            foreach (RadioButton other in Peers())
            {
                other.SetByUser(false);
            }
        }

        protected override void OnCheckedSet() => Rove();

        protected override void OnHostChanged()
        {
            base.OnHostChanged();

            if (Host != null)
            {
                Rove();
            }
        }

        private void Rove()
        {
            List<RadioButton> members = Members();

            if (members.Count < 2)
            {
                return;
            }

            RadioButton stop = members[0];

            foreach (RadioButton member in members)
            {
                if (member.Checked)
                {
                    stop = member;

                    break;
                }
            }

            foreach (RadioButton member in members)
            {
                member.TabIndex = member == stop ? 0 : -1;
            }
        }

        private void OnArrow(object sender, KeyEventArgs args)
        {
            int step;

            switch (args.Key)
            {
                case Key.Down:
                case Key.Right:
                    step = 1;
                    break;

                case Key.Up:
                case Key.Left:
                    step = -1;
                    break;

                default:
                    return;
            }

            List<RadioButton> members = Members();
            int at = members.IndexOf(this);

            if (at < 0 || members.Count < 2)
            {
                return;
            }

            args.Handled = true;

            for (int moved = 1; moved < members.Count; moved++)
            {
                RadioButton next = members[(at + step * moved + members.Count * moved) % members.Count];

                if (!next.IsEnabled)
                {
                    continue;
                }

                next.Focus();
                next.Activate();

                return;
            }
        }

        private List<RadioButton> Members()
        {
            var members = new List<RadioButton>();

            if (string.IsNullOrEmpty(Group))
            {
                return members;
            }

            VisualElement root = this;

            while (root.Parent != null)
            {
                root = root.Parent;
            }

            foreach (RadioButton found in Walk(root))
            {
                if (found.Group == Group)
                {
                    members.Add(found);
                }
            }

            return members;
        }

        private IEnumerable<RadioButton> Peers()
        {
            foreach (RadioButton member in Members())
            {
                if (member != this)
                {
                    yield return member;
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
