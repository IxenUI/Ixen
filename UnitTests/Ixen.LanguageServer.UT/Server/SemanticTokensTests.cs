using System.Collections.Generic;
using Ixen.LanguageServer.Server;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.LanguageServer.UT.Server
{
    [TestClass]
    public class SemanticTokensTests
    {
        private static IReadOnlyList<int> Build(string path, string text)
        {
            return SemanticTokens.Build(path, text, new LineIndex(text));
        }

        private static void AssertToken(IReadOnlyList<int> data, int index,
            int deltaLine, int deltaCharacter, int length, int kind)
        {
            int at = index * 5;

            Assert.AreEqual(deltaLine, data[at], "delta line of token " + index);
            Assert.AreEqual(deltaCharacter, data[at + 1], "delta character of token " + index);
            Assert.AreEqual(length, data[at + 2], "length of token " + index);
            Assert.AreEqual(kind, data[at + 3], "kind of token " + index);
            Assert.AreEqual(0, data[at + 4], "modifiers of token " + index);
        }

        [TestMethod]
        public void EveryTokenIsFiveIntegers()
        {
            IReadOnlyList<int> data = Build("a.xns", "panel {\n    width: 200px\n}\n");

            Assert.AreEqual(0, data.Count % 5);
            Assert.IsTrue(data.Count > 0);
        }

        [TestMethod]
        public void ThePositionsAreDeltaEncoded()
        {
            IReadOnlyList<int> data = Build("a.xns", "panel {\n    width: 200px\n}\n");

            AssertToken(data, 0, 0, 0, 5, SemanticTokens.TYPE);
            AssertToken(data, 1, 1, 4, 5, SemanticTokens.PROPERTY);
            AssertToken(data, 2, 0, 7, 5, SemanticTokens.STRING);
        }

        [TestMethod]
        public void TwoTokensOnOneLineShareThatLine()
        {
            IReadOnlyList<int> data = Build("a.xns", "panel { width: 200px }\n");

            AssertToken(data, 1, 0, 8, 5, SemanticTokens.PROPERTY);
            AssertToken(data, 2, 0, 7, 5, SemanticTokens.STRING);
        }

        [TestMethod]
        public void ATokenIsNeverEmittedBackwards()
        {
            IReadOnlyList<int> data = Build("a.xns",
                "$accent: #4C6EF5\n\n@mixin one { width: 1* }\n\npanel {\n    @include one\n    background: $accent\n}\n");

            for (int at = 0; at < data.Count; at += 5)
            {
                Assert.IsTrue(data[at] >= 0, "delta line at " + at);

                if (data[at] == 0)
                {
                    Assert.IsTrue(data[at + 1] >= 0, "delta character at " + at);
                }
            }
        }

        [TestMethod]
        public void AMultiLineTokenIsSplitPerLine()
        {
            string text = "root {} [\n    @if (Ready\n        && Loaded) {\n        item {}\n    @}\n]\n";
            IReadOnlyList<int> data = Build("a.xnl", text);

            int macros = 0;

            for (int at = 0; at < data.Count; at += 5)
            {
                if (data[at + 3] == SemanticTokens.MACRO)
                {
                    macros++;
                }
            }

            Assert.IsTrue(macros >= 3, "the header should contribute one token per line it covers, saw " + macros);
        }

        [TestMethod]
        public void NoTokenCrossesALineEnd()
        {
            string text = "root {} [\n    @if (Ready\n        && Loaded) {\n        item {}\n    @}\n]\n";
            LineIndex lines = new LineIndex(text);
            IReadOnlyList<int> data = SemanticTokens.Build("a.xnl", text, lines);

            int line = 0;
            int character = 0;

            for (int at = 0; at < data.Count; at += 5)
            {
                line += data[at];
                character = data[at] == 0 ? character + data[at + 1] : data[at + 1];

                Assert.IsTrue(lines.StartOf(line) + character + data[at + 2] <= lines.EndOf(line),
                    "token at line " + line + " runs past the end of its line");
            }
        }

        [TestMethod]
        public void AZeroLengthTokenIsSkipped()
        {
            string text = "root { text: \"\" }\n";
            IReadOnlyList<int> data = Build("a.xnl", text);

            for (int at = 0; at < data.Count; at += 5)
            {
                Assert.IsTrue(data[at + 2] > 0, "a zero-length token was emitted");
            }
        }

        [TestMethod]
        public void AnUnknownExtensionProducesNothing()
        {
            Assert.AreEqual(0, Build("a.txt", "panel { width: 200px }").Count);
        }

        [TestMethod]
        public void TheLegendCoversEveryKindTheMapperCanReturn()
        {
            Assert.AreEqual(8, SemanticTokens.Legend.Length);
            Assert.AreEqual("type", SemanticTokens.Legend[SemanticTokens.TYPE]);
            Assert.AreEqual("operator", SemanticTokens.Legend[SemanticTokens.OPERATOR]);
            Assert.AreEqual(0, SemanticTokens.Modifiers.Length);
        }

        [TestMethod]
        public void AViewIsColouredByItsOwnTokenizer()
        {
            IReadOnlyList<int> data = Build("a.xnl", "title<VisualElement> { text: \"Hi\" }\n");

            AssertToken(data, 0, 0, 0, 5, SemanticTokens.VARIABLE);
            AssertToken(data, 1, 0, 5, 1, SemanticTokens.OPERATOR);
            AssertToken(data, 2, 0, 1, 13, SemanticTokens.TYPE);
        }
    }
}
