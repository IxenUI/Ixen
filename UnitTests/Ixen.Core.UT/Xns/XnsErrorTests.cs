using Ixen.Core.Language.Base;
using Ixen.Core.Language.Xns;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Ixen.Core.UT.Xns
{
    [TestClass]
    public class XnsErrorTests
    {
        private static LanguageError SingleError(string source)
        {
            var xnsSource = new XnsSource(source);
            xnsSource.Compile();

            Assert.AreEqual(1, xnsSource.Errors.Count, "expected exactly one error");

            return xnsSource.Errors[0];
        }

        [TestMethod]
        public void ValidSource_ReportsNoError()
        {
            var xnsSource = new XnsSource(@"container {
    layout: row
    width: 100%

    panel {
        width: 50px
        background: #222222
    }
}");
            var classes = xnsSource.Compile();

            Assert.IsFalse(xnsSource.HasErrors, string.Join(" | ", xnsSource.Errors.Select(e => e.Message)));
            Assert.IsNotNull(classes);
        }

        [TestMethod]
        public void TrailingWhitespaceAndNewlines_AreNotAnError()
        {
            var xnsSource = new XnsSource("container {\r\n    layout: row\r\n}\r\n   \r\n\r\n");
            xnsSource.Compile();

            Assert.IsFalse(xnsSource.HasErrors, string.Join(" | ", xnsSource.Errors.Select(e => e.Message)));
        }

        [TestMethod]
        public void TrailingComment_IsNotAnError()
        {
            var xnsSource = new XnsSource("container {\r\n    layout: row\r\n}\r\n// done\r\n");
            xnsSource.Compile();

            Assert.IsFalse(xnsSource.HasErrors, string.Join(" | ", xnsSource.Errors.Select(e => e.Message)));
        }

        [TestMethod]
        public void UnknownStyleProperty_IsReportedAtTheNamePosition()
        {
            string source = "container {\r\n    bogus: 12px\r\n}";
            LanguageError error = SingleError(source);

            Assert.AreEqual(LanguageErrorCode.UNKNOWN_STYLE, error.Code);
            Assert.AreEqual("bogus", source.Substring(error.Index, error.Length));
        }

        [TestMethod]
        public void InvalidStyleValue_IsReportedAtTheValuePosition()
        {
            string source = "container {\r\n    width: 12ox\r\n}";
            LanguageError error = SingleError(source);

            Assert.AreEqual(LanguageErrorCode.INVALID_STYLE_VALUE, error.Code);
            Assert.AreEqual("12ox", source.Substring(error.Index, error.Length));
        }

        [TestMethod]
        public void UnknownLayoutValue_IsReportedAsAnInvalidValue()
        {
            string source = "container {\r\n    layout: diagonal\r\n}";
            LanguageError error = SingleError(source);

            Assert.AreEqual(LanguageErrorCode.INVALID_STYLE_VALUE, error.Code);
            Assert.AreEqual("diagonal", source.Substring(error.Index, error.Length));
        }

        [TestMethod]
        public void DockLayout_IsAccepted()
        {
            var xnsSource = new XnsSource("container {\r\n    layout: dock\r\n}");
            xnsSource.Compile();

            Assert.IsFalse(xnsSource.HasErrors, string.Join(" | ", xnsSource.Errors.Select(e => e.Message)));
        }

        [TestMethod]
        public void UnclosedBlock_IsReportedAtEndOfFile()
        {
            var xnsSource = new XnsSource("container {\r\n    layout: row\r\n");
            xnsSource.Compile();

            Assert.IsTrue(xnsSource.HasErrors);
            Assert.AreEqual(LanguageErrorCode.SYNTAX, xnsSource.Errors[0].Code);
        }

        [TestMethod]
        public void UnexpectedCharacter_IsReportedAtItsPosition()
        {
            string source = "container {\r\n    layout: row\r\n}\r\n@\r\n";
            var xnsSource = new XnsSource(source);
            xnsSource.Compile();

            Assert.IsTrue(xnsSource.HasErrors);
            Assert.AreEqual(LanguageErrorCode.SYNTAX, xnsSource.Errors[0].Code);
            Assert.AreEqual("@", source.Substring(xnsSource.Errors[0].Index, xnsSource.Errors[0].Length));
        }

        [TestMethod]
        public void AnInvalidStyle_DoesNotDiscardTheWholeSheet()
        {
            var xnsSource = new XnsSource(@"container {
    layout: row
    bogus: 1
    width: 100%
}");
            var classes = xnsSource.Compile();

            Assert.IsTrue(xnsSource.HasErrors);
            Assert.IsNotNull(classes);
            Assert.AreEqual(1, classes.Classes.Count);
            Assert.AreEqual(2, classes.Classes[0].Styles.Count);
        }
    }
}
