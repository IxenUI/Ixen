using Ixen.Core.Components;
using Ixen.Views;

namespace Ixen.AotApp
{
    public class MainComponent : Component<MainView>
    {
        private int _presses;

        public string Greeting
        {
            get
            {
                if (_presses == 0)
                {
                    return "Nothing has happened yet.";
                }

                return _presses == 1 ? "Pressed once." : $"Pressed {_presses} times.";
            }
        }

        public void Press()
        {
            SetState(() => _presses++);
        }
    }
}
