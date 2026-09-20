using Ixen.LanguageServer.Server;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.LanguageServer.UT.Server
{
    [TestClass]
    public class LineIndexTests
    {
        [TestMethod]
        public void AnEmptyTextIsOneLine()
        {
            LineIndex lines = new LineIndex(string.Empty);

            Assert.AreEqual(1, lines.Count);
            Assert.AreEqual(0, lines.StartOf(0));
            Assert.AreEqual(0, lines.EndOf(0));
        }

        [TestMethod]
        public void ALineEndsBeforeItsTerminator()
        {
            LineIndex lines = new LineIndex("ab\r\ncd\n");

            Assert.AreEqual(3, lines.Count);
            Assert.AreEqual(2, lines.EndOf(0));
            Assert.AreEqual(4, lines.StartOf(1));
            Assert.AreEqual(6, lines.EndOf(1));
        }

        [TestMethod]
        public void AnOffsetMapsToItsOwnLine()
        {
            LineIndex lines = new LineIndex("ab\ncd\nef");

            lines.PositionOf(0, out int line, out int character);
            Assert.AreEqual(0, line);
            Assert.AreEqual(0, character);

            lines.PositionOf(4, out line, out character);
            Assert.AreEqual(1, line);
            Assert.AreEqual(1, character);

            lines.PositionOf(7, out line, out character);
            Assert.AreEqual(2, line);
            Assert.AreEqual(1, character);
        }

        [TestMethod]
        public void APositionMapsBackToItsOffset()
        {
            string text = "ab\r\ncd\nef";
            LineIndex lines = new LineIndex(text);

            for (int offset = 0; offset <= text.Length; offset++)
            {
                lines.PositionOf(offset, out int line, out int character);

                if (offset == 3)
                {
                    continue;
                }

                Assert.AreEqual(offset, lines.OffsetOf(line, character), "offset " + offset);
            }
        }

        [TestMethod]
        public void ACharacterPastTheEndOfALineIsClamped()
        {
            LineIndex lines = new LineIndex("ab\ncd");

            Assert.AreEqual(2, lines.OffsetOf(0, 40));
        }

        [TestMethod]
        public void ALinePastTheEndIsClamped()
        {
            LineIndex lines = new LineIndex("ab\ncd");

            Assert.AreEqual(5, lines.OffsetOf(40, 0));
        }

        [TestMethod]
        public void AnOffsetPastTheEndIsTheLastPosition()
        {
            LineIndex lines = new LineIndex("ab\ncd");

            lines.PositionOf(999, out int line, out int character);

            Assert.AreEqual(1, line);
            Assert.AreEqual(2, character);
        }
    }
}
