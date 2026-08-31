using Ixen.Core.Visual;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Ixen.Core.UT.Tree
{
    [TestClass]
    public class TreeGuardTests
    {
        private static VisualElement Named(string name) => new VisualElement { Name = name };

        [TestMethod]
        public void AnElementCannotBeItsOwnChild()
        {
            VisualElement box = Named("box");

            Assert.Throws<InvalidOperationException>(() => box.AddChild(box),
                "every pass over the tree is recursive, so a cycle is a StackOverflowException "
                + "rather than something a host could catch");
        }

        [TestMethod]
        public void AnAncestorCannotBecomeADescendant()
        {
            VisualElement root = Named("root");
            VisualElement middle = Named("middle");
            VisualElement leaf = Named("leaf");

            root.AddChild(middle);
            middle.AddChild(leaf);

            Assert.Throws<InvalidOperationException>(() => leaf.AddChild(root));
            Assert.Throws<InvalidOperationException>(() => leaf.AddChild(middle));
        }

        [TestMethod]
        public void TheMessageNamesBothElements()
        {
            VisualElement root = Named("page");
            VisualElement leaf = Named("row");

            root.AddChild(leaf);

            try
            {
                leaf.AddChild(root);
                Assert.Fail("expected a throw");
            }
            catch (InvalidOperationException error)
            {
                StringAssert.Contains(error.Message, "page");
                StringAssert.Contains(error.Message, "row");
            }
        }

        [TestMethod]
        public void ASiblingIsFine()
        {
            VisualElement root = Named("root");
            VisualElement first = Named("first");
            VisualElement second = Named("second");

            root.AddChild(first);
            root.AddChild(second);

            first.AddChild(Named("leaf"));

            Assert.AreEqual(2, root.ChildElements.Count, "the guard walks ancestors, not the whole tree");
        }

        [TestMethod]
        public void TheLimitIsTheLastLevelThatFits()
        {
            VisualElement root = Named("root");
            VisualElement current = root;

            for (int index = 1; index < VisualElement.MAX_DEPTH; index++)
            {
                VisualElement child = Named("n");
                current.AddChild(child);
                current = child;
            }

            Assert.Throws<InvalidOperationException>(() => current.AddChild(Named("one too many")),
                "the stack gives out somewhere around a thousand levels, so the limit leaves a "
                + "factor of two of margin");
        }

        [TestMethod]
        public void InsertChildIsGuardedToo()
        {
            VisualElement root = Named("root");
            VisualElement leaf = Named("leaf");

            root.AddChild(leaf);

            Assert.Throws<InvalidOperationException>(() => leaf.InsertChild(0, root));
        }

        [TestMethod]
        public void AddChildrenIsGuardedToo()
        {
            VisualElement root = Named("root");
            VisualElement leaf = Named("leaf");

            root.AddChild(leaf);

            Assert.Throws<InvalidOperationException>(() => leaf.AddChildren(Named("fine"), root));
        }

        [TestMethod]
        public void NothingIsAddedWhenOneOfTheBatchIsRefused()
        {
            VisualElement root = Named("root");
            VisualElement leaf = Named("leaf");

            root.AddChild(leaf);

            try
            {
                leaf.AddChildren(Named("fine"), root);
            }
            catch (InvalidOperationException)
            {
            }

            Assert.AreEqual(0, leaf.ChildElements.Count,
                "the whole batch is checked before any of it is attached, so a refusal leaves no "
                + "half-built tree behind");
        }

        [TestMethod]
        public void ANullChildIsRefusedByName()
        {
            Assert.Throws<ArgumentNullException>(() => Named("root").AddChild(null));
        }
    }
}
