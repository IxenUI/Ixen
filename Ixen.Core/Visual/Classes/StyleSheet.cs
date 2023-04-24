using System;
using System.Collections.Generic;
using System.Linq;

namespace Ixen.Core.Visual.Classes
{
    public class ClassesSet
    {
        public string Scope { get; set; }
        public List<StyleClass> Classes { get; set; }
    }

    public class StyleSheet : ClassesSet
    {
        private static Dictionary<string, StyleClass> _globalClassesByName = new();
        private static Dictionary<string, StyleClass> _globalElementClassesByName = new();
        private static Dictionary<string, StyleClass> _globalTypeClassesByName = new();

        private static Dictionary<string, Dictionary<string, StyleClass>> _globalClassesByScopeAndName = new();
        private static Dictionary<string, Dictionary<string, StyleClass>> _globalElementClassesByScopeAndName = new();
        private static Dictionary<string, Dictionary<string, StyleClass>> _globalTypeClassesByScopeAndName = new();

        private static Dictionary<string, Dictionary<string, StyleClass>> _classesBySheetScopeAndName = new();
        private static Dictionary<string, Dictionary<string, StyleClass>> _elementClassesBySheetScopeAndName = new();
        private static Dictionary<string, Dictionary<string, StyleClass>> _typeClassesBySheetScopeAndName = new();

        private static Dictionary<string, Dictionary<string, Dictionary<string, StyleClass>>> _classesBySheetScopeScopeAndName = new();
        private static Dictionary<string, Dictionary<string, Dictionary<string, StyleClass>>> _elementClassesBySheetScopeScopeAndName = new();
        private static Dictionary<string, Dictionary<string, Dictionary<string, StyleClass>>> _typeClassesBySheetScopeScopeAndName = new();

        static StyleSheet() {
            ScanForClasses();
        }

        private static void ScanForClasses()
        {
            var type = typeof(StyleSheet);
            var sheets = AppDomain.CurrentDomain
                .GetAssemblies()
                .ToList()
                .SelectMany(x => x.GetTypes())
                .Where(t => type.IsAssignableFrom(t) && t.IsClass && t != type)
                .ToList();

            foreach (var sheet in sheets)
            {
                Activator.CreateInstance(sheet);
            }
        }

        internal static StyleClass GetGlobalClass(string name)
        {
            if (name != null && _globalClassesByName.TryGetValue(name, out StyleClass value))
            {
                return value;
            }

            return null;
        }

        internal static StyleClass GetGlobalClass(string name, string scope)
        {
            if (name != null && scope != null
                && _globalClassesByScopeAndName.TryGetValue(scope, out Dictionary<string, StyleClass> scopedClasses)
                && scopedClasses.TryGetValue(name, out StyleClass value))
            {
                return value;
            }

            return null;
        }

        internal static StyleClass GetClass(string name, string sheetScope)
        {
            if (name != null && sheetScope != null
                && _classesBySheetScopeAndName.TryGetValue(sheetScope, out Dictionary<string, StyleClass> classes)
                && classes.TryGetValue(name, out StyleClass value))
            {
                return value;
            }

            return null;
        }

        internal static StyleClass GetClass(string name, string sheetScope, string scope)
        {
            if (name != null && sheetScope != null && scope != null
                && _classesBySheetScopeScopeAndName.TryGetValue(sheetScope, out Dictionary<string, Dictionary<string, StyleClass>> scopedClasses)
                && scopedClasses.TryGetValue(scope, out Dictionary<string, StyleClass> classes)
                && classes.TryGetValue(name, out StyleClass value))
            {
                return value;
            }

            return null;
        }

        internal static StyleClass GetGlobalElementClass(string name)
        {
            if (name != null && _globalElementClassesByName.TryGetValue(name, out StyleClass value))
            {
                return value;
            }

            return null;
        }

        internal static StyleClass GetGlobalElementClass(string name, string scope)
        {
            if (name != null && scope != null
                && _globalElementClassesByScopeAndName.TryGetValue(scope, out Dictionary<string, StyleClass> scopedClasses)
                && scopedClasses.TryGetValue(name, out StyleClass value))
            {
                return value;
            }

            return null;
        }

        internal static StyleClass GetElementClass(string name, string sheetScope)
        {
            if (name != null && sheetScope != null
                && _elementClassesBySheetScopeAndName.TryGetValue(sheetScope, out Dictionary<string, StyleClass> classes)
                && classes.TryGetValue(name, out StyleClass value))
            {
                return value;
            }

            return null;
        }

        internal static StyleClass GetElementClass(string name, string sheetScope, string scope)
        {
            if (name != null && sheetScope != null && scope != null
                && _elementClassesBySheetScopeScopeAndName.TryGetValue(sheetScope, out Dictionary<string, Dictionary<string, StyleClass>> scopedClasses)
                && scopedClasses.TryGetValue(scope, out Dictionary<string, StyleClass> classes)
                && classes.TryGetValue(name, out StyleClass value))
            {
                return value;
            }

            return null;
        }

        internal static StyleClass GetGlobalTypeClass(string name)
        {
            if (name != null && _globalTypeClassesByName.TryGetValue(name, out StyleClass value))
            {
                return value;
            }

            return null;
        }

        internal static StyleClass GetGlobalTypeClass(string name, string scope)
        {
            if (name != null && scope != null
                && _globalTypeClassesByScopeAndName.TryGetValue(scope, out Dictionary<string, StyleClass> scopedClasses)
                && scopedClasses.TryGetValue(name, out StyleClass value))
            {
                return value;
            }

            return null;
        }

        internal static StyleClass GetTypeClass(string name, string sheetScope)
        {
            if (name != null && sheetScope != null
                && _typeClassesBySheetScopeAndName.TryGetValue(sheetScope, out Dictionary<string, StyleClass> classes)
                && classes.TryGetValue(name, out StyleClass value))
            {
                return value;
            }

            return null;
        }

        internal static StyleClass GetTypeClass(string name, string sheetScope, string scope)
        {
            if (name != null && sheetScope != null && scope != null
                && _typeClassesBySheetScopeScopeAndName.TryGetValue(sheetScope, out Dictionary<string, Dictionary<string, StyleClass>> scopedClasses)
                && scopedClasses.TryGetValue(scope, out Dictionary<string, StyleClass> classes)
                && classes.TryGetValue(name, out StyleClass value))
            {
                return value;
            }

            return null;
        }

        private void AddClass(Dictionary<string, StyleClass> dic, StyleClass styleClass)
        {
            if (!dic.ContainsKey(styleClass.Name))
            {
                dic.Add(styleClass.Name, styleClass);
            }
            else
            {
                dic[styleClass.Name] = styleClass;
            }
        }

        private void AddScopedClass(Dictionary<string, Dictionary<string, StyleClass>> dic, StyleClass styleClass)
        {
            if (!dic.ContainsKey(styleClass.Scope))
            {
                dic.Add(styleClass.Scope, new());
            }

            AddClass(dic[styleClass.Scope], styleClass);
        }

        private void AddSheetScopedClass(Dictionary<string, Dictionary<string, StyleClass>> dic, StyleClass styleClass)
        {
            if (!dic.ContainsKey(styleClass.SheetScope))
            {
                dic.Add(styleClass.SheetScope, new());
            }

            AddClass(dic[styleClass.SheetScope], styleClass);
        }

        private void AddSheetScopedScopedClass(Dictionary<string, Dictionary<string, Dictionary<string, StyleClass>>> dic, StyleClass styleClass)
        {
            if (!dic.ContainsKey(styleClass.SheetScope))
            {
                dic.Add(styleClass.SheetScope, new());
            }

            AddScopedClass(dic[styleClass.SheetScope], styleClass);
        }

        protected void AddClass(StyleClass styleClass)
        {
            if (styleClass.SheetScope == null)
            {
                if (styleClass.Scope == null)
                {
                    switch (styleClass.Target)
                    {
                        case StyleClassTarget.ClassName:
                            AddClass(_globalClassesByName, styleClass);
                            break;
                        case StyleClassTarget.ElementName:
                            AddClass(_globalElementClassesByName, styleClass);
                            break;
                        case StyleClassTarget.ElementType:
                            AddClass(_globalTypeClassesByName, styleClass);
                            break;
                    }
                }
                else
                {
                    switch (styleClass.Target)
                    {
                        case StyleClassTarget.ClassName:
                            AddScopedClass(_globalClassesByScopeAndName, styleClass);
                            break;
                        case StyleClassTarget.ElementName:
                            AddScopedClass(_globalElementClassesByScopeAndName, styleClass);
                            break;
                        case StyleClassTarget.ElementType:
                            AddScopedClass(_globalTypeClassesByScopeAndName, styleClass);
                            break;
                    }
                }
            }
            else
            {
                if (styleClass.Scope == null)
                {
                    switch (styleClass.Target)
                    {
                        case StyleClassTarget.ClassName:
                            AddSheetScopedClass(_classesBySheetScopeAndName, styleClass);
                            break;
                        case StyleClassTarget.ElementName:
                            AddSheetScopedClass(_elementClassesBySheetScopeAndName, styleClass);
                            break;
                        case StyleClassTarget.ElementType:
                            AddSheetScopedClass(_typeClassesBySheetScopeAndName, styleClass);
                            break;
                    }
                }
                else
                {
                    switch (styleClass.Target)
                    {
                        case StyleClassTarget.ClassName:
                            AddSheetScopedScopedClass(_classesBySheetScopeScopeAndName, styleClass);
                            break;
                        case StyleClassTarget.ElementName:
                            AddSheetScopedScopedClass(_elementClassesBySheetScopeScopeAndName, styleClass);
                            break;
                        case StyleClassTarget.ElementType:
                            AddSheetScopedScopedClass(_typeClassesBySheetScopeScopeAndName, styleClass);
                            break;
                    }
                }
            }
        }
    }
}
