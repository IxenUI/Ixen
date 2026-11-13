using System.Collections.Generic;
using System.Text;

namespace Ixen.Core
{
    public sealed class FilePickerOptions
    {
        public string Title { get; set; }

        public bool Multiple { get; set; }

        public IList<FileFilter> Filters { get; } = new List<FileFilter>();

        public string StartFolder { get; set; }

        public string SuggestedName { get; set; }

        internal string FilterText()
        {
            var text = new StringBuilder();

            foreach (FileFilter filter in Filters)
            {
                if (filter == null)
                {
                    continue;
                }

                if (text.Length > 0)
                {
                    text.Append('\n');
                }

                string pattern = filter.Pattern();
                string name = string.IsNullOrWhiteSpace(filter.Name) ? pattern : filter.Name.Replace('|', ' ').Replace('\n', ' ');

                text.Append(name).Append('|').Append(pattern);
            }

            return text.ToString();
        }
    }
}
