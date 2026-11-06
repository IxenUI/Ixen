using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Ixen.Core.UT.Layout
{
    [TestClass]
    public class PrepassSizeTests
    {
        private sealed class Watcher : VisualElement
        {
            internal readonly List<float> Seen = new List<float>();
            internal bool Follows;

            protected internal override bool PrepassFollowsSize => Follows;

            protected internal override void OnPrepass(float viewportWidth, float viewportHeight)
            {
                Seen.Add(ContentWidth);
            }
        }

        private VisualElement _root;
        private Watcher _watcher;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            var holder = new VisualElement { Name = "holder" };
            holder.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Percents, Value = 50 };
            holder.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };

            _watcher = new Watcher { Name = "watcher" };
            holder.AddChild(_watcher);
            _root.AddChild(holder);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
        }

        [TestMethod]
        public void AnElementThatFollowsItsSizeSeesItInTheSameFrame()
        {
            _watcher.Follows = true;

            _surface.ComputeLayout(400, 300);

            Assert.AreEqual(200, _watcher.Seen[_watcher.Seen.Count - 1],
                "the first frame ends with a prepass that knew the measured width");

            _watcher.Seen.Clear();
            _surface.ComputeLayout(600, 300);

            Assert.AreEqual(300, _watcher.Seen[_watcher.Seen.Count - 1],
                "and a resize is seen within the frame that made it, not one frame late");
        }

        [TestMethod]
        public void WithoutItTheFirstPrepassKnowsNothing()
        {
            _surface.ComputeLayout(400, 300);

            Assert.AreEqual(1, _watcher.Seen.Count);
            Assert.AreEqual(0, _watcher.Seen[0], "which is why a virtual list used to guess from the window");
        }

        [TestMethod]
        public void AnUnchangedSizeCostsNoSecondPass()
        {
            _watcher.Follows = true;

            _surface.ComputeLayout(400, 300);

            _watcher.Seen.Clear();
            _watcher.InvalidateLayout();
            _surface.ComputeLayout(400, 300);

            Assert.AreEqual(1, _watcher.Seen.Count, "the size it saw is still its size");
        }
    }
}
