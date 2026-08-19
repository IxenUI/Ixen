using System.Collections.Generic;

namespace Ixen.Core.Language.Xnl
{
    internal static class XnlClasses
    {
        internal static List<string> Split(string value)
        {
            var names = new List<string>();

            if (string.IsNullOrEmpty(value))
            {
                return names;
            }

            int start = -1;

            for (int index = 0; index <= value.Length; index++)
            {
                bool separator = index == value.Length || value[index] == ' ' || value[index] == '\t';

                if (separator)
                {
                    if (start >= 0)
                    {
                        names.Add(value.Substring(start, index - start));
                        start = -1;
                    }

                    continue;
                }

                if (start < 0)
                {
                    start = index;
                }
            }

            return names;
        }
    }
}
