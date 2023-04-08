using System;
using System.IO;

namespace Ixen.Core.Language.Base
{
    internal abstract class BaseSource
    {
        protected string _source;
        protected string _sourceFilePath;
        protected string[] _inputLines;

        public bool IsLoaded { get; protected set; }
        public bool IsParsed { get; protected set; }
        public bool IsValid { get; protected set; }

        public BaseSource(string filePath, string source)
        {
            _sourceFilePath = filePath;
            _source = source;

            Load();
        }

        private void Load()
        {
            if (IsLoaded)
            {
                return;
            }

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


            IsLoaded = true;
        }
    }
}
