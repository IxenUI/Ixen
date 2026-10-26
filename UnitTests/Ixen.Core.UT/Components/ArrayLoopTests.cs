using Ixen.Core.UT.Components.Fixtures;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Ixen.Core.UT.Components
{
    [TestClass]
    public class ArrayLoopTests
    {
        private ArrayLoopComponent _component;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _component = new ArrayLoopComponent();
            _surface = new IxenSurface(_component) { Styles = new StyleRegistry() };
            _surface.ComputeLayout(300, 300);
        }

        private string Texts(string parent)
            => string.Join(",", _component.View.FindByName(parent).ChildElements.Select(e => e.Text));

        [TestMethod]
        public void AnArrayIsASource()
        {
            Assert.AreEqual("swans,lake", Texts("names"),
                "an array has Length rather than a public Count, and the generated loop once read Count");

            _component.Update(() => _component.Names = new[] { "notes", "swans", "lake" });
            _surface.ComputeLayout(300, 300);

            Assert.AreEqual("notes,swans,lake", Texts("names"));
        }

        [TestMethod]
        public void AReadOnlyListIsASourceToo()
        {
            Assert.AreEqual("one", Texts("items"));

            _component.Update(() => _component.Items = new[]
            {
                new ListItem { Id = 2, Name = "two" },
                new ListItem { Id = 1, Name = "one" }
            });
            _surface.ComputeLayout(300, 300);

            Assert.AreEqual("two,one", Texts("items"));
        }

        [TestMethod]
        public void AVirtualisingHostCountsAnArrayToo()
        {
            var host = (RecordingHost)_component.View.FindByName("host");

            Assert.AreEqual(2, host.Counter());

            _component.Update(() => _component.Names = new[] { "only" });
            _surface.ComputeLayout(300, 300);

            Assert.AreEqual(1, host.Counter());
        }

        [TestMethod]
        public void ANullSourceIsEmpty()
        {
            _component.Update(() => _component.Names = null);
            _surface.ComputeLayout(300, 300);

            Assert.AreEqual("", Texts("names"));
        }
    }
}
