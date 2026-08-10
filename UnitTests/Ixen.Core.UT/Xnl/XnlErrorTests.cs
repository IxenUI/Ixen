using Ixen.Core.Language.Base;
using Ixen.Core.Language.Xnl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Ixen.Core.UT.Xnl
{
    [TestClass]
    public class XnlErrorTests
    {
        [TestMethod]
        public void ValidSource_ReportsNoError()
        {
            var xnlSource = new XnlSource("container {}\r\n[\r\n\tel1 {}\r\n\tel2 {}\r\n]\r\n");
            var node = xnlSource.Nodify();

            Assert.IsFalse(xnlSource.HasErrors, string.Join(" | ", xnlSource.Diagnostics.Select(e => e.Message)));
            Assert.IsNotNull(node);
        }

        [TestMethod]
        public void TrailingWhitespaceAndNewlines_AreNotAnError()
        {
            var xnlSource = new XnlSource("container {}\r\n   \r\n\r\n");
            xnlSource.Nodify();

            Assert.IsFalse(xnlSource.HasErrors, string.Join(" | ", xnlSource.Diagnostics.Select(e => e.Message)));
        }

        [TestMethod]
        public void UnclosedChildrenBlock_IsReportedAtEndOfFile()
        {
            var xnlSource = new XnlSource("container {}\r\n[\r\n\tel1 {}\r\n");
            var node = xnlSource.Nodify();

            Assert.IsTrue(xnlSource.HasErrors);
            Assert.AreEqual(LanguageErrorCode.SYNTAX, xnlSource.Diagnostics[0].Code);
            Assert.IsNull(node);
        }

        [TestMethod]
        public void UnexpectedCharacter_IsReportedAtItsPosition()
        {
            string source = "container {}\r\n@\r\n";
            var xnlSource = new XnlSource(source);
            xnlSource.Nodify();

            Assert.IsTrue(xnlSource.HasErrors);
            Assert.AreEqual(LanguageErrorCode.SYNTAX, xnlSource.Diagnostics[0].Code);
            Assert.AreEqual("@", source.Substring(xnlSource.Diagnostics[0].Index, xnlSource.Diagnostics[0].Length));
        }

        [TestMethod]
        [Timeout(3000)]
        public void AnUnterminatedPropertyValue_DoesNotHang()
        {
            var xnlSource = new XnlSource("container {}\r\n[\r\n\tel1 { text: \"coucou }\r\n\tel2 {}\r\n]\r\n");

            xnlSource.Tokenize();

            Assert.IsTrue(xnlSource.HasErrors, "an unterminated string should be reported");
        }

        [TestMethod]
        [Timeout(3000)]
        public void AnUnterminatedPropertyValueAtEndOfFile_DoesNotHang()
        {
            var xnlSource = new XnlSource("container {}\r\n[\r\n\tel1 { text: \"");

            xnlSource.Tokenize();

            Assert.IsTrue(xnlSource.HasErrors, "an unterminated string should be reported");
        }

        [TestMethod]
        public void ErrorsPreventNodifying()
        {
            var xnlSource = new XnlSource("container {}\r\n@\r\n");

            Assert.IsNull(xnlSource.Nodify());
        }
    }
}
