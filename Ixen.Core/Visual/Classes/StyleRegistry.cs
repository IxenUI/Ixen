using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Ixen.Core.Visual.Classes
{
    public sealed class StyleRegistry
    {
        private static readonly Lazy<StyleRegistry> _default =
            new Lazy<StyleRegistry>(CreateFromLoadedAssemblies);

        private readonly Dictionary<(StyleClassTarget target, string sheetScope, string scope, string name), StyleClass> _classes = new();

        public static StyleRegistry Default => _default.Value;

        public int Count => _classes.Count;

        internal bool HasScopedClasses { get; private set; }

        public void Add(StyleClass styleClass)
        {
            if (styleClass == null)
            {
                return;
            }

            if (styleClass.Scope != null)
            {
                HasScopedClasses = true;
            }

            _classes[Key(styleClass.Target, styleClass.SheetScope, styleClass.Scope, styleClass.Name)] = styleClass;
        }

        public void Add(StyleSheet sheet)
        {
            if (sheet?.Classes == null)
            {
                return;
            }

            foreach (StyleClass styleClass in sheet.Classes)
            {
                Add(styleClass);
            }
        }

        public void Add(ClassesSet set)
        {
            if (set?.Classes == null)
            {
                return;
            }

            foreach (StyleClass styleClass in set.Classes)
            {
                Add(styleClass);
            }
        }

        public void Clear()
        {
            _classes.Clear();
            HasScopedClasses = false;
        }

        internal StyleClass GetGlobalClass(string name)
            => Get(StyleClassTarget.ClassName, null, null, name);

        internal StyleClass GetGlobalClass(string name, string scope)
            => scope == null ? null : Get(StyleClassTarget.ClassName, null, scope, name);

        internal StyleClass GetClass(string name, string sheetScope)
            => sheetScope == null ? null : Get(StyleClassTarget.ClassName, sheetScope, null, name);

        internal StyleClass GetClass(string name, string sheetScope, string scope)
            => sheetScope == null || scope == null ? null : Get(StyleClassTarget.ClassName, sheetScope, scope, name);

        internal StyleClass GetGlobalElementClass(string name)
            => Get(StyleClassTarget.ElementName, null, null, name);

        internal StyleClass GetGlobalElementClass(string name, string scope)
            => scope == null ? null : Get(StyleClassTarget.ElementName, null, scope, name);

        internal StyleClass GetElementClass(string name, string sheetScope)
            => sheetScope == null ? null : Get(StyleClassTarget.ElementName, sheetScope, null, name);

        internal StyleClass GetElementClass(string name, string sheetScope, string scope)
            => sheetScope == null || scope == null ? null : Get(StyleClassTarget.ElementName, sheetScope, scope, name);

        internal StyleClass GetGlobalTypeClass(string name)
            => Get(StyleClassTarget.ElementType, null, null, name);

        internal StyleClass GetGlobalTypeClass(string name, string scope)
            => scope == null ? null : Get(StyleClassTarget.ElementType, null, scope, name);

        internal StyleClass GetTypeClass(string name, string sheetScope)
            => sheetScope == null ? null : Get(StyleClassTarget.ElementType, sheetScope, null, name);

        internal StyleClass GetTypeClass(string name, string sheetScope, string scope)
            => sheetScope == null || scope == null ? null : Get(StyleClassTarget.ElementType, sheetScope, scope, name);

        private StyleClass Get(StyleClassTarget target, string sheetScope, string scope, string name)
        {
            if (name == null)
            {
                return null;
            }

            return _classes.TryGetValue(Key(target, sheetScope, scope, name), out StyleClass value)
                ? value
                : null;
        }

        private static (StyleClassTarget, string, string, string) Key(StyleClassTarget target, string sheetScope, string scope, string name)
            => (target, sheetScope, scope, name);

        public static StyleRegistry CreateFromLoadedAssemblies()
        {
            var registry = new StyleRegistry();
            Type baseType = typeof(StyleSheet);

            IEnumerable<Type> sheetTypes = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(GetTypesSafely)
                .Where(t => baseType.IsAssignableFrom(t)
                    && t.IsClass
                    && !t.IsAbstract
                    && t != baseType
                    && t.GetConstructor(Type.EmptyTypes) != null);

            foreach (Type sheetType in sheetTypes)
            {
                try
                {
                    if (Activator.CreateInstance(sheetType) is StyleSheet sheet)
                    {
                        registry.Add(sheet);
                    }
                }
                catch
                {
                }
            }

            return registry;
        }

        private static IEnumerable<Type> GetTypesSafely(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(t => t != null);
            }
            catch
            {
                return Enumerable.Empty<Type>();
            }
        }
    }
}
