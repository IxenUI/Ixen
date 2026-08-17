using System;

namespace Ixen.Core.Visual
{
    public class TransitionEventArgs : EventArgs
    {
        public string Property { get; private set; }
        public VisualElement Source { get; private set; }

        internal TransitionEventArgs(string property, VisualElement source)
        {
            Property = property;
            Source = source;
        }
    }
}
