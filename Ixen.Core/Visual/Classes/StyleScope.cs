using System;
using System.Collections.Generic;

namespace Ixen.Core.Visual.Classes
{
    internal static class StyleScope
    {
        internal const string SEPARATOR = "/";

        internal static string Build<TNode>(TNode node, Func<TNode, TNode> parentOf, Func<TNode, string> nameOf)
            where TNode : class
        {
            var names = new List<string>();

            for (TNode current = parentOf(node); current != null; current = parentOf(current))
            {
                string name = nameOf(current);

                if (!string.IsNullOrEmpty(name))
                {
                    names.Add(name);
                }
            }

            if (names.Count == 0)
            {
                return null;
            }

            names.Reverse();

            return string.Join(SEPARATOR, names);
        }
    }
}
