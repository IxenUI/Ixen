using Ixen.Core.Visual.Styles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Ixen.Core.Visual.Classes
{
    public sealed class StyleRegistry
    {
        private static Lazy<StyleRegistry> _default =
            new Lazy<StyleRegistry>(CreateFromLoadedAssemblies);

        internal static bool DefaultIsCreated => _default.IsValueCreated;

        internal static void ResetDefault()
            => _default = new Lazy<StyleRegistry>(CreateFromLoadedAssemblies);

        private sealed class ScopedClass
        {
            internal StyleClass Class { get; }
            internal StyleScopeSegment[] Segments { get; }
            internal StyleScopeSegment[] Negations { get; }

            internal ScopedClass(StyleClass styleClass)
            {
                Class = styleClass;
                Segments = StyleScope.Parse(styleClass.Scope);
                Negations = StyleScope.ParseNegations(styleClass.Negations);
            }
        }

        private readonly Dictionary<(StyleClassTarget target, string sheetScope, string name), StyleClass> _unscoped = new();
        private readonly Dictionary<(StyleClassTarget target, string sheetScope, string name), List<ScopedClass>> _scoped = new();
        private readonly Dictionary<(StyleClassTarget target, string sheetScope, string name), List<ScopedClass>> _media = new();
        private readonly Dictionary<(StyleClassTarget target, string name), StyleClass> _defaults = new();
        private readonly Dictionary<string, KeyframesSet> _keyframes = new();
        private readonly Dictionary<(StyleClassTarget target, string sheetScope, string name), List<ScopedClass>> _container = new();
        private readonly List<MediaQuery> _queries = new();
        private readonly List<MediaQuery> _containerQueries = new();

        private int _count;
        private bool _hasStateClasses;
        private bool _hasFocusClasses;
        private StructuralKinds _structural;

        public static StyleRegistry Default => _default.Value;

        public int Count => _count;

        internal bool HasScopedClasses => _scoped.Count > 0;

        internal bool HasStateClasses => _hasStateClasses;

        internal StructuralKinds Structural => _structural;

        internal bool HasStructuralClasses => _structural != StructuralKinds.None;

        internal bool HasFocusClasses => _hasFocusClasses;

        internal bool HasMediaClasses => _media.Count > 0;

        internal bool HasContainerClasses => _container.Count > 0;

        internal bool HasDefaultClasses => _defaults.Count > 0;

        internal bool HasKeyframes => _keyframes.Count > 0;

        private static bool DeclaresFocus(StyleClass styleClass)
            => Mentions(styleClass.Name) || Mentions(styleClass.Scope)
                || Mentions(styleClass.Negations);

        private static bool Mentions(string selector)
            => selector != null
                && selector.IndexOf(StyleScope.STATE_SEPARATOR + Styles.StyleStates.FOCUS) >= 0;

        private static bool DeclaresState(StyleClass styleClass)
            => styleClass.Name.IndexOf(StyleScope.STATE_SEPARATOR) >= 0
                || (styleClass.Scope != null && styleClass.Scope.IndexOf(StyleScope.STATE_SEPARATOR) >= 0)
                || (styleClass.Negations != null && styleClass.Negations.IndexOf(StyleScope.STATE_SEPARATOR) >= 0);

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

            if (!_hasFocusClasses && DeclaresFocus(styleClass))
            {
                _hasFocusClasses = true;
            }

            _structural |= StyleStructural.KindsOf(styleClass.Name)
                | StyleStructural.KindsOf(styleClass.Scope);

            var key = (styleClass.Target, styleClass.SheetScope, styleClass.Name);

            if (styleClass.Container != null)
            {
                AddContainer(key, styleClass);
                return;
            }

            if (styleClass.Media != null)
            {
                AddMedia(key, styleClass);
                return;
            }

            if (styleClass.Scope == null && styleClass.Negations == null)
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
            int existing = candidates.FindIndex(c => c.Class.Scope == styleClass.Scope
                && c.Class.Negations == styleClass.Negations);

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

        private void AddMedia((StyleClassTarget, string, string) key, StyleClass styleClass)
        {
            if (!_media.TryGetValue(key, out List<ScopedClass> candidates))
            {
                candidates = new List<ScopedClass>();
                _media[key] = candidates;
            }

            var entry = new ScopedClass(styleClass);
            int existing = candidates.FindIndex(c => c.Class.Scope == styleClass.Scope
                && c.Class.Negations == styleClass.Negations
                && c.Class.Media.Source == styleClass.Media.Source);

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

            if (!_queries.Exists(q => q.Source == styleClass.Media.Source))
            {
                _queries.Add(styleClass.Media);
            }
        }

        private void AddContainer((StyleClassTarget, string, string) key, StyleClass styleClass)
        {
            if (!_container.TryGetValue(key, out List<ScopedClass> candidates))
            {
                candidates = new List<ScopedClass>();
                _container[key] = candidates;
            }

            var entry = new ScopedClass(styleClass);
            int existing = candidates.FindIndex(c => c.Class.Scope == styleClass.Scope
                && c.Class.Negations == styleClass.Negations
                && c.Class.Container.Source == styleClass.Container.Source
                && c.Class.Media?.Source == styleClass.Media?.Source);

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

            if (!_containerQueries.Exists(q => q.Source == styleClass.Container.Source))
            {
                _containerQueries.Add(styleClass.Container);
            }

            if (styleClass.Media != null && !_queries.Exists(q => q.Source == styleClass.Media.Source))
            {
                _queries.Add(styleClass.Media);
            }
        }

        internal long ContainerSignature(float width, float height)
        {
            long signature = 0;

            for (int index = 0; index < _containerQueries.Count && index < 63; index++)
            {
                if (_containerQueries[index].Matches(width, height))
                {
                    signature |= 1L << index;
                }
            }

            return signature;
        }

        internal void CollectMatchingContainerClasses(StyleClassTarget target, string name,
            VisualElement element, float width, float height, List<StyleClass> result)
        {
            if (name == null || _container.Count == 0)
            {
                return;
            }

            if (!_container.TryGetValue((target, null, name), out List<ScopedClass> candidates))
            {
                return;
            }

            foreach (ScopedClass candidate in candidates)
            {
                StyleClass styleClass = candidate.Class;

                if (styleClass.Media != null && !styleClass.Media.Matches(width, height))
                {
                    continue;
                }

                if (!StyleScope.Holds(candidate.Negations, element))
                {
                    continue;
                }

                if (!StyleScope.Matches(candidate.Segments, element,
                    styleClass.ContainerDepth - 1, out VisualElement container) || container == null)
                {
                    continue;
                }

                container.IsQueryContainer = true;

                if (styleClass.Container.Matches(container.ContentWidth, container.ContentHeight))
                {
                    result.Add(styleClass);
                }
            }
        }

        internal long MediaSignature(float width, float height)
        {
            long signature = 0;

            for (int index = 0; index < _queries.Count && index < 63; index++)
            {
                if (_queries[index].Matches(width, height))
                {
                    signature |= 1L << index;
                }
            }

            return signature;
        }

        internal void CollectMatchingMediaClasses(StyleClassTarget target, string name, VisualElement element,
            float width, float height, List<StyleClass> result)
        {
            if (name == null || _media.Count == 0)
            {
                return;
            }

            if (!_media.TryGetValue((target, null, name), out List<ScopedClass> candidates))
            {
                return;
            }

            foreach (ScopedClass candidate in candidates)
            {
                if (!candidate.Class.Media.Matches(width, height))
                {
                    continue;
                }

                if (StyleScope.Holds(candidate.Negations, element)
                    && StyleScope.Matches(candidate.Segments, element))
                {
                    result.Add(candidate.Class);
                }
            }
        }

        public void Add(KeyframesSet keyframes)
        {
            if (keyframes == null || keyframes.Name == null)
            {
                return;
            }

            _keyframes[keyframes.Name] = keyframes;
        }

        public void AddDefaults(StyleSheet sheet)
        {
            Verify(sheet);
            AddDefaults((ClassesSet)sheet);
        }

        public void AddDefaults(ClassesSet set)
        {
            if (set?.Classes != null)
            {
                foreach (StyleClass styleClass in set.Classes)
                {
                    AddDefault(styleClass);
                }
            }

            AddRange(set?.Keyframes);
        }

        internal static bool CanBeDefault(StyleClass styleClass)
            => styleClass != null && styleClass.Name != null && styleClass.Scope == null
                && styleClass.Media == null && styleClass.Negations == null;

        private void AddDefault(StyleClass styleClass)
        {
            if (!CanBeDefault(styleClass))
            {
                return;
            }

            if (!_hasStateClasses && DeclaresState(styleClass))
            {
                _hasStateClasses = true;
            }

            if (!_hasFocusClasses && DeclaresFocus(styleClass))
            {
                _hasFocusClasses = true;
            }

            _defaults[(styleClass.Target, styleClass.Name)] = styleClass;
        }

        internal StyleClass GetDefault(StyleClassTarget target, string name)
            => name != null && _defaults.TryGetValue((target, name), out StyleClass found) ? found : null;

        public void Add(StyleSheet sheet)
        {
            Verify(sheet);
            Add((ClassesSet)sheet);
        }

        private static void Verify(StyleSheet sheet)
        {
            if (sheet == null || sheet.FormatVersion == StyleFormat.VERSION)
            {
                return;
            }

            throw new System.InvalidOperationException(
                $"{sheet.GetType().FullName} was generated for style format "
                + $"{sheet.FormatVersion}, and this build of Ixen.Core reads format "
                + $"{StyleFormat.VERSION}. The assembly that carries it has to be rebuilt "
                + "against this version of Ixen.Generators - a stylesheet is compiled code, so "
                + "an older one describes its descriptors in a shape this build may read "
                + "differently.");
        }

        public void Add(ClassesSet set)
        {
            AddRange(set?.Classes);
            AddRange(set?.Keyframes);
        }

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

        private void AddRange(List<KeyframesSet> keyframes)
        {
            if (keyframes == null)
            {
                return;
            }

            foreach (KeyframesSet set in keyframes)
            {
                Add(set);
            }
        }

        internal KeyframesSet GetKeyframes(string name)
            => name != null && _keyframes.TryGetValue(name, out KeyframesSet set) ? set : null;

        public void Clear()
        {
            _unscoped.Clear();
            _scoped.Clear();
            _media.Clear();
            _container.Clear();
            _containerQueries.Clear();
            _queries.Clear();
            _keyframes.Clear();
            _count = 0;
            _hasStateClasses = false;
            _hasFocusClasses = false;
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
                if (StyleScope.Holds(candidate.Negations, element)
                    && StyleScope.Matches(candidate.Segments, element))
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

            registry.AddLoadedSheets(false);

            return registry;
        }

        public void AddLoadedDefaults() => AddLoadedSheets(true);

        private void AddLoadedSheets(bool defaultsOnly)
        {
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
                bool isDefault = sheetType.Assembly
                    .IsDefined(typeof(IxenDefaultStylesAttribute), false);

                if (defaultsOnly && !isDefault)
                {
                    continue;
                }

                try
                {
                    if (Activator.CreateInstance(sheetType) is StyleSheet sheet)
                    {
                        if (isDefault)
                        {
                            AddDefaults(sheet);
                        }
                        else
                        {
                            Add(sheet);
                        }
                    }
                }
                catch
                {
                }
            }
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
