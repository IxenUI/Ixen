using System.Collections.Generic;

namespace Ixen.Core
{
    public sealed class FileFilter
    {
        public FileFilter(string name, params string[] extensions)
        {
            Name = name;
            Extensions = extensions ?? new string[0];
        }

        public string Name { get; }

        public IReadOnlyList<string> Extensions { get; }

        internal string Pattern()
        {
            var patterns = new List<string>();

            foreach (string extension in Extensions)
            {
                string bare = (extension ?? string.Empty).Trim().TrimStart('*').TrimStart('.');

                if (bare.Length > 0)
                {
                    patterns.Add("*." + bare);
                }
            }

            return patterns.Count == 0 ? "*.*" : string.Join(";", patterns);
        }
    }
}
