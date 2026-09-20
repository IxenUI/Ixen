using System.Collections.Generic;
using System.IO;
using Ixen.LanguageServer.Server;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.LanguageServer.UT.Server
{
    [TestClass]
    public class DefinitionTests
    {
        private string _folder;
        private Documents _documents;
        private Workspace _workspace;

        [TestInitialize]
        public void Setup()
        {
            _folder = Path.Combine(Path.GetTempPath(), "ixen-lsp-" + Path.GetRandomFileName());
            Directory.CreateDirectory(_folder);

            _documents = new Documents();
            _workspace = new Workspace();
            _workspace.AddRoot(_folder);
        }

        [TestCleanup]
        public void Cleanup()
        {
            try
            {
                Directory.Delete(_folder, true);
            }
            catch (IOException)
            {
            }
        }

        private Document Open(string name, string text)
        {
            string path = Path.Combine(_folder, name);

            File.WriteAllText(path, text);

            return _documents.Open(Uris.FromPath(path), text);
        }

        private IReadOnlyList<Target> Find(Document document, string needle)
        {
            return Definition.Find(document, document.Text.IndexOf(needle) + 1, _workspace, _documents);
        }

        [TestMethod]
        public void AVariableJumpsToItsDeclaration()
        {
            Document styles = Open("a.xns", "$accent: #4C6EF5\n\npanel { background: $accent }\n");
            IReadOnlyList<Target> targets = Find(styles, "$accent }");

            Assert.AreEqual(1, targets.Count);
            Assert.AreEqual(styles.Path, targets[0].Path);
            Assert.AreEqual(0, targets[0].Index);
        }

        [TestMethod]
        public void StandingOnTheDeclarationJumpsNowhere()
        {
            Document styles = Open("a.xns", "$accent: #4C6EF5\n\npanel { background: $accent }\n");

            Assert.AreEqual(0, Definition.Find(styles, 2, _workspace, _documents).Count);
        }

        [TestMethod]
        public void AnIncludeJumpsToItsMixin()
        {
            Document styles = Open("a.xns", "@mixin one_line { text-wrap: nowrap }\n\npanel { @include one_line }\n");
            IReadOnlyList<Target> targets = Find(styles, "@include one_line");

            Assert.AreEqual(1, targets.Count);
            Assert.AreEqual(0, targets[0].Index);
        }

        [TestMethod]
        public void AnAnimationJumpsToItsKeyframes()
        {
            Document styles = Open("a.xns",
                "@keyframes pulse {\n    0% { background: #000000 }\n    100% { background: #FFFFFF }\n}\n\npanel { animation: pulse 320ms }\n");
            IReadOnlyList<Target> targets = Find(styles, "pulse 320ms");

            Assert.AreEqual(1, targets.Count);
            Assert.AreEqual(0, targets[0].Index);
        }

        [TestMethod]
        public void AnElementNameJumpsToTheRuleThatStylesIt()
        {
            Open("a.xns", "panel {\n    width: 200px\n}\n");
            Document view = Open("a.xnl", "root {} [\n    panel {}\n]\n");

            IReadOnlyList<Target> targets = Find(view, "panel {}");

            Assert.AreEqual(1, targets.Count);
            Assert.IsTrue(targets[0].Path.EndsWith("a.xns"));
            Assert.AreEqual(0, targets[0].Index);
        }

        [TestMethod]
        public void AClassValueJumpsToTheClassRule()
        {
            Open("a.xns", "root {\n    width: 1*\n}\n\n.card {\n    width: 200px\n}\n");
            Document view = Open("a.xnl", "root {} [\n    box { class: \"card\" }\n]\n");

            IReadOnlyList<Target> targets = Find(view, "card\"");

            Assert.AreEqual(1, targets.Count);
            Assert.IsTrue(targets[0].Path.EndsWith("a.xns"));
        }

        [TestMethod]
        public void AValueThatIsNotAClassJumpsNowhere()
        {
            Open("a.xns", ".card {\n    width: 200px\n}\n");
            Document view = Open("a.xnl", "root {} [\n    box { text: \"card\" }\n]\n");

            Assert.AreEqual(0, Find(view, "card\"").Count);
        }

        [TestMethod]
        public void AnElementTypeJumpsToItsTypeRule()
        {
            Open("a.xns", "#Button {\n    width: 200px\n}\n");
            Document view = Open("a.xnl", "root {} [\n    ok<Button> {}\n]\n");

            IReadOnlyList<Target> targets = Find(view, "Button> {}");

            Assert.AreEqual(1, targets.Count);
            Assert.IsTrue(targets[0].Path.EndsWith("a.xns"));
        }

        [TestMethod]
        public void AStateVariantAnswersForItsBareSelector()
        {
            Open("a.xns", "panel:hover {\n    width: 200px\n}\n");
            Document view = Open("a.xnl", "root {} [\n    panel {}\n]\n");

            Assert.AreEqual(1, Find(view, "panel {}").Count);
        }

        [TestMethod]
        public void EveryRuleOfTheSameNameIsOffered()
        {
            Open("a.xns", "panel {\n    width: 200px\n}\n\npanel:hover {\n    width: 300px\n}\n");
            Document view = Open("a.xnl", "root {} [\n    panel {}\n]\n");

            Assert.AreEqual(2, Find(view, "panel {}").Count);
        }

        [TestMethod]
        public void TheRulesComeFromTheWholeWorkspaceRatherThanOneFile()
        {
            Open("theme.xns", "panel {\n    width: 200px\n}\n");
            Document view = Open("a.xnl", "root {} [\n    panel {}\n]\n");

            IReadOnlyList<Target> targets = Find(view, "panel {}");

            Assert.AreEqual(1, targets.Count);
            Assert.IsTrue(targets[0].Path.EndsWith("theme.xns"));
        }

        [TestMethod]
        public void AnOpenBufferIsPreferredToWhatIsOnDisk()
        {
            Open("a.xns", "panel {\n    width: 200px\n}\n");
            Document view = Open("a.xnl", "root {} [\n    card {}\n]\n");

            Assert.AreEqual(0, Find(view, "card {}").Count);

            _documents.Change(Uris.FromPath(Path.Combine(_folder, "a.xns")), "card {\n    width: 200px\n}\n");

            Assert.AreEqual(1, Find(view, "card {}").Count);
        }

        [TestMethod]
        public void AnUnknownExtensionJumpsNowhere()
        {
            Document other = _documents.Open(Uris.FromPath(Path.Combine(_folder, "a.txt")), "panel {}");

            Assert.AreEqual(0, Definition.Find(other, 1, _workspace, _documents).Count);
        }
    }
}
