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

        private sealed class ScopedClass
        {
            internal StyleClass Class { get; }
            internal StyleScopeSegment[] Segments { get; }

            internal ScopedClass(StyleClass styleClass)
            {
                Class = styleClass;
                Segments = StyleScope.Parse(styleClass.Scope);
            }
        }

        private readonly Dictionary<(StyleClassTarget target, string sheetScope, string name), StyleClass> _unscoped = new();
        private readonly Dictionary<(StyleClassTarget target, string sheetScope, string name), List<ScopedClass>> _scoped = new();

        private int _count;
        private bool _hasStateClasses;

        public static StyleRegistry Default => _default.Value;

        public int Count => _count;

        internal bool HasScopedClasses => _scoped.Count > 0;

        internal bool HasStateClasses => _hasStateClasses;

        private static bool DeclaresState(StyleClass styleClass)
            => styleClass.Name.IndexOf(StyleScope.STATE_SEPARATOR) >= 0
                || (styleClass.Scope != null && styleClass.Scope.IndexOf(StyleScope.STATE_SEPARATOR) >= 0);

        public void Add(StyleClass styleClass)
        {
            if (styleClass == null || styleClass.Name == null)
            {
                return;
            }

            if (!_hasStateClasses && DeclaresState(styleClass))
            {
                _hasStateClasses = true;
            }

            var key = (styleClass.Target, styleClass.SheetScope, styleClass.Name);

            if (styleClass.Scope == null)
            {
                if (!_unscoped.ContainsKey(key))
                {
                    _count++;
                }

                _unscoped[key] = styleClass;
                return;
            }

            if (!_scoped.TryGetValue(key, out List<ScopedClass> candidates))
            {
                candidates = new List<ScopedClass>();
                _scoped[key] = candidates;
            }

            var entry = new ScopedClass(styleClass);
            int existing = candidates.FindIndex(c => c.Class.Scope == styleClass.Scope);

            if (existing >= 0)
            {
                candidates[existing] = entry;
            }
            else
            {
                candidates.Add(entry);
                _count++;
            }

            candidates.Sort((a, b) => a.Segments.Length.CompareTo(b.Segments.Length));
        }

        public void Add(StyleSheet sheet) => AddRange(sheet?.Classes);

        public void Add(ClassesSet set) => AddRange(set?.Classes);

        private void AddRange(List<StyleClass> classes)
        {
            if (classes == null)
            {
                return;
            }

            foreach (StyleClass styleClass in classes)
            {
                Add(styleClass);
            }
        }

        public void Clear()
        {
            _unscoped.Clear();
            _scoped.Clear();
            _count = 0;
            _hasStateClasses = false;
        }

        internal StyleClass GetGlobal(StyleClassTarget target, string name)
            => GetUnscoped(target, null, name);

        internal StyleClass GetGlobalClass(string name)
            => GetUnscoped(StyleClassTarget.ClassName, null, name);

        internal StyleClass GetClass(string name, string sheetScope)
            => sheetScope == null ? null : GetUnscoped(StyleClassTarget.ClassName, sheetScope, name);

        internal StyleClass GetGlobalElementClass(string name)
            => GetUnscoped(StyleClassTarget.ElementName, null, name);

        internal StyleClass GetElementClass(string name, string sheetScope)
            => sheetScope == null ? null : GetUnscoped(StyleClassTarget.ElementName, sheetScope, name);

        internal StyleClass GetGlobalTypeClass(string name)
            => GetUnscoped(StyleClassTarget.ElementType, null, name);

        internal StyleClass GetTypeClass(string name, string sheetScope)
            => sheetScope == null ? null : GetUnscoped(StyleClassTarget.ElementType, sheetScope, name);

        internal void CollectMatchingScopedClasses(StyleClassTarget target, string name, VisualElement element,
            List<StyleClass> result)
        {
            if (name == null || _scoped.Count == 0)
            {
                return;
            }

            if (!_scoped.TryGetValue((target, null, name), out List<ScopedClass> candidates))
            {
                return;
            }

            foreach (ScopedClass candidate in candidates)
            {
                if (StyleScope.Matches(candidate.Segments, element))
                {
                    result.Add(candidate.Class);
                }
            }
        }

        internal StyleClass GetScopedClass(StyleClassTarget target, string name, string sheetScope, string scope)
        {
            if (name == null || scope == null
                || !_scoped.TryGetValue((target, sheetScope, name), out List<ScopedClass> candidates))
            {
                return null;
            }

            return candidates.FirstOrDefault(c => c.Class.Scope == scope)?.Class;
        }

        private StyleClass GetUnscoped(StyleClassTarget target, string sheetScope, string name)
        {
            if (name == null)
            {
                return null;
            }

            return _unscoped.TryGetValue((target, sheetScope, name), out StyleClass value)
                ? value
                : null;
        }

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
