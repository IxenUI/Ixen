using System;
using System.Collections.Generic;
using System.Text.Json;
using Ixen.Core.Language.Base;
using Ixen.LanguageServer.Protocol;

namespace Ixen.LanguageServer.Server
{
    internal sealed class Server
    {
        private readonly Connection _connection;
        private readonly Documents _documents = new Documents();
        private readonly Workspace _workspace = new Workspace();

        private bool _running = true;
        private bool _shuttingDown;

        public Server(Connection connection)
        {
            _connection = connection;
        }

        public int Run()
        {
            while (_running)
            {
                JsonDocument message = _connection.Read();

                if (message == null)
                {
                    break;
                }

                using (message)
                {
                    Dispatch(message.RootElement);
                }
            }

            return _shuttingDown ? 0 : 1;
        }

        private void Dispatch(JsonElement message)
        {
            string method = Messages.Text(message, "method");

            if (method == null)
            {
                return;
            }

            bool answered = Messages.TryProperty(message, "id", out JsonElement id);
            Messages.TryProperty(message, "params", out JsonElement parameters);

            try
            {
                Handle(method, parameters, answered, id);
            }
            catch (Exception error)
            {
                if (answered)
                {
                    _connection.Write(Rpc.Error(id, Rpc.INTERNAL_ERROR, error.Message));
                }
            }
        }

        private void Handle(string method, JsonElement parameters, bool answered, JsonElement id)
        {
            switch (method)
            {
                case "initialize":
                    Initialize(parameters, id);
                    return;

                case "shutdown":
                    _shuttingDown = true;
                    _connection.Write(Rpc.Result(id, null));
                    return;

                case "exit":
                    _running = false;
                    return;

                case "textDocument/didOpen":
                    DidOpen(parameters);
                    return;

                case "textDocument/didChange":
                    DidChange(parameters);
                    return;

                case "textDocument/didClose":
                    DidClose(parameters);
                    return;

                case "textDocument/semanticTokens/full":
                    SemanticTokensFull(parameters, id);
                    return;

                case "textDocument/completion":
                    Complete(parameters, id);
                    return;

                case "textDocument/definition":
                    Define(parameters, id);
                    return;

                default:
                    if (answered)
                    {
                        _connection.Write(Rpc.Error(id, Rpc.METHOD_NOT_FOUND, "unknown method: " + method));
                    }
                    return;
            }
        }

        private void Initialize(JsonElement parameters, JsonElement id)
        {
            AddRoots(parameters);

            _connection.Write(Rpc.Result(id, writer =>
            {
                writer.WriteStartObject();
                writer.WriteStartObject("capabilities");

                writer.WriteStartObject("textDocumentSync");
                writer.WriteBoolean("openClose", true);
                writer.WriteNumber("change", 1);
                writer.WriteEndObject();

                writer.WriteStartObject("semanticTokensProvider");
                writer.WriteStartObject("legend");
                writer.WriteStartArray("tokenTypes");

                foreach (string type in SemanticTokens.Legend)
                {
                    writer.WriteStringValue(type);
                }

                writer.WriteEndArray();
                writer.WriteStartArray("tokenModifiers");
                writer.WriteEndArray();
                writer.WriteEndObject();
                writer.WriteBoolean("full", true);
                writer.WriteEndObject();

                writer.WriteStartObject("completionProvider");
                writer.WriteStartArray("triggerCharacters");
                writer.WriteStringValue(":");
                writer.WriteStringValue("<");
                writer.WriteStringValue("-");
                writer.WriteEndArray();
                writer.WriteEndObject();

                writer.WriteBoolean("definitionProvider", true);
                writer.WriteEndObject();

                writer.WriteStartObject("serverInfo");
                writer.WriteString("name", "Ixen");
                writer.WriteEndObject();

                writer.WriteEndObject();
            }));
        }

        private void AddRoots(JsonElement parameters)
        {
            if (Messages.TryProperty(parameters, "workspaceFolders", out JsonElement folders)
                && folders.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement folder in folders.EnumerateArray())
                {
                    _workspace.AddRoot(Uris.ToPath(Messages.Text(folder, "uri")));
                }
            }

            _workspace.AddRoot(Uris.ToPath(Messages.Text(parameters, "rootUri")));
            _workspace.AddRoot(Messages.Text(parameters, "rootPath"));
        }

        private void DidOpen(JsonElement parameters)
        {
            if (!Messages.TryProperty(parameters, "textDocument", out JsonElement document))
            {
                return;
            }

            Document opened = _documents.Open(Messages.Text(document, "uri"), Messages.Text(document, "text"));

            Publish(opened);
        }

        private void DidChange(JsonElement parameters)
        {
            string uri = Messages.DocumentUri(parameters);

            if (uri == null || !Messages.TryProperty(parameters, "contentChanges", out JsonElement changes)
                || changes.ValueKind != JsonValueKind.Array)
            {
                return;
            }

            string text = null;

            foreach (JsonElement change in changes.EnumerateArray())
            {
                text = Messages.Text(change, "text");
            }

            if (text == null)
            {
                return;
            }

            Publish(_documents.Change(uri, text));
        }

        private void DidClose(JsonElement parameters)
        {
            string uri = Messages.DocumentUri(parameters);

            if (uri == null)
            {
                return;
            }

            _documents.Close(uri);

            _connection.Write(Rpc.Notification("textDocument/publishDiagnostics", writer =>
            {
                writer.WriteStartObject();
                writer.WriteString("uri", uri);
                writer.WriteStartArray("diagnostics");
                writer.WriteEndArray();
                writer.WriteEndObject();
            }));
        }

        private void Publish(Document document)
        {
            if (document == null)
            {
                return;
            }

            IReadOnlyList<LanguageError> errors = Analysis.Diagnose(document.Path, document.Text);

            _connection.Write(Rpc.Notification("textDocument/publishDiagnostics", writer =>
            {
                writer.WriteStartObject();
                writer.WriteString("uri", document.Uri);
                writer.WriteNumber("version", document.Version);
                writer.WriteStartArray("diagnostics");

                foreach (LanguageError error in errors)
                {
                    writer.WriteStartObject();
                    WriteRange(writer, document.Lines, error.Index, error.Length);
                    writer.WriteNumber("severity", error.Severity == LanguageErrorSeverity.Warning ? 2 : 1);
                    writer.WriteString("code", error.Code);
                    writer.WriteString("source", "ixen");
                    writer.WriteString("message", error.Message ?? string.Empty);
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
                writer.WriteEndObject();
            }));
        }

        private void SemanticTokensFull(JsonElement parameters, JsonElement id)
        {
            Document document = _documents.Find(Messages.DocumentUri(parameters));

            if (document == null)
            {
                _connection.Write(Rpc.Result(id, null));
                return;
            }

            IReadOnlyList<int> data = SemanticTokens.Build(document.Path, document.Text, document.Lines);

            _connection.Write(Rpc.Result(id, writer =>
            {
                writer.WriteStartObject();
                writer.WriteStartArray("data");

                foreach (int value in data)
                {
                    writer.WriteNumberValue(value);
                }

                writer.WriteEndArray();
                writer.WriteEndObject();
            }));
        }

        private void Complete(JsonElement parameters, JsonElement id)
        {
            Document document = _documents.Find(Messages.DocumentUri(parameters));

            if (document == null || !Messages.Position(parameters, out int line, out int character))
            {
                _connection.Write(Rpc.Result(id, null));
                return;
            }

            int offset = document.Lines.OffsetOf(line, character);
            IReadOnlyList<CompletionItem> items = Completion.At(document, offset);

            _connection.Write(Rpc.Result(id, writer =>
            {
                writer.WriteStartObject();
                writer.WriteBoolean("isIncomplete", false);
                writer.WriteStartArray("items");

                foreach (CompletionItem item in items)
                {
                    writer.WriteStartObject();
                    writer.WriteString("label", item.Label);
                    writer.WriteNumber("kind", item.Kind);
                    writer.WriteStartObject("textEdit");
                    WriteRange(writer, document.Lines, item.Start, item.Length);
                    writer.WriteString("newText", item.Label);
                    writer.WriteEndObject();
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
                writer.WriteEndObject();
            }));
        }

        private void Define(JsonElement parameters, JsonElement id)
        {
            Document document = _documents.Find(Messages.DocumentUri(parameters));

            if (document == null || !Messages.Position(parameters, out int line, out int character))
            {
                _connection.Write(Rpc.Result(id, null));
                return;
            }

            int offset = document.Lines.OffsetOf(line, character);
            IReadOnlyList<Target> targets = Definition.Find(document, offset, _workspace, _documents);

            _connection.Write(Rpc.Result(id, writer =>
            {
                writer.WriteStartArray();

                foreach (Target target in targets)
                {
                    Document owner = _documents.FindByPath(target.Path);
                    LineIndex lines = owner != null ? owner.Lines : Lines(target.Path);

                    if (lines == null)
                    {
                        continue;
                    }

                    writer.WriteStartObject();
                    writer.WriteString("uri", Uris.FromPath(target.Path));
                    WriteRange(writer, lines, target.Index, target.Length);
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
            }));
        }

        private static LineIndex Lines(string path)
        {
            try
            {
                return new LineIndex(System.IO.File.ReadAllText(path));
            }
            catch (System.IO.IOException)
            {
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }
        }

        private static void WriteRange(Utf8JsonWriter writer, LineIndex lines, int index, int length)
        {
            lines.PositionOf(index, out int startLine, out int startCharacter);
            lines.PositionOf(index + (length > 0 ? length : 0), out int endLine, out int endCharacter);

            writer.WriteStartObject("range");
            writer.WriteStartObject("start");
            writer.WriteNumber("line", startLine);
            writer.WriteNumber("character", startCharacter);
            writer.WriteEndObject();
            writer.WriteStartObject("end");
            writer.WriteNumber("line", endLine);
            writer.WriteNumber("character", endCharacter);
            writer.WriteEndObject();
            writer.WriteEndObject();
        }
    }
}
