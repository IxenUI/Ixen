using Ixen.Core.Accessibility;
using System;

namespace Ixen.Controls.UT
{
    internal static class AccessibleSearch
    {
        internal static AccessibleNode Find(AccessibleNode node, Func<AccessibleNode, bool> match)
        {
            if (match(node))
            {
                return node;
            }

            foreach (AccessibleNode child in node.Children)
            {
                AccessibleNode found = Find(child, match);

                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
