using System;
using System.IO;
using System.Text.Json;
using Ixen.LanguageServer.Server;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.LanguageServer.UT.Protocol
{
    [TestClass]
    public class ProtocolTests
    {
        private string _folder;
        private LspClient _client;

        [TestInitialize]
        public void Setup()
        {
            _folder = Path.Combine(Path.GetTempPath(), "ixen-lsp-" + Path.GetRandomFileName());
            Directory.CreateDirectory(_folder);

            _client = new LspClient();
            _client.Request("initialize", "{\"rootUri\":" + LspClient.Quote(Uris.FromPath(_folder))
                + ",\"capabilities\":{}}");
            _client.Send("initialized", "{}");
        }

        [TestCleanup]
        public void Cleanup()
        {
            _client.Dispose();

            try
            {
                Directory.Delete(_folder, true);
            }
            catch (IOException)
            {
            }
        }

        private string Write(string name, string text)
        {
            string path = Path.Combine(_folder, name);

            File.WriteAllText(path, text);

            return path;
        }

        private string Open(string name, string text)
        {
            string path = Write(name, text);
            string uri = Uris.FromPath(path);

            _client.Send("textDocument/didOpen", "{\"textDocument\":{\"uri\":" + LspClient.Quote(uri)
                + ",\"languageId\":\"ixen\",\"version\":1,\"text\":" + LspClient.Quote(text) + "}}");

            return uri;
        }

        private static JsonElement Diagnostics(JsonElement notification)
        {
            return notification.GetProperty("params").GetProperty("diagnostics");
        }

        [TestMethod]
        [Timeout(15000)]
        public void TheServerAnswersInitializeWithItsCapabilities()
        {
            using (LspClient client = new LspClient())
            {
                JsonElement answer = client.Request("initialize", "{\"capabilities\":{}}");
                JsonElement capabilities = answer.GetProperty("result").GetProperty("capabilities");

                Assert.IsTrue(capabilities.GetProperty("definitionProvider").GetBoolean());
                Assert.AreEqual(8, capabilities.GetProperty("semanticTokensProvider")
                    .GetProperty("legend").GetProperty("tokenTypes").GetArrayLength());
                Assert.AreEqual(1, capabilities.GetProperty("textDocumentSync").GetProperty("change").GetInt32());
            }
        }

        [TestMethod]
        [Timeout(15000)]
        public void OpeningAGoodStylesheetPublishesNoDiagnostic()
        {
            Open("a.xns", "panel {\n    width: 200px\n}\n");

            JsonElement notification = _client.WaitForNotification("textDocument/publishDiagnostics", "a.xns");

            Assert.AreEqual(0, Diagnostics(notification).GetArrayLength());
        }

        [TestMethod]
        [Timeout(15000)]
        public void OpeningABadStylesheetPublishesTheCodeAndTheRange()
        {
            Open("bad.xns", "panel {\n    widht: 200px\n}\n");

            JsonElement diagnostics = Diagnostics(_client.WaitForNotification("textDocument/publishDiagnostics", "bad.xns"));

            Assert.AreEqual(1, diagnostics.GetArrayLength());

            JsonElement first = diagnostics[0];

            Assert.AreEqual("XN002", first.GetProperty("code").GetString());
            Assert.AreEqual("ixen", first.GetProperty("source").GetString());
            Assert.AreEqual(1, first.GetProperty("severity").GetInt32());
            Assert.AreEqual(1, first.GetProperty("range").GetProperty("start").GetProperty("line").GetInt32());
            Assert.AreEqual(4, first.GetProperty("range").GetProperty("start").GetProperty("character").GetInt32());
        }

        [TestMethod]
        [Timeout(15000)]
        public void ChangingADocumentRepublishes()
        {
            string uri = Open("live.xns", "panel {\n    widht: 200px\n}\n");

            Assert.AreEqual(1, Diagnostics(_client.WaitForNotification("textDocument/publishDiagnostics", "live.xns")).GetArrayLength());

            _client.Send("textDocument/didChange", "{\"textDocument\":{\"uri\":" + LspClient.Quote(uri)
                + ",\"version\":2},\"contentChanges\":[{\"text\":" + LspClient.Quote("panel {\n    width: 200px\n}\n") + "}]}");

            JsonElement second = _client.Request("textDocument/semanticTokens/full",
                "{\"textDocument\":{\"uri\":" + LspClient.Quote(uri) + "}}");

            Assert.IsTrue(second.TryGetProperty("result", out JsonElement _));

            foreach (JsonElement notification in _client.Notifications)
            {
                if (notification.TryGetProperty("params", out JsonElement parameters)
                    && parameters.TryGetProperty("version", out JsonElement version) && version.GetInt32() == 2)
                {
                    Assert.AreEqual(0, parameters.GetProperty("diagnostics").GetArrayLength());
                    return;
                }
            }

            Assert.Fail("no diagnostics were published for the second version");
        }

        [TestMethod]
        [Timeout(15000)]
        public void ClosingADocumentClearsItsDiagnostics()
        {
            string uri = Open("closing.xns", "panel {\n    widht: 200px\n}\n");

            _client.WaitForNotification("textDocument/publishDiagnostics", "closing.xns");
            _client.Send("textDocument/didClose", "{\"textDocument\":{\"uri\":" + LspClient.Quote(uri) + "}}");

            JsonElement answer = _client.Request("textDocument/semanticTokens/full",
                "{\"textDocument\":{\"uri\":" + LspClient.Quote(uri) + "}}");

            Assert.AreEqual(JsonValueKind.Null, answer.GetProperty("result").ValueKind);

            int cleared = 0;

            foreach (JsonElement notification in _client.Notifications)
            {
                if (notification.TryGetProperty("params", out JsonElement parameters)
                    && parameters.TryGetProperty("uri", out JsonElement seen)
                    && seen.GetString().EndsWith("closing.xns", StringComparison.OrdinalIgnoreCase)
                    && parameters.GetProperty("diagnostics").GetArrayLength() == 0)
                {
                    cleared++;
                }
            }

            Assert.AreEqual(1, cleared);
        }

        [TestMethod]
        [Timeout(15000)]
        public void SemanticTokensComeBackAsFiveIntegersEach()
        {
            string uri = Open("tokens.xns", "panel {\n    width: 200px\n}\n");

            JsonElement data = _client.Request("textDocument/semanticTokens/full",
                "{\"textDocument\":{\"uri\":" + LspClient.Quote(uri) + "}}")
                .GetProperty("result").GetProperty("data");

            Assert.IsTrue(data.GetArrayLength() > 0);
            Assert.AreEqual(0, data.GetArrayLength() % 5);
        }

        [TestMethod]
        [Timeout(15000)]
        public void CompletionComesBackWithAnEditSpan()
        {
            string uri = Open("complete.xns", "panel {\n    wid\n}\n");

            JsonElement items = _client.Request("textDocument/completion",
                "{\"textDocument\":{\"uri\":" + LspClient.Quote(uri) + "},\"position\":{\"line\":1,\"character\":7}}")
                .GetProperty("result").GetProperty("items");

            Assert.IsTrue(items.GetArrayLength() > 0);

            bool found = false;

            foreach (JsonElement item in items.EnumerateArray())
            {
                if (item.GetProperty("label").GetString() == "width")
                {
                    JsonElement range = item.GetProperty("textEdit").GetProperty("range");

                    Assert.AreEqual(1, range.GetProperty("start").GetProperty("line").GetInt32());
                    Assert.AreEqual(4, range.GetProperty("start").GetProperty("character").GetInt32());
                    Assert.AreEqual(7, range.GetProperty("end").GetProperty("character").GetInt32());

                    found = true;
                }
            }

            Assert.IsTrue(found, "width was not proposed");
        }

        [TestMethod]
        [Timeout(15000)]
        public void DefinitionCrossesFromAViewToItsStylesheet()
        {
            Open("rules.xns", "panel {\n    width: 200px\n}\n");
            string uri = Open("view.xnl", "root {} [\n    panel {}\n]\n");

            JsonElement targets = _client.Request("textDocument/definition",
                "{\"textDocument\":{\"uri\":" + LspClient.Quote(uri) + "},\"position\":{\"line\":1,\"character\":6}}")
                .GetProperty("result");

            Assert.AreEqual(1, targets.GetArrayLength());
            Assert.IsTrue(targets[0].GetProperty("uri").GetString().EndsWith("rules.xns", StringComparison.OrdinalIgnoreCase));
            Assert.AreEqual(0, targets[0].GetProperty("range").GetProperty("start").GetProperty("line").GetInt32());
        }

        [TestMethod]
        [Timeout(15000)]
        public void DefinitionReachesARuleThatIsOnlyOnDisk()
        {
            Write("disk.xns", "card {\n    width: 200px\n}\n");

            string uri = Open("view.xnl", "root {} [\n    card {}\n]\n");

            JsonElement targets = _client.Request("textDocument/definition",
                "{\"textDocument\":{\"uri\":" + LspClient.Quote(uri) + "},\"position\":{\"line\":1,\"character\":5}}")
                .GetProperty("result");

            Assert.AreEqual(1, targets.GetArrayLength());
            Assert.IsTrue(targets[0].GetProperty("uri").GetString().EndsWith("disk.xns", StringComparison.OrdinalIgnoreCase));
        }

        [TestMethod]
        [Timeout(15000)]
        public void AnUnknownRequestIsAnsweredWithMethodNotFound()
        {
            JsonElement answer = _client.Request("textDocument/wobble", "{}");

            Assert.AreEqual(-32601, answer.GetProperty("error").GetProperty("code").GetInt32());
        }

        [TestMethod]
        [Timeout(15000)]
        public void AnUnknownNotificationIsIgnored()
        {
            _client.Send("$/setTrace", "{\"value\":\"off\"}");

            JsonElement answer = _client.Request("shutdown", "null");

            Assert.AreEqual(JsonValueKind.Null, answer.GetProperty("result").ValueKind);
        }
    }
}
