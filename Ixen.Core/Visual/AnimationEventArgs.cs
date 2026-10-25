using System;

namespace Ixen.Core.Visual
{
    public class AnimationEventArgs : EventArgs
    {
        public string Name { get; private set; }
        public VisualElement Source { get; private set; }

        internal AnimationEventArgs(string name, VisualElement source)
        {
            Name = name;
            Source = source;
        }
    }
}
