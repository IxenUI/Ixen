using Ixen.Core.Accessibility;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Accessibility
{
    [TestClass]
    public class TextDeltaTests
    {
        private static void AssertDelta(string before, string after, int start, int removed, int added)
        {
            TextDelta delta = TextDelta.Of(before, after);

            Assert.AreEqual(start, delta.Start, $"start of '{before}' -> '{after}'");
            Assert.AreEqual(removed, delta.Removed, $"removed of '{before}' -> '{after}'");
            Assert.AreEqual(added, delta.Added, $"added of '{before}' -> '{after}'");
        }

        [TestMethod]
        public void TypingAtTheEndAddsOneCharacterThere()
        {
            AssertDelta("Ad", "Ada", 2, 0, 1);
        }

        [TestMethod]
        public void TypingInTheMiddleIsFoundBetweenTheCommonEnds()
        {
            AssertDelta("Aa", "Ada", 1, 0, 1);
        }

        [TestMethod]
        public void BackspaceRemovesOne()
        {
            AssertDelta("Ada", "Ad", 2, 1, 0);
        }

        [TestMethod]
        public void ReplacingASelectionRemovesAndAddsAtOnce()
        {
            AssertDelta("Hello Ada", "Hello Zoe", 6, 3, 3);
        }

        [TestMethod]
        public void ARepeatedLetterIsAttributedOnce()
        {
            AssertDelta("Ana", "Anna", 2, 0, 1);
        }

        [TestMethod]
        public void NullIsTheEmptyText()
        {
            AssertDelta(null, "A", 0, 0, 1);
            AssertDelta("A", null, 0, 1, 0);
            Assert.IsTrue(TextDelta.Of(null, string.Empty).IsEmpty);
        }

        [TestMethod]
        public void TheSameTextIsNoChange()
        {
            Assert.IsTrue(TextDelta.Of("Ada", "Ada").IsEmpty);
        }

        [TestMethod]
        public void ASurrogatePairIsNeverSplit()
        {
            AssertDelta("a\U0001F600b", "a\U0001F601b", 1, 2, 2);
        }
    }
}
