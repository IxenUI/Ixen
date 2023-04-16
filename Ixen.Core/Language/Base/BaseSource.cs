using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ixen.Core.Language.Base
{
    internal abstract class BaseSource
    {
        protected string _source;
        protected string _sourceFilePath;
        protected List<string> _inputLines;

        public bool HasErrors { get; protected set; }

        public BaseSource(IEnumerable<string> inputLines)
        {
            _inputLines = inputLines.ToList();
        }

        public BaseSource(string filePath, string source)
        {
            _sourceFilePath = filePath;
            _source = source;

            if (_sourceFilePath != null)
            {
                _inputLines = File.ReadAllLines(_sourceFilePath).ToList();
            }
            else if (!string.IsNullOrEmpty(_source))
            {
                _inputLines = _source.Split(new string[] { Environment.NewLine }, StringSplitOptions.None).ToList();
            }
            else
            {
                throw new ArgumentException();
            }
        }

        public virtual bool UpdateLine(int lineNum, string line, int totalLines)
        {
            if (lineNum < 0)
            {
                return false;
            }

            bool update = false;

            while (lineNum >= _inputLines.Count)
            {
                _inputLines.Add(string.Empty);
                update = true;
            }

            int currentTotalLines = _inputLines.Count;
            while (totalLines < currentTotalLines)
            {
                _inputLines.RemoveAt(--currentTotalLines);
                update = true;
            }

            string currentLine = _inputLines[lineNum];
            if (currentLine.Length != line.Length || currentLine != line)
            {
                _inputLines[lineNum] = line;
                update = true;
            }

            return update;
        }
    }
}
