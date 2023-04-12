using System;
using System.IO;

namespace Ixen.Core.Language.Base
{
    internal abstract class BaseSource
    {
        protected string _source;
        protected string _sourceFilePath;
        protected string[] _inputLines;

        public bool HasErrors { get; protected set; }

        public BaseSource(string[] inputLines)
        {
            _inputLines = inputLines;
        }

        public BaseSource(string filePath, string source)
        {
            _sourceFilePath = filePath;
            _source = source;

            if (_sourceFilePath != null)
            {
                _inputLines = File.ReadAllLines(_sourceFilePath);
            }
            else if (!string.IsNullOrEmpty(_source))
            {
                _inputLines = _source.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            }
            else
            {
                throw new ArgumentException();
            }
        }
    }
}
