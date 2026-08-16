using Ixen.Core.Language.Xns;
using Ixen.Core.Visual.Styles;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Ixen.Core.UT.Xns
{
    [TestClass]
    public class StyleDefinitionsTests
    {
        private static bool Compiles(string name, string value)
        {
            var source = new XnsSource($"el {{ {name}: {value} }}");
            source.Compile();

            return !source.HasErrors;
        }

        [TestMethod]
        public void EveryDeclaredValueActuallyParses()
        {
            var broken = new List<string>();

            foreach (StyleDefinition definition in StyleDefinitions.All)
            {
                foreach (string value in definition.Values)
                {
                    if (!Compiles(definition.Name, value))
                    {
                        broken.Add($"{definition.Name}: {value}");
                    }
                }
            }

            Assert.AreEqual(0, broken.Count,
                "the registry offers values its own parser rejects: " + string.Join(", ", broken));
        }

        [TestMethod]
        public void EveryDefinitionIsReachableByName()
        {
            foreach (StyleDefinition definition in StyleDefinitions.All)
            {
                Assert.AreSame(definition, StyleDefinitions.Find(definition.Name), definition.Name);
                Assert.AreSame(definition, StyleDefinitions.Find(definition.Name.ToUpper()),
                    "a lookup is case-insensitive, like the compiler was");
            }
        }

        [TestMethod]
        public void TheRegistryCoversEveryStyleTheCompilerAccepts()
        {
            List<string> identifiers = typeof(StyleIdentifier)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.IsLiteral)
                .Select(f => (string)f.GetRawConstantValue())
                .ToList();

            var missing = identifiers
                .Where(id => StyleDefinitions.Find(id) == null)
                .ToList();

            CollectionAssert.AreEquivalent(
                new[] { StyleIdentifier.COLUMN_INDEX, StyleIdentifier.ROW_INDEX },
                missing,
                "only the two index identifiers have no parser; anything else missing is a style XNS silently rejects");
        }

        [TestMethod]
        public void AnUnknownNameIsNotFound()
        {
            Assert.IsNull(StyleDefinitions.Find("wobble"));
            Assert.IsNull(StyleDefinitions.Find(null));
            Assert.AreEqual(0, StyleDefinitions.ValuesOf("wobble").Count);
        }

        [TestMethod]
        public void AFreeFormStyleOffersNoKeywords()
        {
            Assert.AreEqual(0, StyleDefinitions.ValuesOf(StyleIdentifier.WIDTH).Count,
                "a size is not a closed list, so completion has nothing to propose");

            Assert.IsTrue(StyleDefinitions.ValuesOf(StyleIdentifier.LAYOUT).Count > 0,
                "but an enum-valued style does");
        }

        [TestMethod]
        public void TheNamesAreWhatTheDocumentationClaims()
        {
            Assert.AreEqual(25, StyleDefinitions.All.Count,
                string.Join(", ", StyleDefinitions.All.Select(d => d.Name)));
        }
    }
}
