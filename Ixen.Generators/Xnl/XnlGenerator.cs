using Ixen.Core.Language.Base;
using Ixen.Core.Language.Xnl;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Ixen.Generators.Xnl
{
    [Generator]
    public class XnlGenerator : IIncrementalGenerator
    {
        private const string DEFAULT_ELEMENT_TYPE = "VisualElement";
        private const string VISUAL_ELEMENT_METADATA_NAME = "Ixen.Core.Visual.VisualElement";
        private const string COMPONENT_METADATA_NAME = "Ixen.Core.Components.Component";
        private const string COMPONENT_TYPE_NAME = "Component";
        private const string CLASS_PROPERTY = "class";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<AdditionalText> textFiles = context.AdditionalTextsProvider
                .Where(static file => file.Path.EndsWith(".xnl"));

            IncrementalValuesProvider<(string name, string content, string path)> namesAndContents = textFiles
                .Select((text, cancellationToken) => (
                    name: Path.GetFileNameWithoutExtension(text.Path),
                    content: text.GetText(cancellationToken)!.ToString(),
                    path: text.Path));

            IncrementalValueProvider<(Compilation, ImmutableArray<(string name, string content, string path)>)> compilationAndNC
                = context.CompilationProvider.Combine(namesAndContents.Collect());

            context.RegisterSourceOutput(compilationAndNC, (spc, source) => Execute(source.Item1, source.Item2, spc));
        }

        static void Execute(Compilation compilation, ImmutableArray<(string name, string content, string path)> texts, SourceProductionContext context)
        {
            Debug.WriteLine("Execute Ixen XNL code generator");

            INamedTypeSymbol visualElementSymbol = compilation.GetTypeByMetadataName(VISUAL_ELEMENT_METADATA_NAME);
            INamedTypeSymbol componentSymbol = compilation.GetTypeByMetadataName(COMPONENT_METADATA_NAME);

            foreach ((string name, string content, string path) in texts)
            {
                var xnlSource = new XnlSource(content);
                XnlNode node = xnlSource.Nodify();

                var diagnostics = new List<LanguageError>(xnlSource.Diagnostics);

                if (node == null)
                {
                    Diagnostics.Report(context, path, content, diagnostics);
                    continue;
                }

                var resolver = new TypeResolver(compilation, visualElementSymbol, componentSymbol);
                var file = new FileContext(resolver, diagnostics);

                CollectBoundNodes(node, file);

                if (file.HasBindings)
                {
                    file.Model = resolver.FindModelForView(node, name, diagnostics);
                    file.ModelMembers = file.Model != null ? XnlBindings.MemberNames(file.Model) : null;
                }

                var body = new StringBuilder();

                foreach (XnlNode child in node.Children)
                {
                    AddDeclaration(body, child, 3, file);
                    body.AppendLine($"\t\t\tAddChild({Identifier(child)});");
                }

                var sb = new StringBuilder();

                sb.AppendLine("using Ixen.Core;");
                sb.AppendLine("using Ixen.Core.Visual;");
                sb.AppendLine();
                sb.AppendLine($"namespace Ixen.Views");
                sb.AppendLine("{");

                string bases = file.CanBind ? "VisualElement, IBoundView" : "VisualElement";

                sb.AppendLine($"\tpublic class {name} : {bases}");
                sb.AppendLine("\t{");

                foreach ((string type, string variable) in file.Fields)
                {
                    sb.AppendLine($"\t\tprivate readonly {type} {variable};");
                }

                if (file.Fields.Count > 0)
                {
                    sb.AppendLine();
                }

                sb.AppendLine($"\t\tpublic {name}() ");
                sb.AppendLine("\t\t{");
                sb.Append(body);
                sb.AppendLine("\t\t}");

                AddBindMethod(sb, file);

                sb.AppendLine("\t}");
                sb.AppendLine("}");

                Diagnostics.Report(context, path, content, diagnostics);

                context.AddSource($"{name}.layout.g.cs", sb.ToString());
            }
        }

        static string Identifier(XnlNode node)
            => node.Name != null ? $"el{node.Id}_{node.Name}" : $"el{node.Id}";

        class FileContext
        {
            internal readonly TypeResolver Resolver;
            internal readonly List<LanguageError> Diagnostics;
            internal readonly HashSet<int> BoundNodes = new HashSet<int>();
            internal readonly List<(string type, string variable)> Fields = new List<(string, string)>();
            internal readonly List<string> Bindings = new List<string>();

            internal INamedTypeSymbol Model;
            internal HashSet<string> ModelMembers;

            internal FileContext(TypeResolver resolver, List<LanguageError> diagnostics)
            {
                Resolver = resolver;
                Diagnostics = diagnostics;
            }

            internal bool HasBindings => BoundNodes.Count > 0;
            internal bool CanBind => Bindings.Count > 0 && Model != null;

            internal bool IsBound(XnlNode node) => BoundNodes.Contains(node.Id);

            internal void Field(string type, string variable) => Fields.Add((type, variable));
        }

        static void CollectBoundNodes(XnlNode node, FileContext file)
        {
            foreach (XnlNodeParameter param in node.Properties)
            {
                if (param.Name != CLASS_PROPERTY && XnlBindings.HasBinding(param.Value))
                {
                    file.BoundNodes.Add(node.Id);
                    break;
                }
            }

            foreach (XnlNode child in node.Children)
            {
                CollectBoundNodes(child, file);
            }
        }

        static void AddBindMethod(StringBuilder sb, FileContext file)
        {
            if (!file.CanBind)
            {
                return;
            }

            string modelType = $"global::{file.Model.ToDisplayString()}";

            sb.AppendLine();
            sb.AppendLine($"\t\tpublic void Bind({modelType} {XnlBindings.MODEL_PARAMETER})");
            sb.AppendLine("\t\t{");

            foreach (string binding in file.Bindings)
            {
                sb.AppendLine($"\t\t\t{binding}");
            }

            sb.AppendLine("\t\t}");
            sb.AppendLine();
            sb.AppendLine($"\t\tvoid IBoundView.Bind(object {XnlBindings.MODEL_PARAMETER})");
            sb.AppendLine($"\t\t\t=> Bind(({modelType}){XnlBindings.MODEL_PARAMETER});");
        }

        static void AddDeclaration(StringBuilder sb, XnlNode node, int tabLevel, FileContext file)
        {
            string tabs = new string('\t', tabLevel);
            string nodeId = Identifier(node);
            bool bound = file.IsBound(node);

            ResolvedType resolved = file.Resolver.Resolve(node, file.Diagnostics);

            if (resolved.IsComponent)
            {
                AddComponentDeclaration(sb, node, tabs, nodeId, resolved, file, bound);
            }
            else
            {
                string elementType = resolved.UseDeclaredType ? node.Type : DEFAULT_ELEMENT_TYPE;

                sb.AppendLine($"{tabs}{Declarator(file, bound, elementType, nodeId)} = new {elementType}();");

                if (node.Name != null)
                {
                    sb.AppendLine($"{tabs}{nodeId}.Name = \"{node.Name}\";");
                }

                if (node.Type != null)
                {
                    sb.AppendLine($"{tabs}{nodeId}.TypeName = \"{node.Type}\";");
                }

                foreach (XnlNodeParameter param in node.Properties)
                {
                    AddProperty(sb, tabs, nodeId, param, resolved.Symbol, file);
                }
            }

            if (node.Children.Count > 0)
            {
                sb.AppendLine();
            }

            foreach (XnlNode child in node.Children)
            {
                AddDeclaration(sb, child, tabLevel, file);
                sb.AppendLine($"{tabs}{nodeId}.AddChild({Identifier(child)});");
                sb.AppendLine();
            }

            sb.AppendLine();
        }

        static string Declarator(FileContext file, bool bound, string type, string variable)
        {
            if (!bound)
            {
                return $"var {variable}";
            }

            file.Field(type, variable);
            return variable;
        }

        static void AddComponentDeclaration(StringBuilder sb, XnlNode node, string tabs, string nodeId,
            ResolvedType resolved, FileContext file, bool bound)
        {
            string componentId = $"{nodeId}_component";
            string componentType = resolved.Symbol != null
                ? $"global::{resolved.Symbol.ToDisplayString()}"
                : node.Type;

            sb.AppendLine($"{tabs}{Declarator(file, bound, componentType, componentId)} = new {componentType}();");

            foreach (XnlNodeParameter param in node.Properties)
            {
                if (param.Name == CLASS_PROPERTY)
                {
                    continue;
                }

                AddProperty(sb, tabs, componentId, param, resolved.Symbol, file);
            }

            sb.AppendLine($"{tabs}{Declarator(file, bound, DEFAULT_ELEMENT_TYPE, nodeId)} = {componentId}.Initialize();");

            if (node.Name != null)
            {
                sb.AppendLine($"{tabs}{nodeId}.Name = \"{node.Name}\";");
            }

            foreach (XnlNodeParameter param in node.Properties)
            {
                if (param.Name != CLASS_PROPERTY)
                {
                    continue;
                }

                sb.AppendLine($"{tabs}{nodeId}.Classes.Add({StringLiteral(param.Value)});");
            }
        }

        struct ResolvedType
        {
            internal INamedTypeSymbol Symbol;
            internal bool UseDeclaredType;
            internal bool IsComponent;
        }

        class TypeResolver
        {
            private readonly Compilation _compilation;
            private readonly INamedTypeSymbol _visualElementSymbol;
            private readonly INamedTypeSymbol _componentSymbol;

            internal TypeResolver(Compilation compilation, INamedTypeSymbol visualElementSymbol,
                INamedTypeSymbol componentSymbol)
            {
                _compilation = compilation;
                _visualElementSymbol = visualElementSymbol;
                _componentSymbol = componentSymbol;
            }

            internal ResolvedType Resolve(XnlNode node, List<LanguageError> diagnostics)
            {
                if (node.Type == null)
                {
                    return new ResolvedType { Symbol = _visualElementSymbol };
                }

                INamedTypeSymbol symbol = _compilation.GetTypeByMetadataName($"Ixen.Core.Visual.{node.Type}")
                    ?? _compilation.GetTypeByMetadataName($"Ixen.Core.{node.Type}")
                    ?? _compilation.GetTypeByMetadataName($"Ixen.Views.{node.Type}")
                    ?? _compilation.GetTypeByMetadataName(node.Type);

                if (symbol != null && IsVisualElement(symbol))
                {
                    return Instantiable(node, symbol, diagnostics)
                        ? new ResolvedType { Symbol = symbol, UseDeclaredType = true }
                        : new ResolvedType();
                }

                INamedTypeSymbol component = FindComponent(node, diagnostics);

                if (component != null)
                {
                    return Instantiable(node, component, diagnostics)
                        ? new ResolvedType { Symbol = component, UseDeclaredType = true, IsComponent = true }
                        : new ResolvedType();
                }

                if (symbol == null)
                {
                    return new ResolvedType { UseDeclaredType = true };
                }

                diagnostics.Add(new LanguageError(
                    LanguageErrorCode.INVALID_ELEMENT_TYPE,
                    $"'{node.Type}' is neither a VisualElement nor a Component, so it cannot be used as an XNL element type.",
                    node.TypeIndex,
                    node.Type.Length));

                return new ResolvedType();
            }

            internal INamedTypeSymbol FindModelForView(XnlNode node, string viewName, List<LanguageError> diagnostics)
            {
                if (_componentSymbol == null)
                {
                    return null;
                }

                List<INamedTypeSymbol> candidates = _compilation
                    .GetSymbolsWithName(_ => true, SymbolFilter.Type)
                    .OfType<INamedTypeSymbol>()
                    .Where(s => DerivesFrom(s, _componentSymbol) && ViewNameOf(s) == viewName)
                    .OrderBy(s => s.ToDisplayString(), StringComparer.Ordinal)
                    .ToList();

                if (candidates.Count <= 1)
                {
                    return candidates.FirstOrDefault();
                }

                diagnostics.Add(new LanguageError(
                    LanguageErrorCode.INVALID_PROPERTY_VALUE,
                    $"several components use this view ({string.Join(", ", candidates.Select(c => c.ToDisplayString()))}), so a binding has no single model.",
                    0,
                    0));

                return null;
            }

            private static string ViewNameOf(INamedTypeSymbol component)
            {
                for (INamedTypeSymbol current = component; current != null; current = current.BaseType)
                {
                    if (current.IsGenericType
                        && current.Name == COMPONENT_TYPE_NAME
                        && current.TypeArguments.Length == 1)
                    {
                        return current.TypeArguments[0].Name;
                    }
                }

                return null;
            }

            private bool IsVisualElement(INamedTypeSymbol symbol)
                => _visualElementSymbol == null || DerivesFrom(symbol, _visualElementSymbol);

            private INamedTypeSymbol FindComponent(XnlNode node, List<LanguageError> diagnostics)
            {
                if (_componentSymbol == null)
                {
                    return null;
                }

                List<INamedTypeSymbol> candidates = _compilation
                    .GetSymbolsWithName(name => name == node.Type, SymbolFilter.Type)
                    .OfType<INamedTypeSymbol>()
                    .Where(s => DerivesFrom(s, _componentSymbol))
                    .OrderBy(s => s.ToDisplayString(), StringComparer.Ordinal)
                    .ToList();

                if (candidates.Count > 1)
                {
                    diagnostics.Add(new LanguageError(
                        LanguageErrorCode.INVALID_ELEMENT_TYPE,
                        $"'{node.Type}' matches several components ({string.Join(", ", candidates.Select(c => c.ToDisplayString()))}). Rename one of them.",
                        node.TypeIndex,
                        node.Type.Length));

                    return null;
                }

                return candidates.FirstOrDefault();
            }

            private bool Instantiable(XnlNode node, INamedTypeSymbol symbol, List<LanguageError> diagnostics)
            {
                if (!symbol.IsAbstract && symbol.InstanceConstructors.Any(c =>
                        c.Parameters.Length == 0 && c.DeclaredAccessibility == Accessibility.Public))
                {
                    return true;
                }

                diagnostics.Add(new LanguageError(
                    LanguageErrorCode.INVALID_ELEMENT_TYPE,
                    $"'{node.Type}' has no public parameterless constructor, so XNL cannot instantiate it.",
                    node.TypeIndex,
                    node.Type.Length));

                return false;
            }
        }

        static bool DerivesFrom(INamedTypeSymbol symbol, INamedTypeSymbol baseSymbol)
        {
            for (INamedTypeSymbol current = symbol; current != null; current = current.BaseType)
            {
                if (SymbolEqualityComparer.Default.Equals(current, baseSymbol))
                {
                    return true;
                }
            }

            return false;
        }

        static void AddProperty(StringBuilder sb, string tabs, string nodeId, XnlNodeParameter param,
            INamedTypeSymbol elementSymbol, FileContext file)
        {
            List<LanguageError> diagnostics = file.Diagnostics;

            if (param.Name == CLASS_PROPERTY)
            {
                sb.AppendLine($"{tabs}{nodeId}.Classes.Add({StringLiteral(param.Value)});");
                return;
            }

            string propertyName = ToPropertyName(param.Name);

            List<BindingPart> parts = XnlBindings.Parse(param.Value);
            bool bound = XnlBindings.IsBinding(parts);
            string value = bound ? param.Value : XnlBindings.LiteralText(parts);

            if (elementSymbol != null)
            {
                IPropertySymbol property = FindSettableProperty(elementSymbol, propertyName, out string reason);

                if (property == null)
                {
                    diagnostics.Add(new LanguageError(
                        LanguageErrorCode.INVALID_PROPERTY,
                        $"'{param.Name}' does not map to a usable property on '{elementSymbol.Name}': {reason}",
                        param.NameIndex,
                        param.Name.Length));

                    return;
                }

                if (!bound)
                {
                    if (!TryFormatValue(property.Type, value, out string literal, out string valueReason))
                    {
                        diagnostics.Add(new LanguageError(
                            LanguageErrorCode.INVALID_PROPERTY_VALUE,
                            $"cannot assign '{value}' to '{propertyName}': {valueReason}",
                            param.ValueIndex,
                            param.Value?.Length ?? 0));

                        return;
                    }

                    sb.AppendLine($"{tabs}{nodeId}.{propertyName} = {literal};");
                    return;
                }
            }

            if (!bound)
            {
                sb.AppendLine($"{tabs}{nodeId}.{propertyName} = {StringLiteral(value)};");
                return;
            }

            AddBinding(nodeId, propertyName, param, parts, file);
        }

        static void AddBinding(string nodeId, string propertyName, XnlNodeParameter param,
            List<BindingPart> parts, FileContext file)
        {
            if (file.Model == null)
            {
                file.Diagnostics.Add(new LanguageError(
                    LanguageErrorCode.INVALID_PROPERTY_VALUE,
                    $"'{param.Value}' binds to a component, but no single component uses this view.",
                    param.ValueIndex,
                    param.Value?.Length ?? 0));

                return;
            }

            string expression = XnlBindings.BuildExpression(parts, file.ModelMembers);

            file.Bindings.Add($"{nodeId}.{propertyName} = {expression};");
        }

        static string StringLiteral(string value)
            => SymbolDisplay.FormatLiteral(value ?? string.Empty, true);

        static IPropertySymbol FindSettableProperty(INamedTypeSymbol type, string propertyName, out string reason)
        {
            for (INamedTypeSymbol current = type; current != null; current = current.BaseType)
            {
                IPropertySymbol property = current.GetMembers(propertyName)
                    .OfType<IPropertySymbol>()
                    .FirstOrDefault();

                if (property == null)
                {
                    continue;
                }

                if (property.SetMethod == null || property.SetMethod.DeclaredAccessibility != Accessibility.Public)
                {
                    reason = $"'{propertyName}' has no public setter.";
                    return null;
                }

                reason = null;
                return property;
            }

            reason = $"no property named '{propertyName}' was found.";
            return null;
        }

        static bool TryFormatValue(ITypeSymbol type, string value, out string literal, out string reason)
        {
            literal = null;
            reason = null;

            ITypeSymbol target = type;

            if (target is INamedTypeSymbol named
                && named.IsGenericType
                && named.ConstructedFrom?.SpecialType == SpecialType.System_Nullable_T)
            {
                if (string.IsNullOrEmpty(value))
                {
                    literal = "null";
                    return true;
                }

                target = named.TypeArguments[0];
            }

            if (target.TypeKind == TypeKind.Enum)
            {
                IFieldSymbol member = target.GetMembers()
                    .OfType<IFieldSymbol>()
                    .FirstOrDefault(f => f.IsConst && string.Equals(f.Name, value, StringComparison.OrdinalIgnoreCase));

                if (member == null)
                {
                    reason = $"'{value}' is not a member of enum '{target.Name}'.";
                    return false;
                }

                literal = $"global::{target.ToDisplayString()}.{member.Name}";
                return true;
            }

            switch (target.SpecialType)
            {
                case SpecialType.System_String:
                    literal = StringLiteral(value);
                    return true;

                case SpecialType.System_Boolean:
                    if (!bool.TryParse(value?.Trim(), out bool boolValue))
                    {
                        reason = "expected 'true' or 'false'.";
                        return false;
                    }
                    literal = boolValue ? "true" : "false";
                    return true;

                case SpecialType.System_Char:
                    if (value == null || value.Length != 1)
                    {
                        reason = "expected exactly one character.";
                        return false;
                    }
                    literal = SymbolDisplay.FormatLiteral(value[0], true);
                    return true;

                case SpecialType.System_Byte:
                case SpecialType.System_SByte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                    return TryFormatIntegral(target, value, out literal, out reason);

                case SpecialType.System_Single:
                    return TryFormatFloatingPoint(value, "f", out literal, out reason);

                case SpecialType.System_Double:
                    return TryFormatFloatingPoint(value, "d", out literal, out reason);

                case SpecialType.System_Decimal:
                    if (!decimal.TryParse(value?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out decimal decimalValue))
                    {
                        reason = "expected a decimal number.";
                        return false;
                    }
                    literal = decimalValue.ToString(CultureInfo.InvariantCulture) + "m";
                    return true;
            }

            reason = $"XNL cannot convert a value to '{target.Name}' yet.";
            return false;
        }

        static bool TryFormatIntegral(ITypeSymbol target, string value, out string literal, out string reason)
        {
            literal = null;

            if (!long.TryParse(value?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out long parsed))
            {
                reason = "expected a whole number.";
                return false;
            }

            reason = null;
            literal = $"({target.ToDisplayString()}){parsed.ToString(CultureInfo.InvariantCulture)}";
            return true;
        }

        static bool TryFormatFloatingPoint(string value, string suffix, out string literal, out string reason)
        {
            literal = null;

            if (!double.TryParse(value?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed))
            {
                reason = "expected a number.";
                return false;
            }

            reason = null;
            literal = parsed.ToString("R", CultureInfo.InvariantCulture) + suffix;
            return true;
        }

        static string ToPropertyName(string xnlName)
        {
            if (string.IsNullOrEmpty(xnlName))
            {
                return xnlName;
            }

            var sb = new StringBuilder();
            bool upperNext = true;

            foreach (char c in xnlName)
            {
                if (c == '-')
                {
                    upperNext = true;
                    continue;
                }

                sb.Append(upperNext ? char.ToUpperInvariant(c) : c);
                upperNext = false;
            }

            return sb.ToString();
        }
    }
}
