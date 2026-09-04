using System;

namespace Ixen.Platform
{
    public enum IxenErrorPhase
    {
        Frame,
        Pointer,
        Keyboard,
        Timer,
        Posted
    }

    public class IxenErrorEventArgs : EventArgs
    {
        public Exception Error { get; private set; }
        public IxenErrorPhase Phase { get; private set; }

        public bool Handled { get; set; }

        internal IxenErrorEventArgs(IxenErrorPhase phase, Exception error)
        {
            Phase = phase;
            Error = error;
        }
    }
}
