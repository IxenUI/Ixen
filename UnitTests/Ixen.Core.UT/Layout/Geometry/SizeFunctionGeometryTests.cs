using Ixen.Core.Language.Xns;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Ixen.Core.UT.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Ixen.Core.UT.Layout.Geometry
{
    [TestClass]
    public class SizeFunctionGeometryTests : BaseGeometryTests
    {
        private static SizeStyleDescriptor Parse(string value)
        {
            var source = new XnsSource($"box {{ width: {value} }}");
            ClassesSet set = source.Compile();

            Assert.IsFalse(source.HasErrors,
                string.Join(" | ", source.Diagnostics.Select(d => d.Message)));

            return (SizeStyleDescriptor)set.Classes.Single().Styles.Single();
        }

        private static void AssertRejected(string value)
        {
            var source = new XnsSource($"box {{ width: {value} }}");

            source.Compile();

            Assert.IsTrue(source.HasErrors, $"'{value}' should have been rejected");
        }

        private static float Resolve(string value, float available)
            => Parse(value).Of(available);

        [TestMethod]
        public void MinTakesTheSmallerOnceTheContainerIsKnown()
        {
            Assert.AreEqual(300f, Resolve("min(50%, 300px)", 1000), 0.01f);
            Assert.AreEqual(200f, Resolve("min(50%, 300px)", 400), 0.01f);
        }

        [TestMethod]
        public void MaxTakesTheLarger()
        {
            Assert.AreEqual(500f, Resolve("max(50%, 300px)", 1000), 0.01f);
            Assert.AreEqual(300f, Resolve("max(50%, 300px)", 400), 0.01f);
        }

        [TestMethod]
        public void ClampKeepsTheMiddleValueInsideTheOtherTwo()
        {
            Assert.AreEqual(200f, Resolve("clamp(200px, 50%, 600px)", 100), 0.01f);
            Assert.AreEqual(400f, Resolve("clamp(200px, 50%, 600px)", 800), 0.01f);
            Assert.AreEqual(600f, Resolve("clamp(200px, 50%, 600px)", 2000), 0.01f);
        }

        [TestMethod]
        public void ClampWithAFloorAboveItsCeilingGivesTheFloor()
        {
            Assert.AreEqual(500f, Resolve("clamp(500px, 50%, 300px)", 800), 0.01f,
                "CSS resolves clamp as max(low, min(value, high)), so an inverted pair is not an "
                + "error - the floor wins");
        }

        [TestMethod]
        public void TheyTakeMoreThanTwoArguments()
        {
            Assert.AreEqual(120f, Resolve("min(50%, 300px, 120px)", 1000), 0.01f);
            Assert.AreEqual(500f, Resolve("max(50%, 300px, 120px)", 1000), 0.01f);
        }

        [TestMethod]
        public void AnArgumentMayBeAnExpression()
        {
            Assert.AreEqual(180f, Resolve("min(100% - 20px, 400px)", 200), 0.01f,
                "the same linear form calc folds to, evaluated per argument");

            Assert.AreEqual(400f, Resolve("min(100% - 20px, 400px)", 1000), 0.01f);
        }

        [TestMethod]
        public void AFunctionResolvesAgainstTheContainerLikeAPercentage()
        {
            SizeStyleDescriptor size = Parse("min(50%, 300px)");

            Assert.AreEqual(SizeUnit.Percents, size.Unit,
                "every measure site already resolves Percents through Of(), so reporting that unit "
                + "is what let the seven of them take this for free");

            Assert.AreEqual(SizeFunction.Min, size.Function);
            Assert.AreEqual(2, size.Parts.Count);
        }

        [TestMethod]
        public void AFunctionActuallySizesAnElement()
        {
            VisualElement box = Element("box");
            box.Styles.Width = Width("min(50%, 120px)");
            box.Styles.Height = new HeightStyleDescriptor
            {
                Unit = SizeUnit.Pixels,
                Value = 40
            };

            VisualElement page = Element("page");
            page.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            page.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 400 };
            page.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            page.AddChild(box);

            Layout(page);

            AssertActualSize(box, 120, 40);
        }

        private static WidthStyleDescriptor Width(string value)
        {
            SizeStyleDescriptor parsed = Parse(value);
            var width = new WidthStyleDescriptor();

            width.Set(parsed);

            return width;
        }

        [TestMethod]
        public void SetCarriesTheFunctionAcrossTheTypedVariants()
        {
            WidthStyleDescriptor width = Width("clamp(100px, 50%, 300px)");

            Assert.AreEqual(SizeFunction.Clamp, width.Function);
            Assert.AreEqual(3, width.Parts.Count);
            Assert.AreEqual(200f, width.Of(400), 0.01f);
        }

        [TestMethod]
        public void ClampWantsExactlyThreeArguments()
        {
            AssertRejected("clamp(100px, 50%)");
            AssertRejected("clamp(100px, 50%, 300px, 400px)");
        }

        [TestMethod]
        public void AnEmptyArgumentIsRejected()
        {
            AssertRejected("min(50%, )");
            AssertRejected("min(, 50%)");
            AssertRejected("min()");
        }

        [TestMethod]
        public void NonsenseInsideIsRejected()
        {
            AssertRejected("min(50%, wobble)");
            AssertRejected("min(50%, 300)");
            AssertRejected("min(50%, -300px)");
        }

        [TestMethod]
        public void AnUnknownFunctionIsRejectedRatherThanReadAsAName()
        {
            AssertRejected("wobble(50%, 300px)");
        }

        [TestMethod]
        public void NestingIsRejected()
        {
            AssertRejected("min(max(10px, 20px), 300px)");
        }

        [TestMethod]
        public void AFunctionSurvivesGeneration()
        {
            string source = Width("clamp(100px, 50% - 8px, 300px)").ToSource();

            StringAssert.Contains(source, "Function = SizeFunction.Clamp");

            StringAssert.Contains(source, "Value = 0f, Offset = 100f");
            StringAssert.Contains(source, "Value = 50f, Offset = -8f");
            StringAssert.Contains(source, "Value = 0f, Offset = 300f");
        }

        private static VisualElement Sheet(string content, int available)
        {
            var source = new XnsSource(content);
            ClassesSet set = source.Compile();

            Assert.IsFalse(source.HasErrors,
                string.Join(" | ", source.Diagnostics.Select(d => d.Message)));

            var registry = new StyleRegistry();
            registry.Add(set);

            var box = new VisualElement { Name = "box" };

            var page = new VisualElement { Name = "page" };
            page.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            page.Styles.Width = new WidthStyleDescriptor
            {
                Unit = SizeUnit.Pixels,
                Value = available
            };
            page.Styles.Height = new HeightStyleDescriptor
            {
                Unit = SizeUnit.Pixels,
                Value = 200
            };

            page.AddChild(box);

            var root = new VisualElement { Name = "root" };
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            root.AddChild(page);

            var surface = new IxenSurface(root)
            {
                Styles = registry,
                Scheduler = new FakeScheduler()
            };

            root.Invalidate();
            surface.ComputeLayout(800, 400);

            return box;
        }

        [TestMethod]
        public void ATransitionOnAFunctionSnapsRatherThanCollapsing()
        {
            VisualElement box = Sheet("box {\r\n"
                + "    width: min(50%, 120px)\r\n"
                + "    height: 40px\r\n"
                + "    transition: width 160ms\r\n"
                + "}", 400);

            Assert.AreEqual(120f, box.ActualWidth, 0.01f,
                "a transition carries one scalar, so it cannot hold a function - and if it "
                + "answered anyway the element would measure 0");
        }

        [TestMethod]
        public void AFunctionInAKeyframeStopBuildsNoTrack()
        {
            VisualElement box = Sheet("@keyframes grow {\r\n"
                + "    0%   { width: min(50%, 120px) }\r\n"
                + "    100% { width: 300px }\r\n"
                + "}\r\n"
                + "box {\r\n"
                + "    width: min(50%, 120px)\r\n"
                + "    height: 40px\r\n"
                + "    animation: grow 320ms\r\n"
                + "}", 400);

            Assert.AreEqual(120f, box.ActualWidth, 0.01f,
                "a stop that is a function is not a track, the same rule a stop carrying a pixel "
                + "part already follows - otherwise the width animates from zero");
        }
    }
}
