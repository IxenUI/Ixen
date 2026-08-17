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
        private const string BOUND_MODEL_METADATA_NAME = "Ixen.Core.Components.IBoundModel";
        private const string IF_KEYWORD = "if";
        private const string FOREACH_KEYWORD = "foreach";
        private const string IN_KEYWORD = "in";
        private const string KEY_KEYWORD = "key";
        private const string CHANGED_SUFFIX = "Changed";
        private const string CLASS_PROPERTY = "class";
        private const string EACH_PROPERTY = "each";
        private const string KEY_PROPERTY = "key";

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

                AddChildren(body, node, "this", 3, file);

                var members = new StringBuilder();

                AddBindMethod(members, file);

                var sb = new StringBuilder();

                sb.AppendLine("using Ixen.Core;");
                sb.AppendLine("using Ixen.Core.Visual;");
                sb.AppendLine();
                sb.AppendLine($"namespace Ixen.Views");
                sb.AppendLine("{");

                string bases = file.CanBind ? "VisualElement, IBoundView" : "VisualElement";

                sb.AppendLine($"\tpublic class {name} : {bases}");
                sb.AppendLine("\t{");

                if (file.HasModelField)
                {
                    sb.AppendLine($"\t\tprivate global::{file.Model.ToDisplayString()} {XnlBindings.MODEL_FIELD};");
                }

                foreach ((string type, string variable, string initializer) in file.Fields)
                {
                    sb.AppendLine(initializer == null
                        ? $"\t\tprivate readonly {type} {variable};"
                        : $"\t\tprivate readonly {type} {variable} = {initializer};");
                }

                if (file.Fields.Count > 0 || file.HasModelField)
                {
                    sb.AppendLine();
                }

                sb.AppendLine($"\t\tpublic {name}() ");
                sb.AppendLine("\t\t{");
                sb.Append(body);
                sb.AppendLine("\t\t}");
                sb.Append(members);

                sb.AppendLine("\t}");
                sb.AppendLine("}");

                Diagnostics.Report(context, path, content, diagnostics);

                context.AddSource($"{name}.layout.g.cs", sb.ToString());
            }
        }

        static string Identifier(XnlNode node)
            => node.Name != null ? $"el{node.Id}_{node.Name}" : $"el{node.Id}";

        class Region
        {
            internal string ParentId;
            internal string Offset;
            internal string Instances;
            internal string Prefix;
            internal List<XnlNode> Body = new List<XnlNode>();

            internal string Condition;
            internal string Declaration;
            internal string Item;
            internal string Source;
            internal string Key;

            internal bool IsLoop => Item != null;
        }

        class FileContext
        {
            internal readonly TypeResolver Resolver;
            internal readonly List<LanguageError> Diagnostics;
            internal readonly HashSet<int> BoundNodes = new HashSet<int>();
            internal readonly List<(string type, string variable, string initializer)> Fields
                = new List<(string, string, string)>();
            internal readonly List<string> Bindings = new List<string>();
            internal readonly List<Region> Regions = new List<Region>();

            internal INamedTypeSymbol Model;
            internal HashSet<string> ModelMembers;
            internal string RepeatItem;
            internal bool InFactory;
            internal int ModelUses;

            internal FileContext(TypeResolver resolver, List<LanguageError> diagnostics)
            {
                Resolver = resolver;
                Diagnostics = diagnostics;
            }

            internal bool HasBindings => BoundNodes.Count > 0 || Regions.Count > 0;

            internal bool CanBind
                => (Bindings.Count > 0 || Regions.Count > 0 || ModelUses > 0) && Model != null;

            internal bool HasModelField => ModelUses > 0 && Model != null;

            internal bool IsBound(XnlNode node) => BoundNodes.Contains(node.Id);

            internal void Field(string type, string variable, string initializer = null)
                => Fields.Add((type, variable, initializer));
        }

        static void AddRegionBinding(StringBuilder sb, Region region, FileContext file)
        {
            string instances = region.Instances;
            int groupSize = region.Body.Count;

            sb.AppendLine();

            if (!region.IsLoop)
            {
                string condition = XnlBindings.Qualify(region.Condition, file.ModelMembers);

                sb.AppendLine($"\t\t\tRepeater.Sync({region.ParentId}, {instances}, {region.Offset}, " +
                    $"{condition} ? 1 : 0, {groupSize}, Create_{instances});");

                var block = new StringBuilder();

                for (int k = 0; k < groupSize; k++)
                {
                    AddRegionBindings(block, region.Body[k], $"{instances}[{k}]", null, file);
                }

                if (block.Length == 0)
                {
                    return;
                }

                sb.AppendLine();
                sb.AppendLine($"\t\t\tif ({instances}.Count > 0)");
                sb.AppendLine("\t\t\t{");
                sb.Append(block);
                sb.AppendLine("\t\t\t}");

                return;
            }

            sb.AppendLine($"\t\t\tvar {instances}_source = {XnlBindings.Qualify(region.Source, file.ModelMembers)};");
            sb.AppendLine($"\t\t\tint {instances}_count = {instances}_source == null ? 0 : {instances}_source.Count;");

            if (region.Key == null)
            {
                sb.AppendLine($"\t\t\tRepeater.Sync({region.ParentId}, {instances}, {region.Offset}, " +
                    $"{instances}_count, {groupSize}, Create_{instances});");
            }
            else
            {
                sb.AppendLine($"\t\t\t{region.Prefix}_next.Clear();");
                sb.AppendLine();
                sb.AppendLine($"\t\t\tfor (int i = 0; i < {instances}_count; i++)");
                sb.AppendLine("\t\t\t{");
                sb.AppendLine($"\t\t\t\t{region.Declaration} = {instances}_source[i];");
                sb.AppendLine($"\t\t\t\t{region.Prefix}_next.Add(" +
                    $"{XnlBindings.Qualify(region.Key, file.ModelMembers)});");
                sb.AppendLine("\t\t\t}");
                sb.AppendLine();
                sb.AppendLine($"\t\t\tRepeater.SyncKeyed({region.ParentId}, {instances}, {region.Prefix}_keys, " +
                    $"{region.Prefix}_next, {region.Offset}, {groupSize}, Create_{instances});");
            }

            var loop = new StringBuilder();

            for (int k = 0; k < groupSize; k++)
            {
                string index = groupSize == 1 ? "i" : $"i * {groupSize} + {k}";

                AddRegionBindings(loop, region.Body[k], $"{instances}[{index}]", region.Item, file);
            }

            if (loop.Length == 0)
            {
                return;
            }

            sb.AppendLine();
            sb.AppendLine($"\t\t\tfor (int i = 0; i < {instances}_count; i++)");
            sb.AppendLine("\t\t\t{");
            sb.AppendLine($"\t\t\t\t{region.Declaration} = {instances}_source[i];");
            sb.Append(loop);
            sb.AppendLine("\t\t\t}");
        }

        static void AddRegionBindings(StringBuilder sb, XnlNode node, string path, string item, FileContext file)
        {
            INamedTypeSymbol symbol = file.Resolver.Resolve(node, new List<LanguageError>()).Symbol;

            foreach (XnlNodeParameter param in node.Properties)
            {
                if (param.Name == CLASS_PROPERTY || param.Name == KEY_PROPERTY)
                {
                    continue;
                }

                string propertyName = ToPropertyName(param.Name);
                string twoWayPath = XnlBindings.TwoWayPath(param.Value);

                if (twoWayPath != null && item != null)
                {
                    file.Diagnostics.Add(new LanguageError(
                        LanguageErrorCode.INVALID_PROPERTY_VALUE,
                        $"'{param.Name}' cannot be a two-way binding inside a repeated region: the write-back is wired " +
                        $"once when the element is created, where '{item}' is not in scope.",
                        param.ValueIndex,
                        param.Value?.Length ?? 0));

                    continue;
                }

                List<BindingPart> parts = twoWayPath != null
                    ? XnlBindings.PathParts(twoWayPath)
                    : XnlBindings.Parse(param.Value);

                if (twoWayPath == null && !XnlBindings.IsBinding(parts))
                {
                    continue;
                }

                if (twoWayPath == null && symbol != null
                    && FindSettableProperty(symbol, propertyName, out string _) == null)
                {
                    if (item != null && FindEvent(symbol, XnlEvents.Resolve(param.Name, propertyName)) != null)
                    {
                        file.Diagnostics.Add(new LanguageError(
                            LanguageErrorCode.INVALID_PROPERTY_VALUE,
                            $"'{param.Name}' is an event and cannot be bound inside a repeated region: the handler is " +
                            $"wired once when the element is created, where '{item}' is not in scope.",
                            param.ValueIndex,
                            param.Value?.Length ?? 0));
                    }

                    continue;
                }

                sb.AppendLine($"\t\t\t\t{path}.{propertyName} = " +
                    $"{XnlBindings.BuildExpression(parts, file.ModelMembers)};");
            }

            for (int i = 0; i < node.Children.Count; i++)
            {
                if (node.Children[i].IsRegion)
                {
                    continue;
                }

                AddRegionBindings(sb, node.Children[i], $"{path}.Children[{i}]", item, file);
            }
        }

        static void AddRegionFactory(StringBuilder sb, Region region, FileContext file)
        {
            sb.AppendLine();
            sb.AppendLine($"\t\tprivate VisualElement Create_{region.Instances}(int index)");
            sb.AppendLine("\t\t{");

            file.RepeatItem = region.Item;
            file.InFactory = true;

            if (region.Body.Count == 1)
            {
                AddDeclaration(sb, region.Body[0], 3, file, skipBindings: true);
                sb.AppendLine($"\t\t\treturn {Identifier(region.Body[0])};");
            }
            else
            {
                sb.AppendLine("\t\t\tswitch (index)");
                sb.AppendLine("\t\t\t{");

                for (int k = 0; k < region.Body.Count; k++)
                {
                    sb.AppendLine(k == region.Body.Count - 1 ? "\t\t\t\tdefault:" : $"\t\t\t\tcase {k}:");
                    sb.AppendLine("\t\t\t\t{");

                    AddDeclaration(sb, region.Body[k], 5, file, skipBindings: true);

                    sb.AppendLine($"\t\t\t\t\treturn {Identifier(region.Body[k])};");
                    sb.AppendLine("\t\t\t\t}");
                }

                sb.AppendLine("\t\t\t}");
            }

            file.RepeatItem = null;
            file.InFactory = false;

            sb.AppendLine("\t\t}");
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
                if (child.IsRegion)
                {
                    file.BoundNodes.Add(node.Id);
                    continue;
                }

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

            var factories = new StringBuilder();

            for (int i = 0; i < file.Regions.Count; i++)
            {
                AddRegionFactory(factories, file.Regions[i], file);
            }

            var bindings = new StringBuilder();

            foreach (string binding in file.Bindings)
            {
                bindings.AppendLine($"\t\t\t{binding}");
            }

            foreach (Region region in file.Regions)
            {
                AddRegionBinding(bindings, region, file);
            }

            sb.AppendLine();
            sb.AppendLine($"\t\tpublic void Bind({modelType} {XnlBindings.MODEL_PARAMETER})");
            sb.AppendLine("\t\t{");

            if (file.HasModelField)
            {
                sb.AppendLine($"\t\t\t{XnlBindings.MODEL_FIELD} = {XnlBindings.MODEL_PARAMETER};");
                sb.AppendLine();
            }

            sb.Append(bindings);
            sb.AppendLine("\t\t}");
            sb.Append(factories);
            sb.AppendLine();
            sb.AppendLine($"\t\tvoid IBoundView.Bind(object {XnlBindings.MODEL_PARAMETER})");
            sb.AppendLine($"\t\t\t=> Bind(({modelType}){XnlBindings.MODEL_PARAMETER});");
        }

        static void AddDeclaration(StringBuilder sb, XnlNode node, int tabLevel, FileContext file,
            bool skipBindings = false)
        {
            string tabs = new string('\t', tabLevel);
            string nodeId = Identifier(node);
            bool bound = !skipBindings && file.IsBound(node);

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

            AddChildren(sb, node, nodeId, tabLevel, file);

            sb.AppendLine();
        }

        static void AddChildren(StringBuilder sb, XnlNode node, string parentId, int tabLevel, FileContext file)
        {
            string tabs = new string('\t', tabLevel);
            var regions = new List<string>();
            int statics = 0;

            foreach (XnlNode child in node.Children)
            {
                if (child.IsRegion)
                {
                    AddRegion(child, parentId, statics, regions, file);
                    continue;
                }

                AddDeclaration(sb, child, tabLevel, file);
                sb.AppendLine($"{tabs}{parentId}.AddChild({Identifier(child)});");
                sb.AppendLine();

                statics++;
            }
        }

        static void AddRegion(XnlNode node, string parentId, int statics, List<string> regions, FileContext file)
        {
            if (file.InFactory)
            {
                file.Diagnostics.Add(new LanguageError(
                    LanguageErrorCode.INVALID_PROPERTY_VALUE,
                    "a code region cannot appear inside another one yet.",
                    node.CodeIndex,
                    node.Code.Length + 1));

                return;
            }

            string instances = $"{Identifier(node)}_region";

            var region = new Region
            {
                ParentId = parentId,
                Offset = OffsetExpression(statics, regions),
                Instances = instances,
                Prefix = instances
            };

            if (!ParseHeader(node, region, file))
            {
                return;
            }

            foreach (XnlNode body in node.Children)
            {
                if (body.IsRegion)
                {
                    file.Diagnostics.Add(new LanguageError(
                        LanguageErrorCode.INVALID_PROPERTY_VALUE,
                        "a code region cannot be nested in another one yet.",
                        body.CodeIndex,
                        body.Code.Length + 1));

                    continue;
                }

                region.Body.Add(body);
            }

            if (region.Body.Count == 0)
            {
                return;
            }

            file.Regions.Add(region);
            regions.Add(instances);

            file.Field("global::System.Collections.Generic.List<VisualElement>", instances,
                "new global::System.Collections.Generic.List<VisualElement>()");

            if (region.Key != null)
            {
                file.Field("global::System.Collections.Generic.List<object>", $"{region.Prefix}_keys",
                    "new global::System.Collections.Generic.List<object>()");

                file.Field("global::System.Collections.Generic.List<object>", $"{region.Prefix}_next",
                    "new global::System.Collections.Generic.List<object>()");
            }
        }

        static bool ParseHeader(XnlNode node, Region region, FileContext file)
        {
            string code = (node.Code ?? string.Empty).Trim();

            if (StartsWithKeyword(code, IF_KEYWORD))
            {
                string rest = code.Substring(IF_KEYWORD.Length).Trim();

                if (SplitClause(rest, out string condition, out string tail))
                {
                    if (tail.Length == 0)
                    {
                        region.Condition = condition;
                        return true;
                    }

                    file.Diagnostics.Add(new LanguageError(
                        LanguageErrorCode.INVALID_PROPERTY_VALUE,
                        $"'{tail}' does not belong on an '@if': a 'key' clause only means something on a '@foreach'.",
                        node.CodeIndex,
                        code.Length + 1));

                    return false;
                }
            }
            else if (StartsWithKeyword(code, FOREACH_KEYWORD))
            {
                if (ParseForEach(code, region, file, node, out bool reported))
                {
                    return true;
                }

                if (reported)
                {
                    return false;
                }
            }

            file.Diagnostics.Add(new LanguageError(
                LanguageErrorCode.INVALID_PROPERTY_VALUE,
                $"'@{code}' is not a supported code region: only '@if (condition)' and "
                    + "'@foreach (var item in collection) key (expression)' exist today.",
                node.CodeIndex,
                code.Length + 1));

            return false;
        }

        static bool ParseForEach(string code, Region region, FileContext file, XnlNode node, out bool reported)
        {
            reported = false;

            string rest = code.Substring(FOREACH_KEYWORD.Length).Trim();

            if (!SplitClause(rest, out string group, out string tail))
            {
                return false;
            }

            string inner = group.Substring(1, group.Length - 2);
            int split = IndexOfInKeyword(inner);

            if (split < 0)
            {
                return false;
            }

            string declaration = inner.Substring(0, split).Trim();
            string source = inner.Substring(split + IN_KEYWORD.Length).Trim();
            string item = LastIdentifier(declaration);

            if (item == null || source.Length == 0)
            {
                return false;
            }

            if (tail.Length > 0 && !ParseKeyClause(tail, region))
            {
                file.Diagnostics.Add(new LanguageError(
                    LanguageErrorCode.INVALID_PROPERTY_VALUE,
                    $"'{tail}' is not a valid clause: expected 'key (expression)'.",
                    node.CodeIndex,
                    code.Length + 1));

                reported = true;
                return false;
            }

            region.Declaration = declaration;
            region.Item = item;
            region.Source = source;

            return true;
        }

        static bool ParseKeyClause(string tail, Region region)
        {
            if (!StartsWithKeyword(tail, KEY_KEYWORD))
            {
                return false;
            }

            string rest = tail.Substring(KEY_KEYWORD.Length).Trim();

            if (!SplitClause(rest, out string group, out string extra) || extra.Length > 0)
            {
                return false;
            }

            region.Key = group.Substring(1, group.Length - 2).Trim();

            return region.Key.Length > 0;
        }

        static bool SplitClause(string value, out string group, out string tail)
        {
            group = null;
            tail = null;

            if (value.Length < 3 || value[0] != '(')
            {
                return false;
            }

            int depth = 0;

            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];

                if (c == '"' || c == '\'')
                {
                    i = SkipLiteral(value, i);
                    continue;
                }

                if (c == '(')
                {
                    depth++;
                    continue;
                }

                if (c != ')')
                {
                    continue;
                }

                if (--depth > 0)
                {
                    continue;
                }

                group = value.Substring(0, i + 1);
                tail = value.Substring(i + 1).Trim();

                return group.Length > 2;
            }

            return false;
        }

        static bool StartsWithKeyword(string code, string keyword)
        {
            if (!code.StartsWith(keyword, StringComparison.Ordinal))
            {
                return false;
            }

            return code.Length == keyword.Length || !IsIdentifierPart(code[keyword.Length]);
        }

        static int IndexOfInKeyword(string inner)
        {
            int depth = 0;

            for (int i = 0; i < inner.Length; i++)
            {
                char c = inner[i];

                if (c == '"' || c == '\'')
                {
                    i = SkipLiteral(inner, i);
                    continue;
                }

                if (c == '(' || c == '[')
                {
                    depth++;
                    continue;
                }

                if (c == ')' || c == ']')
                {
                    depth--;
                    continue;
                }

                if (depth != 0 || c != IN_KEYWORD[0] || i == 0)
                {
                    continue;
                }

                if (string.CompareOrdinal(inner, i, IN_KEYWORD, 0, IN_KEYWORD.Length) != 0)
                {
                    continue;
                }

                if (!IsIdentifierPart(inner[i - 1])
                    && (i + IN_KEYWORD.Length >= inner.Length || !IsIdentifierPart(inner[i + IN_KEYWORD.Length])))
                {
                    return i;
                }
            }

            return -1;
        }

        static int SkipLiteral(string value, int start)
        {
            char quote = value[start];

            for (int i = start + 1; i < value.Length; i++)
            {
                if (value[i] == '\\')
                {
                    i++;
                    continue;
                }

                if (value[i] == quote)
                {
                    return i;
                }
            }

            return value.Length - 1;
        }

        static string LastIdentifier(string declaration)
        {
            int end = declaration.Length;

            while (end > 0 && char.IsWhiteSpace(declaration[end - 1]))
            {
                end--;
            }

            int start = end;

            while (start > 0 && IsIdentifierPart(declaration[start - 1]))
            {
                start--;
            }

            if (start == end || char.IsDigit(declaration[start]))
            {
                return null;
            }

            return declaration.Substring(start, end - start);
        }

        static bool IsIdentifierPart(char c) => char.IsLetterOrDigit(c) || c == '_';

        static string OffsetExpression(int statics, List<string> regions)
        {
            if (regions.Count == 0)
            {
                return statics.ToString(CultureInfo.InvariantCulture);
            }

            var sb = new StringBuilder(statics.ToString(CultureInfo.InvariantCulture));

            foreach (string instances in regions)
            {
                sb.Append(" + ").Append(instances).Append(".Count");
            }

            return sb.ToString();
        }

        static string ValueOf(XnlNode node, string name)
        {
            foreach (XnlNodeParameter param in node.Properties)
            {
                if (param.Name == name)
                {
                    return param.Value;
                }
            }

            return null;
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

            if (param.Name == EACH_PROPERTY)
            {
                diagnostics.Add(new LanguageError(
                    LanguageErrorCode.INVALID_PROPERTY,
                    "'each' no longer exists: wrap the node in '@foreach (var item in Collection) { … @}' instead.",
                    param.NameIndex,
                    param.Name.Length));

                return;
            }

            if (param.Name == KEY_PROPERTY)
            {
                diagnostics.Add(new LanguageError(
                    LanguageErrorCode.INVALID_PROPERTY,
                    "'key' is no longer a property: it belongs to the iteration, so write "
                        + "'@foreach (var item in Collection) key (item.Id) { … @}'.",
                    param.NameIndex,
                    param.Name.Length));

                return;
            }

            string propertyName = ToPropertyName(param.Name);

            string twoWayPath = XnlBindings.TwoWayPath(param.Value);
            bool twoWay = twoWayPath != null;

            List<BindingPart> parts = twoWay
                ? XnlBindings.PathParts(twoWayPath)
                : XnlBindings.Parse(param.Value);

            bool bound = twoWay || XnlBindings.IsBinding(parts);
            string value = bound ? param.Value : XnlBindings.Unescaped(XnlBindings.LiteralText(parts));

            if (!twoWay && !XnlBindings.IsEscaped(param.Value) && XnlBindings.Inner(param.Value) != null)
            {
                diagnostics.Add(new LanguageError(
                    LanguageErrorCode.INVALID_PROPERTY_VALUE,
                    $"'{XnlBindings.Inner(param.Value)}' is not an assignable member path, so this is not a two-way "
                        + "binding. Double the brackets to mean literal text.",
                    param.ValueIndex,
                    param.Value?.Length ?? 0));

                return;
            }

            if (elementSymbol == null && twoWay)
            {
                diagnostics.Add(new LanguageError(
                    LanguageErrorCode.INVALID_PROPERTY_VALUE,
                    $"a two-way binding needs an element type the generator can resolve, and '{param.Name}' is on one it cannot.",
                    param.ValueIndex,
                    param.Value?.Length ?? 0));

                return;
            }

            if (elementSymbol != null)
            {
                IPropertySymbol property = FindSettableProperty(elementSymbol, propertyName, out string reason);

                if (property == null)
                {
                    string eventName = XnlEvents.Resolve(param.Name, propertyName);

                    if (FindEvent(elementSymbol, eventName) != null)
                    {
                        AddAction(sb, tabs, nodeId, eventName, param, parts, bound, file);
                        return;
                    }

                    diagnostics.Add(new LanguageError(
                        LanguageErrorCode.INVALID_PROPERTY,
                        $"'{param.Name}' does not map to a usable property or event on '{elementSymbol.Name}': {reason}",
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

                if (twoWay && !AddWriteBack(sb, tabs, nodeId, propertyName, property, elementSymbol, param, parts, file))
                {
                    return;
                }
            }

            if (!bound)
            {
                sb.AppendLine($"{tabs}{nodeId}.{propertyName} = {StringLiteral(value)};");
                return;
            }

            if (file.InFactory)
            {
                return;
            }

            AddBinding(nodeId, propertyName, param, parts, file);
        }

        static void AddAction(StringBuilder sb, string tabs, string nodeId, string eventName,
            XnlNodeParameter param, List<BindingPart> parts, bool bound, FileContext file)
        {
            if (!bound || parts.Count != 1)
            {
                file.Diagnostics.Add(new LanguageError(
                    LanguageErrorCode.INVALID_PROPERTY_VALUE,
                    $"'{param.Name}' is an event, so its value must be a single binding expression such as \"{{OnClick()}}\".",
                    param.ValueIndex,
                    param.Value?.Length ?? 0));

                return;
            }

            if (file.RepeatItem != null)
            {
                return;
            }

            if (file.Model == null)
            {
                file.Diagnostics.Add(new LanguageError(
                    LanguageErrorCode.INVALID_PROPERTY_VALUE,
                    $"'{param.Value}' binds to a component, but no single component uses this view.",
                    param.ValueIndex,
                    param.Value?.Length ?? 0));

                return;
            }

            string expression = XnlBindings.Qualify(parts[0].Text, file.ModelMembers, XnlBindings.MODEL_FIELD);

            file.ModelUses++;

            sb.AppendLine($"{tabs}{nodeId}.{eventName} += (sender, e) => {{ if ({XnlBindings.MODEL_FIELD} != null) " +
                $"{{ {expression}; }} }};");
        }

        static bool AddWriteBack(StringBuilder sb, string tabs, string nodeId, string propertyName,
            IPropertySymbol property, INamedTypeSymbol elementSymbol, XnlNodeParameter param,
            List<BindingPart> parts, FileContext file)
        {
            if (file.RepeatItem != null)
            {
                return false;
            }

            string path = parts[0].Text;

            if (!XnlBindings.IsMemberPath(path))
            {
                file.Diagnostics.Add(new LanguageError(
                    LanguageErrorCode.INVALID_PROPERTY_VALUE,
                    $"'{path}' cannot be assigned to, so it cannot be a two-way binding. Use a plain member path such as \"[Name]\".",
                    param.ValueIndex,
                    param.Value?.Length ?? 0));

                return false;
            }

            if (property.GetMethod == null || property.GetMethod.DeclaredAccessibility != Accessibility.Public)
            {
                file.Diagnostics.Add(new LanguageError(
                    LanguageErrorCode.INVALID_PROPERTY_VALUE,
                    $"'{propertyName}' has no public getter, so a two-way binding cannot read its value back.",
                    param.ValueIndex,
                    param.Value?.Length ?? 0));

                return false;
            }

            string eventName = propertyName + CHANGED_SUFFIX;

            if (FindEvent(elementSymbol, eventName) == null)
            {
                file.Diagnostics.Add(new LanguageError(
                    LanguageErrorCode.INVALID_PROPERTY_VALUE,
                    $"a two-way binding on '{param.Name}' needs a public '{eventName}' event on '{elementSymbol.Name}' to know when the value changed, and there is none.",
                    param.ValueIndex,
                    param.Value?.Length ?? 0));

                return false;
            }

            if (file.Model == null)
            {
                file.Diagnostics.Add(new LanguageError(
                    LanguageErrorCode.INVALID_PROPERTY_VALUE,
                    $"'{param.Value}' binds to a component, but no single component uses this view.",
                    param.ValueIndex,
                    param.Value?.Length ?? 0));

                return false;
            }

            string target = XnlBindings.Qualify(path, file.ModelMembers, XnlBindings.MODEL_FIELD);

            file.ModelUses++;

            sb.AppendLine($"{tabs}{nodeId}.{eventName} += (sender, e) => {{ if ({XnlBindings.MODEL_FIELD} != null) " +
                $"{{ {target} = {nodeId}.{propertyName}; " +
                $"((global::{BOUND_MODEL_METADATA_NAME}){XnlBindings.MODEL_FIELD}).SetState(); }} }};");

            return true;
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

            reason = $"no property or event named '{propertyName}' was found.";
            return null;
        }

        static IEventSymbol FindEvent(INamedTypeSymbol type, string eventName)
        {
            for (INamedTypeSymbol current = type; current != null; current = current.BaseType)
            {
                IEventSymbol handler = current.GetMembers(eventName)
                    .OfType<IEventSymbol>()
                    .FirstOrDefault();

                if (handler != null)
                {
                    return handler.DeclaredAccessibility == Accessibility.Public ? handler : null;
                }
            }

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
