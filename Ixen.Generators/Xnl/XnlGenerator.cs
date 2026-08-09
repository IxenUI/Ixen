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

                var sb = new StringBuilder();

                sb.AppendLine("using Ixen.Core;");
                sb.AppendLine("using Ixen.Core.Visual;");
                sb.AppendLine();
                sb.AppendLine($"namespace Ixen.Views");
                sb.AppendLine("{");

                sb.AppendLine($"\tpublic class {name} : VisualElement");
                sb.AppendLine("\t{");

                sb.AppendLine($"\t\tpublic {name}() ");
                sb.AppendLine("\t\t{");

                foreach (XnlNode child in node.Children)
                {
                    AddDeclaration(sb, child, 3, compilation, visualElementSymbol, diagnostics);
                    sb.AppendLine($"\t\t\tAddChild({Identifier(child)});");
                }

                sb.AppendLine("\t\t}");
                sb.AppendLine("\t}");
                sb.AppendLine("}");

                Diagnostics.Report(context, path, content, diagnostics);

                context.AddSource($"{name}.layout.g.cs", sb.ToString());
            }
        }

        static string Identifier(XnlNode node)
            => node.Name != null ? $"el{node.Id}_{node.Name}" : $"el{node.Id}";

        static void AddDeclaration(StringBuilder sb, XnlNode node, int tabLevel, Compilation compilation,
            INamedTypeSymbol visualElementSymbol, List<LanguageError> diagnostics)
        {
            string tabs = new string('\t', tabLevel);
            string nodeId = Identifier(node);
            INamedTypeSymbol elementSymbol = ResolveElementType(node, compilation, visualElementSymbol, diagnostics,
                out bool useDeclaredType);

            string elementType = useDeclaredType ? node.Type : DEFAULT_ELEMENT_TYPE;

            sb.AppendLine($"{tabs}var {nodeId} = new {elementType}();");

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
                AddProperty(sb, tabs, nodeId, param, elementSymbol, diagnostics);
            }

            if (node.Children.Count > 0)
            {
                sb.AppendLine();
            }

            foreach (XnlNode child in node.Children)
            {
                AddDeclaration(sb, child, tabLevel, compilation, visualElementSymbol, diagnostics);
                sb.AppendLine($"{tabs}{nodeId}.AddChild({Identifier(child)});");
                sb.AppendLine();
            }

            sb.AppendLine();
        }

        // The probed namespaces mirror what the generated file can see: its own usings plus its own namespace.
        static INamedTypeSymbol ResolveElementType(XnlNode node, Compilation compilation,
            INamedTypeSymbol visualElementSymbol, List<LanguageError> diagnostics, out bool useDeclaredType)
        {
            if (node.Type == null)
            {
                useDeclaredType = false;
                return visualElementSymbol;
            }

            INamedTypeSymbol symbol = compilation.GetTypeByMetadataName($"Ixen.Core.Visual.{node.Type}")
                ?? compilation.GetTypeByMetadataName($"Ixen.Core.{node.Type}")
                ?? compilation.GetTypeByMetadataName($"Ixen.Views.{node.Type}")
                ?? compilation.GetTypeByMetadataName(node.Type);

            if (symbol == null)
            {
                // Most likely a view generated in this same compilation, which we cannot see from here.
                // Emit it verbatim and let the C# compiler have the final word.
                useDeclaredType = true;
                return null;
            }

            if (visualElementSymbol != null && !DerivesFrom(symbol, visualElementSymbol))
            {
                diagnostics.Add(new LanguageError(
                    LanguageErrorCode.INVALID_ELEMENT_TYPE,
                    $"'{node.Type}' is not a VisualElement, so it cannot be used as an XNL element type.",
                    node.TypeIndex,
                    node.Type.Length));

                useDeclaredType = false;
                return null;
            }

            if (symbol.IsAbstract || !symbol.InstanceConstructors.Any(c =>
                    c.Parameters.Length == 0 && c.DeclaredAccessibility == Accessibility.Public))
            {
                diagnostics.Add(new LanguageError(
                    LanguageErrorCode.INVALID_ELEMENT_TYPE,
                    $"'{node.Type}' has no public parameterless constructor, so XNL cannot instantiate it.",
                    node.TypeIndex,
                    node.Type.Length));

                useDeclaredType = false;
                return null;
            }

            useDeclaredType = true;
            return symbol;
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
            INamedTypeSymbol elementSymbol, List<LanguageError> diagnostics)
        {
            if (param.Name == CLASS_PROPERTY)
            {
                sb.AppendLine($"{tabs}{nodeId}.Classes.Add({StringLiteral(param.Value)});");
                return;
            }

            string propertyName = ToPropertyName(param.Name);

            if (elementSymbol == null)
            {
                // No symbol to validate against, so assume a string and let the C# compiler decide.
                sb.AppendLine($"{tabs}{nodeId}.{propertyName} = {StringLiteral(param.Value)};");
                return;
            }

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

            if (!TryFormatValue(property.Type, param.Value, out string literal, out string valueReason))
            {
                diagnostics.Add(new LanguageError(
                    LanguageErrorCode.INVALID_PROPERTY_VALUE,
                    $"cannot assign '{param.Value}' to '{propertyName}': {valueReason}",
                    param.ValueIndex,
                    param.Value?.Length ?? 0));

                return;
            }

            sb.AppendLine($"{tabs}{nodeId}.{propertyName} = {literal};");
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
