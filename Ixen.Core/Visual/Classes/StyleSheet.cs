﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Ixen.Core.Visual.Classes
{
    internal class StyleSheet
    {
        private static Dictionary<string, StyleClass> _globalClassesByName = new();
        private static Dictionary<string, StyleClass> _globalElementClassesByName = new();
        private static Dictionary<string, StyleClass> _globalTypeClassesByName = new();

        private static Dictionary<string, Dictionary<string, StyleClass>> _classesByScopeAndName = new();
        private static Dictionary<string, Dictionary<string, StyleClass>> _elementClassesByScopeAndName = new();
        private static Dictionary<string, Dictionary<string, StyleClass>> _typeClassesByScopeAndName = new();

        public string Scope { get; set; }
        public List<StyleClass> Classes { get; set; }

        static StyleSheet() {
            ScanForClasses();
        }

        private static StyleClass GetGlobalClass(string name)
        {
            if (name == null)
            {
                return null;
            }

            if (_globalClassesByName.TryGetValue(name, out StyleClass value))
            {
                return value;
            }

            return null;
        }

        internal static StyleClass GetClass(string name, string scope = null)
        {
            if (name == null)
            {
                return null;
            }

            if (scope != null
                && _classesByScopeAndName.TryGetValue(scope, out Dictionary<string, StyleClass> scopedClasses)
                && scopedClasses.TryGetValue(name, out StyleClass value))
            {
                return value;
            }

            return GetGlobalClass(name);
        }

        private static StyleClass GetGlobalElementClass(string name)
        {
            if (name == null)
            {
                return null;
            }

            if (_globalElementClassesByName.TryGetValue(name, out StyleClass value))
            {
                return value;
            }

            return null;
        }

        internal static StyleClass GetTypeClass(string name, string scope = null)
        {
            if (name == null)
            {
                return null;
            }

            if (scope != null
                && _typeClassesByScopeAndName.TryGetValue(scope, out Dictionary<string, StyleClass> scopedClasses)
                && scopedClasses.TryGetValue(name, out StyleClass value))
            {
                return value;
            }

            return GetGlobalTypeClass(name);
        }

        private static StyleClass GetGlobalTypeClass(string name)
        {
            if (name == null)
            {
                return null;
            }

            if (_globalTypeClassesByName.TryGetValue(name, out StyleClass value))
            {
                return value;
            }

            return null;
        }

        internal static StyleClass GetElementClass(string name, string scope = null)
        {
            if (name == null)
            {
                return null;
            }

            if (scope != null
                && _elementClassesByScopeAndName.TryGetValue(scope, out Dictionary<string, StyleClass> scopedClasses)
                && scopedClasses.TryGetValue(name, out StyleClass value))
            {
                return value;
            }

            return GetGlobalElementClass(name);
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

        protected void AddClass(StyleClass styleClass)
        {
            if (styleClass.Scope == null)
            {
                switch (styleClass.Target)
                {
                    case StyleClassTarget.Any:
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
                    case StyleClassTarget.Any:
                        AddScopedClass(_classesByScopeAndName, styleClass);
                        break;
                    case StyleClassTarget.ElementName:
                        AddScopedClass(_elementClassesByScopeAndName, styleClass);
                        break;
                    case StyleClassTarget.ElementType:
                        AddScopedClass(_typeClassesByScopeAndName, styleClass);
                        break;
                }
            }
        }
    }
}