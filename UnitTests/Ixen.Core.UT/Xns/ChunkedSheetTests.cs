using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Ixen.StyleSheets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Ixen.Core.UT.Xns
{
    [TestClass]
    public class ChunkedSheetTests
    {
        private const int CLASSES = 47;

        [TestMethod]
        public void EveryClassSurvivesBeingSplitAcrossMethods()
        {
            var sheet = new ManyClasses_StyleSheet();

            Assert.AreEqual(CLASSES, sheet.Classes.Count,
                $"the fixture declares {CLASSES} classes and the generated constructor no longer "
                + "holds them itself: it calls one method per twenty, because a single method with "
                + "a hundred and eighty of them costs the JIT fourteen megabytes to compile once - "
                + "measured, and super-linear in the size of the method rather than in the number "
                + "of classes. This fixture exists because 47 crosses two boundaries and ends "
                + "seven past the last one, and NOTHING in the suite reached twenty before it: an "
                + "off-by-one at a boundary would have dropped or duplicated classes with only the "
                + "demo to notice.");
        }

        [TestMethod]
        public void EachOneKeepsItsOwnValue()
        {
            var sheet = new ManyClasses_StyleSheet();
            var seen = new Dictionary<string, float>();

            foreach (StyleClass styleClass in sheet.Classes)
            {
                Assert.IsFalse(seen.ContainsKey(styleClass.Name),
                    $"{styleClass.Name} was emitted twice");

                foreach (StyleDescriptor descriptor in styleClass.Styles)
                {
                    if (descriptor is WidthStyleDescriptor width)
                    {
                        seen[styleClass.Name] = width.Value;
                    }
                }
            }

            for (int index = 0; index < CLASSES; index++)
            {
                string name = "c" + index;

                Assert.IsTrue(seen.ContainsKey(name), $"{name} is missing");
                Assert.AreEqual(index + 1, seen[name], $"{name} carries the wrong width");
            }
        }
    }
}
