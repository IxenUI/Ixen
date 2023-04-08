using Ixen.Core.Visual.Styles;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ixen.Core.Visual.Classes
{
    internal class StyleSheet
    {
        private static Dictionary<string, StyleClass> _globalClassesByName = new();
        private static Dictionary<string, Dictionary<string, StyleClass>> _classesByScopeAndName = new();

        static StyleSheet() {
            ScanForClasses();
        }

        private static StyleClass GetGlobalClass(string name)
        {
            if (_globalClassesByName.TryGetValue(name, out StyleClass value))
            {
                return value;
            }

            return null;
        }

        internal static StyleClass GetClass(string name, string scope = null)
        {
            if (scope != null
                && _classesByScopeAndName.TryGetValue(scope, out Dictionary<string, StyleClass> scopedClasses)
                && scopedClasses.TryGetValue(name, out StyleClass value))
            {
                return value;
            }

            return GetGlobalClass(name);
        }

        private static void ScanForClasses()
        {
            var type = typeof(StyleSheet);

            var sheets = AppDomain.CurrentDomain.GetAssemblies()
                .ToList()
                .SelectMany(x => x.GetTypes())
                .Where(t => type.IsAssignableFrom(t) && t.IsClass && t != type).ToList();

            foreach (var sheet in sheets)
            {
                Activator.CreateInstance(sheet);
            }
        }

        protected void AddGlobalClass(StyleClass styleClass)
        {
            if (!_globalClassesByName.ContainsKey(styleClass.Name))
            {
                _globalClassesByName.Add(styleClass.Name, styleClass); 
            }
            else
            {
                _globalClassesByName[styleClass.Name] = styleClass;
            }
        }

        public string Scope { get; set; }
        public List<StyleClass> Classes { get; set; }
    }
}
