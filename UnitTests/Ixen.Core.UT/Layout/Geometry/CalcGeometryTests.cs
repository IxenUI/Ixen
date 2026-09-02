using Ixen.Core.Language.Xns;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Ixen.Core.UT.Layout.Geometry
{
    [TestClass]
    public class CalcGeometryTests
    {
        private const int VIEWPORT = 400;

        private static IxenSurface Surface(string xns, VisualElement root)
        {
            var source = new XnsSource(xns);
            ClassesSet set = source.Compile();

            Assert.IsFalse(source.HasErrors,
                string.Join(" | ", source.Diagnostics.Select(d => d.Message)));

            var registry = new StyleRegistry();
            registry.Add(set);

            var surface = new IxenSurface(root) { Styles = registry };

            root.Invalidate();
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            return surface;
        }

        private static VisualElement Named(string name, LayoutType layout = LayoutType.Column)
        {
            var element = new VisualElement { Name = name };
            element.Styles.Layout = new LayoutStyleDescriptor { Type = layout };

            return element;
        }

        private static VisualElement Child(string xns, out VisualElement box)
        {
            VisualElement root = Named("root");
            VisualElement page = Named("page");

            box = Named("box");

            page.AddChild(box);
            root.AddChild(page);

            Surface(xns, root);

            return root;
        }

        [TestMethod]
        public void AMixedWidthSubtractsThePixelsFromThePercentage()
        {
            Child("page { width: 300px  height: 200px }\r\n"
                + "box { width: calc(100% - 20px)  height: 40px }", out VisualElement box);

            Assert.AreEqual(280f, box.ActualWidth, 0.01f,
                "the percentage resolves against the container at measure time and the pixels "
                + "come off there, which is the whole point of carrying two numbers");
        }

        [TestMethod]
        public void ItFollowsTheContainerWhenItChanges()
        {
            VisualElement root = Named("root");
            VisualElement page = Named("page");
            VisualElement box = Named("box");

            page.AddChild(box);
            root.AddChild(page);

            IxenSurface surface = Surface("page { width: 1*  height: 200px }\r\n"
                + "box { width: calc(50% + 10px)  height: 40px }", root);

            Assert.AreEqual(210f, box.ActualWidth, 0.01f, "half of 400 plus ten");

            surface.ComputeLayout(200, VIEWPORT);

            Assert.AreEqual(110f, box.ActualWidth, 0.01f,
                "and half of 200 plus ten, so nothing was folded at build time");
        }

        [TestMethod]
        public void AMixedHeightWorksTheSameWay()
        {
            Child("page { width: 300px  height: 200px }\r\n"
                + "box { width: 40px  height: calc(100% - 50px) }", out VisualElement box);

            Assert.AreEqual(150f, box.ActualHeight, 0.01f);
        }

        [TestMethod]
        public void ANegativeResultIsRefusedByTheBox()
        {
            Child("page { width: 100px  height: 200px }\r\n"
                + "box { width: calc(10% - 200px)  height: 40px }", out VisualElement box);

            Assert.AreEqual(0f, box.ActualWidth, 0.01f,
                "ten of a hundred minus two hundred is negative, and the width setter refuses "
                + "that, so nothing in the resolution needs a clamp of its own - and a clamp "
                + "there would be wrong for the four offsets, where a negative left is a real "
                + "position");
        }

        [TestMethod]
        public void AnOffsetMayResolveNegative()
        {
            VisualElement root = Named("root");
            VisualElement page = Named("page", LayoutType.Absolute);
            VisualElement box = Named("box");

            page.AddChild(box);
            root.AddChild(page);

            Surface("page { width: 100px  height: 200px }\r\n"
                + "box { left: calc(10% - 40px)  top: 20px  width: 40px  height: 40px }", root);

            Assert.AreEqual(-30f, box.X, 0.01f,
                "ten of a hundred less forty places it thirty units left of its container, "
                + "which is a legitimate thing to ask for");
        }

        [TestMethod]
        public void AnAnchorTakesTheOffsetToo()
        {
            VisualElement root = Named("root");
            VisualElement page = Named("page", LayoutType.Absolute);
            VisualElement box = Named("box");

            page.AddChild(box);
            root.AddChild(page);

            Surface("page { width: 300px  height: 200px }\r\n"
                + "box { left: calc(50% - 10px)  top: 20px  width: 40px  height: 40px }", root);

            Assert.AreEqual(140f, box.X, 0.01f,
                "the four offsets go through the same resolution, so an anchored child is placed "
                + "with the pixels taken off");
        }

        [TestMethod]
        public void AGridTrackTakesItToo()
        {
            VisualElement root = Named("root");
            VisualElement page = Named("page", LayoutType.Grid);
            VisualElement box = Named("box");

            page.AddChild(box);
            root.AddChild(page);

            Surface("page { width: 300px  height: 200px  row-template: calc(50% - 30px) 1* }\r\n"
                + "box { height: 40px }", root);

            Assert.AreEqual(120f, box.ActualWidth, 0.01f,
                "a template entry is an ordinary size descriptor, so the track resolution reads "
                + "the offset with no work of its own");
        }

        [TestMethod]
        public void AMixedSizeSurvivesGeneration()
        {
            var source = new XnsSource("box { width: calc(100% - 20px) }");
            ClassesSet set = source.Compile();

            Assert.IsFalse(source.HasErrors);

            string generated = set.Classes.Single().Styles.Single().ToString();

            var descriptor = (WidthStyleDescriptor)set.Classes.Single().Styles.Single();

            Assert.AreEqual(100f, descriptor.Value);
            Assert.AreEqual(-20f, descriptor.Offset);
            Assert.IsNotNull(generated);
        }

        [TestMethod]
        public void ABoundStillRefusesIt()
        {
            var source = new XnsSource("box { max-width: calc(100% - 20px) }");

            source.Compile();

            Assert.IsTrue(source.HasErrors,
                "a bound is pixels only by design, because it would have to resolve against the "
                + "container and the container size is not in scope where a bound is applied");
        }
    }
}
