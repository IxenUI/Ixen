using Ixen.Core.Language.Xns;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace Ixen.Core.UT.Xns
{
    [TestClass]
    public class XnsStateSelectorTests
    {
        private static List<XnsToken> Tokens(string content)
            => new XnsSource(content).Tokenize();

        private static string Shape(string content)
            => string.Join(" ", Tokens(content).Select(t => $"{t.Type}({t.Content})"));

        private static List<string> ClassNames(string content)
            => Tokens(content)
                .Where(t => t.Type == XnsTokenType.ClassName)
                .Select(t => t.Content)
                .ToList();

        [TestMethod]
        public void AStateSelectorIsOneClassName()
        {
            string content = "action:hover { background: #FF0000 }";

            Assert.AreEqual(XnsTokenType.ClassName, Tokens(content)[0].Type, Shape(content));
            Assert.AreEqual("action:hover", Tokens(content)[0].Content);
        }

        [TestMethod]
        public void AStateSelectorFollowingAPlainOneIsStillAClassName()
        {
            string content = "action { color: #FFFFFF }\r\n\r\naction:hover { background: #FF0000 }\r\n\r\nfields { layout: row }";

            CollectionAssert.AreEqual(new[] { "action", "action:hover", "fields" }, ClassNames(content), Shape(content));
        }

        [TestMethod]
        public void AStateSelectorNestedInAClassIsStillAClassName()
        {
            string content = "panel {\r\n    action { color: #FFFFFF }\r\n    action:hover { background: #FF0000 }\r\n    action:pressed { background: #00FF00 }\r\n}";

            CollectionAssert.AreEqual(
                new[] { "panel", "action", "action:hover", "action:pressed" },
                ClassNames(content),
                Shape(content));
        }

        [TestMethod]
        public void AStyleAfterAStateSelectorIsStillAStyle()
        {
            string content = "action:hover { background: #FF0000 }";
            List<XnsToken> tokens = Tokens(content);

            Assert.IsTrue(tokens.Any(t => t.Type == XnsTokenType.StyleName && t.Content == "background"), Shape(content));
            Assert.IsTrue(tokens.Any(t => t.Type == XnsTokenType.StyleValue && t.Content == "#FF0000"), Shape(content));
        }

        [TestMethod]
        public void TheWholeFileIsTokenizedAfterAStateSelector()
        {
            string content = "action { cursor: hand }\r\naction:hover { background: #FF0000 }\r\nfields { layout: row }\r\n";

            var source = new XnsSource(content);
            source.Tokenize();

            Assert.IsFalse(source.HasErrors, Shape(content));
            Assert.IsTrue(Tokens(content).Any(t => t.Type == XnsTokenType.StyleValue && t.Content == "row"),
                "colouring stops wherever tokenizing stops, so the tail must be reached");
        }
    }
}
