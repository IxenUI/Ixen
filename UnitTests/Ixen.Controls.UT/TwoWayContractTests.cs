using Ixen.Core.Visual;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Ixen.Controls.UT
{
    [TestClass]
    public class TwoWayContractTests
    {
        private class TwoWayPair
        {
            public Type Owner { get; set; }
            public PropertyInfo Property { get; set; }
            public EventInfo Change { get; set; }

            public override string ToString() => $"{Owner.Name}.{Property.Name}";
        }

        private static IEnumerable<Type> ElementTypes()
        {
            var assemblies = new[] { typeof(VisualElement).Assembly, typeof(Button).Assembly };

            return assemblies
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.IsPublic
                    && !type.IsAbstract
                    && typeof(VisualElement).IsAssignableFrom(type)
                    && type.GetConstructor(Type.EmptyTypes) != null)
                .OrderBy(type => type.Name);
        }

        private static List<TwoWayPair> Pairs()
        {
            var pairs = new List<TwoWayPair>();

            foreach (Type type in ElementTypes())
            {
                foreach (PropertyInfo property in type
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .OrderBy(property => property.Name))
                {
                    if (property.GetGetMethod() == null || property.GetSetMethod() == null)
                    {
                        continue;
                    }

                    EventInfo change = type.GetEvent(property.Name + "Changed");

                    if (change == null)
                    {
                        continue;
                    }

                    pairs.Add(new TwoWayPair { Owner = type, Property = property, Change = change });
                }
            }

            return pairs;
        }

        private static bool TryOtherValue(Type type, object current, out object other)
        {
            if (type == typeof(bool))
            {
                other = !(bool)current;
                return true;
            }

            if (type == typeof(int))
            {
                other = (int)current + 1;
                return true;
            }

            if (type == typeof(float))
            {
                other = (float)current + 1f;
                return true;
            }

            if (type == typeof(string))
            {
                other = (string)current == "a" ? "b" : "a";
                return true;
            }

            if (type == typeof(DateTime?))
            {
                other = current == null ? (DateTime?)new DateTime(2024, 3, 4) : null;
                return true;
            }

            other = null;
            return false;
        }

        [TestMethod]
        public void TheTwoWayPairsAreTheOnesTheDocumentationClaims()
        {
            string[] names = Pairs().Select(pair => pair.ToString()).ToArray();

            CollectionAssert.AreEqual(
                new[]
                {
                    "CheckBox.Checked",
                    "ComboBox.SelectedIndex",
                    "DatePicker.Value",
                    "Dialog.Open",
                    "Menu.Open",
                    "RadioButton.Checked",
                    "Slider.Value",
                    "Switch.Checked",
                    "TabControl.SelectedIndex",
                    "TextArea.Text",
                    "TextField.Text"
                },
                names,
                "a two-way binding needs a settable property and a {Property}Changed event on the "
                + "same element; this is every pair the framework offers today: "
                + string.Join(", ", names));
        }

        [TestMethod]
        public void EveryPairIsOneThisTestKnowsHowToChange()
        {
            var unknown = new List<string>();

            foreach (TwoWayPair pair in Pairs())
            {
                var element = (VisualElement)Activator.CreateInstance(pair.Owner);
                object current = pair.Property.GetValue(element);

                if (!TryOtherValue(pair.Property.PropertyType, current, out object _))
                {
                    unknown.Add($"{pair} is a {pair.Property.PropertyType.Name}");
                }
            }

            Assert.AreEqual(0, unknown.Count,
                "a pair whose type this test cannot vary is a pair it silently does not check: "
                + string.Join(", ", unknown));
        }

        [TestMethod]
        public void AChangeEventNeverFiresOnAnAssignment()
        {
            var offenders = new List<string>();
            int examined = 0;

            foreach (TwoWayPair pair in Pairs())
            {
                var element = (VisualElement)Activator.CreateInstance(pair.Owner);
                object current = pair.Property.GetValue(element);

                if (!TryOtherValue(pair.Property.PropertyType, current, out object other))
                {
                    continue;
                }

                bool raised = false;
                EventHandler<EventArgs> handler = (sender, args) => raised = true;

                pair.Change.AddEventHandler(element, handler);

                pair.Property.SetValue(element, current);
                pair.Property.SetValue(element, other);
                pair.Property.SetValue(element, current);

                pair.Change.RemoveEventHandler(element, handler);

                examined++;

                if (raised)
                {
                    offenders.Add(pair.ToString());
                }
            }

            Assert.AreNotEqual(0, examined, "the reflection found no pair to check at all");

            Assert.AreEqual(0, offenders.Count,
                "a two-way binding replays Bind on every SetState, so an element whose "
                + "{Property}Changed fires on assignment re-enters ApplyBindings and throws. "
                + "These raise on an assignment: " + string.Join(", ", offenders));
        }
    }
}
