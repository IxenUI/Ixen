using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Layout.Geometry
{
    [TestClass]
    public class DirtyTrackingTests : BaseGeometryTests
    {
        private const float SENTINEL = 999;

        private static VisualElement BuildTree(out VisualElement child)
        {
            var root = Element("root", LayoutType.Row);
            child = Element("child", LayoutType.Column, SizeUnit.Pixels, 100, SizeUnit.Pixels, 20);
            root.AddChild(child);
            return root;
        }

        [TestMethod]
        public void AFreshTree_IsDirty()
        {
            VisualElement root = BuildTree(out _);

            Assert.IsTrue(root.IsLayoutDirty);
        }

        [TestMethod]
        public void AfterALayout_NothingIsDirtyAnymore()
        {
            VisualElement root = BuildTree(out VisualElement child);
            var surface = new IxenSurface(root);

            surface.ComputeLayout(400, 400);

            Assert.IsFalse(root.IsLayoutDirty, "root");
            Assert.IsFalse(child.IsLayoutDirty, "child");
        }

        [TestMethod]
        public void AnUnchangedSecondLayout_IsSkippedEntirely()
        {
            VisualElement root = BuildTree(out VisualElement child);
            var surface = new IxenSurface(root);
            surface.ComputeLayout(400, 400);

            child.Width = SENTINEL;
            surface.ComputeLayout(400, 400);

            Assert.AreEqual(SENTINEL, child.Width, "the passes should not have run again");
        }

        [TestMethod]
        public void InvalidatingAnElement_MakesTheNextLayoutRun()
        {
            VisualElement root = BuildTree(out VisualElement child);
            var surface = new IxenSurface(root);
            surface.ComputeLayout(400, 400);

            child.Width = SENTINEL;
            child.Invalidate();
            surface.ComputeLayout(400, 400);

            Assert.AreEqual(100, child.Width, "the passes should have run again");
        }

        [TestMethod]
        public void InvalidatingADeepChild_MarksItsAncestorsDirty()
        {
            var leaf = Element("leaf", LayoutType.Column, SizeUnit.Pixels, 10, SizeUnit.Pixels, 10);
            var middle = Element("middle", LayoutType.Column, SizeUnit.Pixels, 50, SizeUnit.Pixels, 50);
            middle.AddChild(leaf);
            var root = Element("root", LayoutType.Column);
            root.AddChild(middle);

            var surface = new IxenSurface(root);
            surface.ComputeLayout(400, 400);
            Assert.IsFalse(root.IsLayoutDirty, "root should be clean after a layout");

            leaf.Invalidate();

            Assert.IsTrue(leaf.IsLayoutDirty, "leaf");
            Assert.IsTrue(middle.IsLayoutDirty, "middle");
            Assert.IsTrue(root.IsLayoutDirty, "root must be reachable for the surface to notice");
        }

        [TestMethod]
        public void AddingAChild_MarksTheContainerAndItsAncestorsDirty()
        {
            var root = Element("root", LayoutType.Column);
            var container = Element("container", LayoutType.Column, SizeUnit.Pixels, 50, SizeUnit.Pixels, 50);
            root.AddChild(container);

            var surface = new IxenSurface(root);
            surface.ComputeLayout(400, 400);
            Assert.IsFalse(root.IsLayoutDirty);

            container.AddChild(Element("late", LayoutType.Column, SizeUnit.Pixels, 10, SizeUnit.Pixels, 10));

            Assert.IsTrue(container.IsLayoutDirty, "container");
            Assert.IsTrue(root.IsLayoutDirty, "root");
        }

        [TestMethod]
        public void RemovingAChild_MarksTheContainerDirty()
        {
            VisualElement root = BuildTree(out VisualElement child);
            var surface = new IxenSurface(root);
            surface.ComputeLayout(400, 400);
            Assert.IsFalse(root.IsLayoutDirty);

            root.RemoveChild(child);

            Assert.IsTrue(root.IsLayoutDirty);
        }

        [TestMethod]
        public void InvalidateLayout_RunsTheLayoutWithoutRecomputingStyles()
        {
            VisualElement root = BuildTree(out VisualElement child);
            var surface = new IxenSurface(root);
            surface.ComputeLayout(400, 400);

            child.Width = SENTINEL;
            child.InvalidateLayout();

            Assert.IsFalse(child.MustRefreshStyles, "styles must stay clean");

            surface.ComputeLayout(400, 400);

            Assert.AreEqual(100, child.Width, "the layout passes should have run");
        }

        [TestMethod]
        public void AResize_RunsTheLayoutEvenWhenNothingIsDirty()
        {
            VisualElement root = BuildTree(out VisualElement child);
            var surface = new IxenSurface(root);
            surface.ComputeLayout(400, 400);

            child.Width = SENTINEL;
            surface.ComputeLayout(300, 300);

            Assert.AreEqual(100, child.Width, "a viewport change must force a layout");
        }

        [TestMethod]
        public void AssigningANewRoot_ForcesALayout()
        {
            VisualElement root = BuildTree(out VisualElement child);
            var surface = new IxenSurface(root);
            surface.ComputeLayout(400, 400);

            VisualElement other = BuildTree(out VisualElement otherChild);
            new IxenSurface(other).ComputeLayout(400, 400);
            otherChild.Width = SENTINEL;

            surface.Root = other;
            surface.ComputeLayout(400, 400);

            Assert.AreEqual(100, otherChild.Width, "a new root must be laid out");
        }
    }
}
