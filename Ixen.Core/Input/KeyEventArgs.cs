using Ixen.Core.Visual;
using System;

namespace Ixen.Core.Input
{
    public class KeyEventArgs : EventArgs
    {
        public Key Key { get; private set; }
        public KeyModifiers Modifiers { get; private set; }
        public VisualElement Source { get; private set; }

        public bool Handled { get; set; }

        public bool HasModifier(KeyModifiers modifier)
            => (Modifiers & modifier) == modifier;

        internal KeyEventArgs(Key key, KeyModifiers modifiers, VisualElement source)
        {
            Key = key;
            Modifiers = modifiers;
            Source = source;
        }
    }
}
