using System.Collections.Generic;
using Ixen.LanguageServer.Server;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.LanguageServer.UT.Server
{
    [TestClass]
    public class CompletionTests
    {
        private readonly Documents _documents = new Documents();

        private IReadOnlyList<CompletionItem> At(string name, string text, string needle)
        {
            Document document = _documents.Open("file:///c:/probe/" + name, text);

            return Completion.At(document, text.IndexOf(needle) + needle.Length);
        }

        private static bool Holds(IReadOnlyList<CompletionItem> items, string label)
        {
            foreach (CompletionItem item in items)
            {
                if (item.Label == label)
                {
                    return true;
                }
            }

            return false;
        }

        [TestMethod]
        public void AStyleNameIsProposedInsideABlock()
        {
            IReadOnlyList<CompletionItem> items = At("a.xns", "panel {\n    wid\n}\n", "wid");

            Assert.IsTrue(Holds(items, "width"));
            Assert.AreEqual(Completion.KIND_PROPERTY, items[0].Kind);
        }

        [TestMethod]
        public void AStyleValueIsProposedAfterItsColon()
        {
            IReadOnlyList<CompletionItem> items = At("a.xns", "panel {\n    layout: ro\n}\n", "ro");

            Assert.IsTrue(Holds(items, "row"));
            Assert.AreEqual(Completion.KIND_VALUE, items[0].Kind);
        }

        [TestMethod]
        public void AStateIsProposedAfterASelectorsColon()
        {
            IReadOnlyList<CompletionItem> items = At("a.xns", "panel:ho\n", "ho");

            Assert.IsTrue(Holds(items, "hover"));
            Assert.AreEqual(Completion.KIND_KEYWORD, items[0].Kind);
        }

        [TestMethod]
        public void NothingIsProposedAtTheTopLevelOfAStylesheet()
        {
            Assert.AreEqual(0, At("a.xns", "pan\n", "pan").Count);
        }

        [TestMethod]
        public void AnElementTypeIsProposedAfterAnAngleBracket()
        {
            IReadOnlyList<CompletionItem> items = At("a.xnl", "title<Vis\n", "Vis");

            Assert.IsTrue(Holds(items, "VisualElement"));
            Assert.AreEqual(Completion.KIND_CLASS, items[0].Kind);
        }

        [TestMethod]
        public void APropertyNameIsProposedInsideAPropertyBlock()
        {
            IReadOnlyList<CompletionItem> items = At("a.xnl", "title<VisualElement> { tex\n", "tex");

            Assert.IsTrue(Holds(items, "text"));
            Assert.AreEqual(Completion.KIND_PROPERTY, items[0].Kind);
        }

        [TestMethod]
        public void TheEditSpanCoversTheWordBeingTyped()
        {
            string text = "panel {\n    wid\n}\n";
            IReadOnlyList<CompletionItem> items = At("a.xns", text, "wid");

            Assert.AreEqual(text.IndexOf("wid"), items[0].Start);
            Assert.AreEqual(3, items[0].Length);
        }

        [TestMethod]
        public void AnUnknownExtensionProposesNothing()
        {
            Assert.AreEqual(0, At("a.txt", "panel {\n    wid\n}\n", "wid").Count);
        }
    }
}
