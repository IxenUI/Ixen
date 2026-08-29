namespace Ixen.Controls
{
    public class TreeNode
    {
        public object Item { get; internal set; }

        public int Depth { get; internal set; }

        public bool HasChildren { get; internal set; }

        public bool Expanded { get; internal set; }
    }
}
