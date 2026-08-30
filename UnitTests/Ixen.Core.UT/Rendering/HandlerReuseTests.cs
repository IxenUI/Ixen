using Ixen.Core.Language.Xns;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Ixen.Core.UT.Rendering
{
    [TestClass]
    public class HandlerReuseTests
    {
        private const int VIEWPORT = 400;
        private const int CELLS = 400;
        private const int PASSES = 50;

        private const long BUDGET = 8 * 1024;

        private static IxenSurface Build(string rule, out VisualElement first)
        {
            var registry = new StyleRegistry();
            var sheet = new XnsSource(rule);
            ClassesSet set = sheet.Compile();

            Assert.IsFalse(sheet.HasErrors, string.Join(" | ", sheet.Diagnostics.Select(d => d.Message)));
            registry.Add(set);

            var root = new VisualElement { Name = "root" };
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            first = null;

            for (int index = 0; index < CELLS; index++)
            {
                var cell = new VisualElement { Name = "cell" };

                if (first == null)
                {
                    first = cell;
                }

                root.AddChild(cell);
            }

            return new IxenSurface(root) { Styles = registry };
        }

        private static long PerPass(string rule)
        {
            IxenSurface surface = Build(rule, out _);

            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            long before = System.GC.GetAllocatedBytesForCurrentThread();

            for (int pass = 0; pass < PASSES; pass++)
            {
                surface.Root.Invalidate();
                surface.ComputeLayout(VIEWPORT, VIEWPORT);
            }

            return (System.GC.GetAllocatedBytesForCurrentThread() - before) / PASSES;
        }

        private static long Extra(string rule)
            => PerPass(rule) - PerPass("cell { z-index: 0 }");

        [TestMethod]
        public void RestylingDoesNotRebuildTheBorderHandler()
        {
            long extra = Extra("cell { border: #CCCCCC 1px inner }");

            Assert.IsTrue(extra < BUDGET,
                $"a border rule cost {extra} bytes a pass over {CELLS} cells above a rule that builds "
                + "nothing. Border was the one painting handler with no per-descriptor cache, so every "
                + "element built a fresh Pen - a native SKPaint - on every pass");
        }

        [TestMethod]
        public void RestylingDoesNotRebuildAPlainHandler()
        {
            long extra = Extra("cell { padding: 2px  corner-radius: 3px  font-size: 11px }");

            Assert.IsTrue(extra < BUDGET,
                $"three plain rules cost {extra} bytes a pass over {CELLS} cells. A handler that "
                + "carries nothing but its descriptor is shared through HandlerCache, so a rule matched "
                + "by 400 elements builds one handler rather than 400");
        }

        [TestMethod]
        public void ElementsSharingARuleShareTheHandler()
        {
            IxenSurface surface = Build("cell { width: 12px }", out VisualElement first);

            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            VisualElement second = surface.Root.ChildElements[1];

            Assert.AreSame(first.StylesHandlers.Width, second.StylesHandlers.Width,
                "the cache is keyed on the descriptor, and one rule has one descriptor");
        }

        [TestMethod]
        public void ADescriptorThatIsNullIsStillHandled()
        {
            IxenSurface surface = Build("cell { height: 12px }", out VisualElement first);

            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            first.Styles.Height = null;
            first.Invalidate();

            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.IsNotNull(first.StylesHandlers.Height,
                "the cache is keyed on the descriptor, so a null one has to bypass it rather than throw");
        }

        [TestMethod]
        public void MutatingABorderColourInPlaceIsSeen()
        {
            IxenSurface surface = Build("cell { border: #FF0000 8px inner }", out VisualElement first);

            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            first.StylesHandlers.Border.Descriptor.Color = "#00FF00";

            surface.Root.Invalidate();
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(0, first.StylesHandlers.Border.Color.SKColor.Red,
                "the cached handler derives a Pen from the colour, so reuse has to be validated by "
                + "content and not by the descriptor's identity");
        }

        [TestMethod]
        public void MutatingATextColourInPlaceIsSeen()
        {
            IxenSurface surface = Build("cell { color: #FF0000 }", out VisualElement first);

            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            first.StylesHandlers.Color.Descriptor.Value = "#00FF00";

            surface.Root.Invalidate();
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(0, first.StylesHandlers.Color.Brush.Color.SKColor.Red,
                "same rule for the Brush a colour handler derives");
        }
    }
}
