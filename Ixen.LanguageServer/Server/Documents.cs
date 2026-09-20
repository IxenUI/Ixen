using System;
using System.Collections.Generic;

namespace Ixen.LanguageServer.Server
{
    internal sealed class Document
    {
        public string Uri { get; }
        public string Path { get; }
        public string Text { get; private set; }
        public LineIndex Lines { get; private set; }
        public int Version { get; private set; }

        public Document(string uri, string path, string text)
        {
            Uri = uri;
            Path = path;
            Set(text);
        }

        public void Set(string text)
        {
            Text = text ?? string.Empty;
            Version++;
            Lines = new LineIndex(Text);
        }
    }

    internal sealed class Documents
    {
        private readonly Dictionary<string, Document> _open = new Dictionary<string, Document>(StringComparer.Ordinal);

        public Document Open(string uri, string text)
        {
            string path = Uris.ToPath(uri);

            if (path == null)
            {
                return null;
            }

            Document document = new Document(uri, path, text);
            _open[uri] = document;

            return document;
        }

        public Document Change(string uri, string text)
        {
            if (!_open.TryGetValue(uri, out Document document))
            {
                return Open(uri, text);
            }

            document.Set(text);

            return document;
        }

        public void Close(string uri)
        {
            _open.Remove(uri);
        }

        public Document Find(string uri)
        {
            return uri != null && _open.TryGetValue(uri, out Document document) ? document : null;
        }

        public Document FindByPath(string path)
        {
            foreach (Document document in _open.Values)
            {
                if (string.Equals(document.Path, path, StringComparison.OrdinalIgnoreCase))
                {
                    return document;
                }
            }

            return null;
        }
    }
}
