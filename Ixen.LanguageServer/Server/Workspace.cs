using System;
using System.Collections.Generic;
using System.IO;
using Ixen.Core.Language.Xns;
using Ixen.Core.Visual.Classes;

namespace Ixen.LanguageServer.Server
{
    internal sealed class Sheet
    {
        public string Path { get; set; }
        public ClassesSet Classes { get; set; }
        public long Stamp { get; set; }
        public int BufferVersion { get; set; }
    }

    internal sealed class Workspace
    {
        private const int MAX_SHEETS = 2000;
        private const int MAX_DEPTH = 24;

        private static readonly string[] _skipped = { "bin", "obj", ".git", ".vs", "node_modules", "TestResults" };

        private readonly List<string> _roots = new List<string>();
        private readonly Dictionary<string, Sheet> _sheets = new Dictionary<string, Sheet>(StringComparer.OrdinalIgnoreCase);

        private bool _scanned;

        public void AddRoot(string path)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            {
                return;
            }

            string full = Path.GetFullPath(path);

            foreach (string known in _roots)
            {
                if (string.Equals(known, full, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            _roots.Add(full);
            _scanned = false;
        }

        public IReadOnlyCollection<Sheet> Sheets(Documents documents)
        {
            Scan();

            foreach (Sheet sheet in _sheets.Values)
            {
                Refresh(sheet, documents);
            }

            return _sheets.Values;
        }

        private void Scan()
        {
            if (_scanned)
            {
                return;
            }

            _scanned = true;

            foreach (string root in _roots)
            {
                Walk(root, 0);
            }
        }

        private void Walk(string directory, int depth)
        {
            if (depth > MAX_DEPTH || _sheets.Count >= MAX_SHEETS)
            {
                return;
            }

            string[] files;
            string[] directories;

            try
            {
                files = Directory.GetFiles(directory);
                directories = Directory.GetDirectories(directory);
            }
            catch (IOException)
            {
                return;
            }
            catch (UnauthorizedAccessException)
            {
                return;
            }

            foreach (string file in files)
            {
                if (!Uris.IsXns(file) || _sheets.Count >= MAX_SHEETS)
                {
                    continue;
                }

                string full = Path.GetFullPath(file);

                if (!_sheets.ContainsKey(full))
                {
                    _sheets[full] = new Sheet { Path = full, Stamp = -1, BufferVersion = -1 };
                }
            }

            foreach (string child in directories)
            {
                if (!IsSkipped(child))
                {
                    Walk(child, depth + 1);
                }
            }
        }

        private static bool IsSkipped(string directory)
        {
            string name = Path.GetFileName(directory);

            foreach (string skipped in _skipped)
            {
                if (string.Equals(name, skipped, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static void Refresh(Sheet sheet, Documents documents)
        {
            Document open = documents?.FindByPath(sheet.Path);

            if (open != null)
            {
                if (sheet.BufferVersion == open.Version)
                {
                    return;
                }

                sheet.Classes = Compile(open.Text);
                sheet.BufferVersion = open.Version;
                sheet.Stamp = -1;

                return;
            }

            long stamp;

            try
            {
                stamp = File.GetLastWriteTimeUtc(sheet.Path).Ticks;
            }
            catch (IOException)
            {
                return;
            }

            if (sheet.BufferVersion < 0 && sheet.Stamp == stamp)
            {
                return;
            }

            try
            {
                sheet.Classes = Compile(File.ReadAllText(sheet.Path));
            }
            catch (IOException)
            {
                sheet.Classes = null;
            }
            catch (UnauthorizedAccessException)
            {
                sheet.Classes = null;
            }

            sheet.Stamp = stamp;
            sheet.BufferVersion = -1;
        }

        private static ClassesSet Compile(string text)
        {
            return new XnsSource(text ?? string.Empty).Compile();
        }
    }
}
