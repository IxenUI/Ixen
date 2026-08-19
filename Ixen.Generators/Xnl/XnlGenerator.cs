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
        private const string FOR_KEYWORD = "for";
        private const string WHILE_KEYWORD = "while";
        private const string ELSE_KEYWORD = "else";
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

        enum RegionKind
        {
            If,
            ForEach,
            For
        }

        class Region
        {
            internal RegionKind Kind;
            internal string ParentId;
            internal int OffsetStatics;
            internal readonly List<string> OffsetRegions = new List<string>();
            internal string Instances;
            internal string Prefix;
            internal string RowType;
            internal Region Owner;
            internal readonly List<Region> Nested = new List<Region>();
            internal readonly List<(string type, string variable, string initializer)> Fields
                = new List<(string, string, string)>();
            internal readonly List<string> Wiring = new List<string>();
            internal readonly List<(string type, string field)> Handlers = new List<(string, string)>();
            internal readonly List<(string field, string body)> Assignments = new List<(string, string)>();
            internal List<XnlNode> Body = new List<XnlNode>();
            internal readonly List<(int before, string statement)> Statements = new List<(int, string)>();

            internal string Condition;
            internal string Guard;
            internal string Test;
            internal bool ClosesChain;
            internal string Declaration;
            internal string Source;
            internal string Key;
            internal string Loop;

            internal bool IsLoop => Kind != RegionKind.If;
        }

        class Emit
        {
            internal string Statement;
            internal Region Region;
        }

        class FileContext
        {
            internal readonly TypeResolver Resolver;
            internal readonly List<LanguageError> Diagnostics;
            internal readonly HashSet<int> BoundNodes = new HashSet<int>();
            internal readonly List<(string type, string variable, string initializer)> Fields
                = new List<(string, string, string)>();
            internal readonly List<Emit> Binds = new List<Emit>();
            internal readonly List<Region> Regions = new List<Region>();

            internal INamedTypeSymbol Model;
            internal HashSet<string> ModelMembers;
            internal Region CurrentRow;
            internal HashSet<int> RowFields;
            internal int ModelUses;

            internal bool InFactory => CurrentRow != null;

            internal FileContext(TypeResolver resolver, List<LanguageError> diagnostics)
            {
                Resolver = resolver;
                Diagnostics = diagnostics;
            }

            internal bool HasBindings => BoundNodes.Count > 0 || Regions.Count > 0 || Binds.Count > 0;

            internal bool CanBind
                => (Binds.Count > 0 || Regions.Count > 0 || ModelUses > 0) && Model != null;

            internal bool HasModelField => ModelUses > 0 && Model != null;

            internal bool IsBound(XnlNode node) => BoundNodes.Contains(node.Id);

            internal void Field(string type, string variable, string initializer = null)
            {
                if (CurrentRow != null)
                {
                    CurrentRow.Fields.Add((type, variable, initializer));
                    return;
                }

                Fields.Add((type, variable, initializer));
            }

            internal void Bind(string statement) => Binds.Add(new Emit { Statement = statement });

            internal void Bind(Region region) => Binds.Add(new Emit { Region = region });
        }

        static string OffsetOf(Region region, string prefix)
        {
            var sb = new StringBuilder(region.OffsetStatics.ToString(CultureInfo.InvariantCulture));

            foreach (string instances in region.OffsetRegions)
            {
                sb.Append(" + ").Append(prefix).Append(instances).Append(".Count");
            }

            return sb.ToString();
        }

        static void AddRegionBinding(StringBuilder sb, Region region, FileContext file, string ownerRow, int tabLevel)
        {
            string prefix = region.Owner == null ? string.Empty : $"{ownerRow}.";
            string tabs = new string('\t', tabLevel);
            string inner = new string('\t', tabLevel + 1);
            string instances = $"{prefix}{region.Instances}";
            string rows = $"{prefix}{region.Prefix}_rows";
            string rowVar = $"{region.Prefix}_row";
            string factory = $"_ => new {region.RowType}()";
            string offset = OffsetOf(region, prefix);
            string counter = $"{region.Prefix}_count";

            sb.AppendLine();

            if (!region.IsLoop)
            {
                sb.AppendLine($"{tabs}bool {region.Test} = {TestExpression(region, file)};");
                sb.AppendLine($"{tabs}Repeater.Sync({prefix}{region.ParentId}, {instances}, {rows}, {offset}, " +
                    $"{region.Test} ? 1 : 0, {factory});");

                var block = new StringBuilder();

                AddRowBindings(block, region, rowVar, file, tabLevel + 1);

                if (block.Length == 0)
                {
                    return;
                }

                sb.AppendLine();
                sb.AppendLine($"{tabs}if ({rows}.Count > 0)");
                sb.AppendLine($"{tabs}{{");
                sb.AppendLine($"{inner}var {rowVar} = {rows}[0];");
                sb.Append(block);
                sb.AppendLine($"{tabs}}}");

                return;
            }

            if (region.Kind == RegionKind.For)
            {
                AddForBinding(sb, region, file, prefix, tabLevel);
                return;
            }

            sb.AppendLine($"{tabs}var {region.Prefix}_source = " +
                $"{XnlBindings.Qualify(region.Source, file.ModelMembers)};");
            sb.AppendLine($"{tabs}int {counter} = {region.Prefix}_source == null ? 0 : {region.Prefix}_source.Count;");

            if (region.Key == null)
            {
                sb.AppendLine($"{tabs}Repeater.Sync({prefix}{region.ParentId}, {instances}, {rows}, {offset}, " +
                    $"{counter}, {factory});");
            }
            else
            {
                sb.AppendLine($"{tabs}{prefix}{region.Prefix}_next.Clear();");
                sb.AppendLine();
                sb.AppendLine($"{tabs}for (int i = 0; i < {counter}; i++)");
                sb.AppendLine($"{tabs}{{");
                sb.AppendLine($"{inner}{region.Declaration} = {region.Prefix}_source[i];");
                sb.AppendLine($"{inner}{prefix}{region.Prefix}_next.Add(" +
                    $"{XnlBindings.Qualify(region.Key, file.ModelMembers)});");
                sb.AppendLine($"{tabs}}}");
                sb.AppendLine();
                sb.AppendLine($"{tabs}Repeater.SyncKeyed({prefix}{region.ParentId}, {instances}, {rows}, " +
                    $"{prefix}{region.Prefix}_keys, {prefix}{region.Prefix}_next, {offset}, {factory});");
            }

            var loop = new StringBuilder();

            AddRowBindings(loop, region, rowVar, file, tabLevel + 1);

            if (loop.Length == 0)
            {
                return;
            }

            sb.AppendLine();
            sb.AppendLine($"{tabs}for (int i = 0; i < {counter}; i++)");
            sb.AppendLine($"{tabs}{{");
            sb.AppendLine($"{inner}{region.Declaration} = {region.Prefix}_source[i];");
            sb.AppendLine($"{inner}var {rowVar} = {rows}[i];");
            sb.Append(loop);
            sb.AppendLine($"{tabs}}}");
        }

        static void AddRowBindings(StringBuilder sb, Region region, string rowVar, FileContext file, int tabLevel)
        {
            string tabs = new string('\t', tabLevel);

            for (int k = 0; k <= region.Body.Count; k++)
            {
                foreach ((int before, string statement) in region.Statements)
                {
                    if (before == k)
                    {
                        sb.AppendLine($"{tabs}{statement}");
                    }
                }

                if (k < region.Body.Count)
                {
                    AddRegionBindings(sb, region.Body[k], rowVar, file, tabLevel);
                }
            }

            foreach ((string field, string body) in region.Assignments)
            {
                sb.AppendLine($"{tabs}{rowVar}.{field} = (sender, e) => {{ {body}; }};");
            }

            foreach (Region nested in region.Nested)
            {
                AddRegionBinding(sb, nested, file, rowVar, tabLevel);
            }
        }

        static string TestExpression(Region region, FileContext file)
        {
            string condition = region.Condition == null
                ? null
                : XnlBindings.Qualify(region.Condition, file.ModelMembers);

            if (region.Guard == null)
            {
                return condition;
            }

            return condition == null ? region.Guard : $"{region.Guard} && {condition}";
        }

        static void AddForBinding(StringBuilder sb, Region region, FileContext file, string prefix, int tabLevel)
        {
            string tabs = new string('\t', tabLevel);
            string inner = new string('\t', tabLevel + 1);
            string instances = $"{prefix}{region.Instances}";
            string rows = $"{prefix}{region.Prefix}_rows";
            string rowVar = $"{region.Prefix}_row";
            string counter = $"{region.Prefix}_index";
            string offset = OffsetOf(region, prefix);

            sb.AppendLine($"{tabs}int {counter} = 0;");
            sb.AppendLine();
            sb.AppendLine($"{tabs}{XnlBindings.Qualify(region.Loop, file.ModelMembers)}");
            sb.AppendLine($"{tabs}{{");
            sb.AppendLine($"{inner}Repeater.Ensure({prefix}{region.ParentId}, {instances}, {rows}, {offset}, " +
                $"{counter} + 1, _ => new {region.RowType}());");
            sb.AppendLine($"{inner}var {rowVar} = {rows}[{counter}];");

            AddRowBindings(sb, region, rowVar, file, tabLevel + 1);

            sb.AppendLine($"{inner}{counter}++;");
            sb.AppendLine($"{tabs}}}");
            sb.AppendLine();
            sb.AppendLine($"{tabs}Repeater.Trim({prefix}{region.ParentId}, {instances}, {rows}, {offset}, " +
                $"{counter});");
        }

        static void CollectRowFields(XnlNode node, HashSet<int> ids)
        {
            foreach (XnlNodeParameter param in node.Properties)
            {
                if (param.Name != CLASS_PROPERTY && XnlBindings.HasBinding(param.Value))
                {
                    ids.Add(node.Id);
                    break;
                }
            }

            foreach (XnlNode child in node.Children)
            {
                if (child.IsCode)
                {
                    ids.Add(node.Id);
                    continue;
                }

                CollectRowFields(child, ids);
            }
        }

        static void AddRegionBindings(StringBuilder sb, XnlNode node, string rowVar, FileContext file, int tabLevel)
        {
            string tabs = new string('\t', tabLevel);
            string path = $"{rowVar}.{Identifier(node)}";
            INamedTypeSymbol symbol = file.Resolver.Resolve(node, new List<LanguageError>()).Symbol;

            foreach (XnlNodeParameter param in node.Properties)
            {
                if (param.Name == CLASS_PROPERTY || param.Name == KEY_PROPERTY)
                {
                    continue;
                }

                string propertyName = ToPropertyName(param.Name);
                string twoWayPath = XnlBindings.TwoWayPath(param.Value);

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
                    continue;
                }

                sb.AppendLine($"{tabs}{path}.{propertyName} = " +
                    $"{XnlBindings.BuildExpression(parts, file.ModelMembers)};");
            }

            foreach (XnlNode child in node.Children)
            {
                if (child.IsStatement)
                {
                    string statement = StatementOf(child, file);

                    if (statement != null)
                    {
                        sb.AppendLine($"{tabs}{statement}");
                    }

                    continue;
                }

                if (child.IsRegion)
                {
                    continue;
                }

                AddRegionBindings(sb, child, rowVar, file, tabLevel);
            }
        }

        static void AddRowClass(StringBuilder sb, Region region, FileContext file)
        {
            Region previousRow = file.CurrentRow;
            HashSet<int> previousFields = file.RowFields;

            var rowFields = new HashSet<int>();

            foreach (XnlNode node in region.Body)
            {
                CollectRowFields(node, rowFields);
            }

            file.CurrentRow = region;
            file.RowFields = rowFields;

            var body = new StringBuilder();

            foreach (XnlNode node in region.Body)
            {
                AddDeclaration(body, node, 4, file, skipBindings: true, forceField: true);
            }

            file.CurrentRow = previousRow;
            file.RowFields = previousFields;

            sb.AppendLine();
            sb.AppendLine($"\t\tprivate sealed class {region.RowType} : IRegionRow");
            sb.AppendLine("\t\t{");

            foreach ((string type, string variable, string initializer) in region.Fields)
            {
                sb.AppendLine(initializer == null
                    ? $"\t\t\tinternal readonly {type} {variable};"
                    : $"\t\t\tinternal readonly {type} {variable} = {initializer};");
            }

            foreach ((string type, string field) in region.Handlers)
            {
                sb.AppendLine($"\t\t\tinternal {type} {field};");
            }

            if (region.Fields.Count > 0 || region.Handlers.Count > 0)
            {
                sb.AppendLine();
            }

            sb.AppendLine($"\t\t\tinternal {region.RowType}()");
            sb.AppendLine("\t\t\t{");
            sb.Append(body);

            foreach (string wiring in region.Wiring)
            {
                sb.AppendLine($"\t\t\t\t{wiring}");
            }

            sb.AppendLine("\t\t\t}");
            sb.AppendLine();
            sb.AppendLine($"\t\t\tpublic int ElementCount => {region.Body.Count};");
            sb.AppendLine();
            sb.AppendLine("\t\t\tpublic VisualElement ElementAt(int index)");
            sb.AppendLine("\t\t\t{");

            if (region.Body.Count == 0)
            {
                sb.AppendLine("\t\t\t\treturn null;");
            }
            else
            {
                sb.AppendLine("\t\t\t\tswitch (index)");
                sb.AppendLine("\t\t\t\t{");

                for (int k = 0; k < region.Body.Count; k++)
                {
                    sb.AppendLine(k == region.Body.Count - 1 ? "\t\t\t\t\tdefault:" : $"\t\t\t\t\tcase {k}:");
                    sb.AppendLine($"\t\t\t\t\t\treturn {Identifier(region.Body[k])};");
                }

                sb.AppendLine("\t\t\t\t}");
            }

            sb.AppendLine("\t\t\t}");
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
                AddRowClass(factories, file.Regions[i], file);
            }

            var bindings = new StringBuilder();

            foreach (Emit emit in file.Binds)
            {
                if (emit.Region != null)
                {
                    AddRegionBinding(bindings, emit.Region, file, null, 3);
                    continue;
                }

                bindings.AppendLine($"\t\t\t{emit.Statement}");
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
            bool skipBindings = false, bool forceField = false)
        {
            string tabs = new string('\t', tabLevel);
            string nodeId = Identifier(node);
            bool bound = forceField
                || (file.RowFields != null && file.RowFields.Contains(node.Id))
                || (!skipBindings && file.IsBound(node));

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

            Region chain = null;

            foreach (XnlNode child in node.Children)
            {
                if (child.IsStatement)
                {
                    AddStatement(child, file);
                    chain = null;
                    continue;
                }

                if (child.IsRegion)
                {
                    chain = AddRegion(child, parentId, statics, regions, file, chain);
                    continue;
                }

                AddDeclaration(sb, child, tabLevel, file);
                sb.AppendLine($"{tabs}{parentId}.AddChild({Identifier(child)});");
                sb.AppendLine();

                chain = null;
                statics++;
            }
        }

        static void AddStatement(XnlNode node, FileContext file)
        {
            if (file.InFactory)
            {
                return;
            }

            string statement = StatementOf(node, file);

            if (statement != null)
            {
                file.Bind(statement);
            }
        }

        static string StatementOf(XnlNode node, FileContext file)
        {
            string code = (node.Code ?? string.Empty).Trim();
            int equals = code.IndexOf('=');
            string declaration = equals < 0 ? null : code.Substring(0, equals).TrimEnd();
            string name = declaration == null ? null : LastIdentifier(declaration);

            if (name == null || !DeclaresLocal(declaration, name))
            {
                file.Diagnostics.Add(new LanguageError(
                    LanguageErrorCode.INVALID_PROPERTY_VALUE,
                    $"'@{code};' is not a supported code statement: only a local declaration such as "
                        + "'@var name = expression;' exists today.",
                    node.CodeIndex,
                    code.Length + 1));

                return null;
            }

            string initializer = XnlBindings.Qualify(code.Substring(equals + 1).Trim(), file.ModelMembers);

            return $"{declaration} = {initializer};";
        }

        static bool DeclaresLocal(string declaration, string name)
        {
            int start = declaration.Length - name.Length;

            return start > 0 && char.IsWhiteSpace(declaration[start - 1]);
        }

        static Region AddRegion(XnlNode node, string parentId, int statics, List<string> regions, FileContext file,
            Region chain)
        {
            string instances = $"{Identifier(node)}_region";

            var region = new Region
            {
                ParentId = parentId,
                Owner = file.CurrentRow,
                OffsetStatics = statics,
                Instances = instances,
                Prefix = instances,
                Test = $"{instances}_test",
                RowType = $"Row_{Identifier(node)}"
            };

            region.OffsetRegions.AddRange(regions);

            if (!ParseHeader(node, region, file, chain))
            {
                return null;
            }

            foreach (XnlNode body in node.Children)
            {
                if (body.IsStatement)
                {
                    string statement = StatementOf(body, file);

                    if (statement != null)
                    {
                        region.Statements.Add((region.Body.Count, statement));
                    }

                    continue;
                }

                if (body.IsRegion)
                {
                    file.Diagnostics.Add(new LanguageError(
                        LanguageErrorCode.INVALID_PROPERTY_VALUE,
                        "a code region needs an element to splice into, so wrap it in one instead of nesting it "
                            + "directly in another region.",
                        body.CodeIndex,
                        body.Code.Length + 1));

                    continue;
                }

                region.Body.Add(body);
            }

            file.Regions.Add(region);
            regions.Add(instances);

            if (region.Owner == null)
            {
                file.Bind(region);
            }
            else
            {
                region.Owner.Nested.Add(region);
            }

            file.Field("global::System.Collections.Generic.List<VisualElement>", instances,
                "new global::System.Collections.Generic.List<VisualElement>()");

            file.Field($"global::System.Collections.Generic.List<{region.RowType}>", $"{region.Prefix}_rows",
                $"new global::System.Collections.Generic.List<{region.RowType}>()");

            if (region.Key != null)
            {
                file.Field("global::System.Collections.Generic.List<object>", $"{region.Prefix}_keys",
                    "new global::System.Collections.Generic.List<object>()");

                file.Field("global::System.Collections.Generic.List<object>", $"{region.Prefix}_next",
                    "new global::System.Collections.Generic.List<object>()");
            }

            return region.Kind == RegionKind.If && !region.ClosesChain ? region : null;
        }

        static bool ParseHeader(XnlNode node, Region region, FileContext file, Region chain)
        {
            string code = (node.Code ?? string.Empty).Trim();

            if (StartsWithKeyword(code, ELSE_KEYWORD))
            {
                return ParseElse(node, region, file, chain, code.Substring(ELSE_KEYWORD.Length).Trim());
            }

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
            else if (StartsWithKeyword(code, WHILE_KEYWORD))
            {
                file.Diagnostics.Add(new LanguageError(
                    LanguageErrorCode.INVALID_PROPERTY_VALUE,
                    "'@while' cannot terminate: a region body holds elements, never statements, so nothing in it can "
                        + "advance the condition. Use '@for' with a counter.",
                    node.CodeIndex,
                    code.Length + 1));

                return false;
            }
            else if (StartsWithKeyword(code, FOR_KEYWORD))
            {
                string rest = code.Substring(FOR_KEYWORD.Length).Trim();

                if (SplitClause(rest, out string header, out string tail))
                {
                    if (tail.Length > 0)
                    {
                        file.Diagnostics.Add(new LanguageError(
                            LanguageErrorCode.INVALID_PROPERTY_VALUE,
                            $"'{tail}' does not belong on a '@for': a key would have to be known before the loop runs, "
                                + "and it is not.",
                            node.CodeIndex,
                            code.Length + 1));

                        return false;
                    }

                    region.Kind = RegionKind.For;
                    region.Loop = $"{FOR_KEYWORD} {header}";

                    return true;
                }
            }

            file.Diagnostics.Add(new LanguageError(
                LanguageErrorCode.INVALID_PROPERTY_VALUE,
                $"'@{code}' is not a supported code region: only '@if (condition)', "
                    + "'@foreach (var item in collection) key (expression)' and '@for (…)' exist today.",
                node.CodeIndex,
                code.Length + 1));

            return false;
        }

        static bool ParseElse(XnlNode node, Region region, FileContext file, Region chain, string rest)
        {
            if (chain == null)
            {
                file.Diagnostics.Add(new LanguageError(
                    LanguageErrorCode.INVALID_PROPERTY_VALUE,
                    "'@else' needs an '@if' or an '@else if' immediately before it, with nothing in between.",
                    node.CodeIndex,
                    node.Code.Length + 1));

                return false;
            }

            region.Guard = chain.Guard == null
                ? $"!{chain.Test}"
                : $"{chain.Guard} && !{chain.Test}";

            if (rest.Length == 0)
            {
                region.ClosesChain = true;
                return true;
            }

            if (StartsWithKeyword(rest, IF_KEYWORD))
            {
                string tail = rest.Substring(IF_KEYWORD.Length).Trim();

                if (SplitClause(tail, out string condition, out string extra) && extra.Length == 0)
                {
                    region.Condition = condition;
                    return true;
                }
            }

            file.Diagnostics.Add(new LanguageError(
                LanguageErrorCode.INVALID_PROPERTY_VALUE,
                $"'@else {rest}' is not valid: write '@else' or '@else if (condition)'.",
                node.CodeIndex,
                node.Code.Length + 1));

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

            region.Kind = RegionKind.ForEach;
            region.Declaration = declaration;
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

                AddClasses(sb, tabs, nodeId, param.Value);
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
                AddClasses(sb, tabs, nodeId, param.Value);
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
                    IEventSymbol handler = FindEvent(elementSymbol, eventName);

                    if (handler != null)
                    {
                        AddAction(sb, tabs, nodeId, eventName, handler, param, parts, bound, file);
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

        static void AddAction(StringBuilder sb, string tabs, string nodeId, string eventName, IEventSymbol handler,
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

            if (file.Model == null)
            {
                file.Diagnostics.Add(new LanguageError(
                    LanguageErrorCode.INVALID_PROPERTY_VALUE,
                    $"'{param.Value}' binds to a component, but no single component uses this view.",
                    param.ValueIndex,
                    param.Value?.Length ?? 0));

                return;
            }

            if (file.CurrentRow != null)
            {
                AddRowHandler(file.CurrentRow, nodeId, eventName, handler,
                    XnlBindings.Qualify(parts[0].Text, file.ModelMembers));

                return;
            }

            string expression = XnlBindings.Qualify(parts[0].Text, file.ModelMembers, XnlBindings.MODEL_FIELD);

            file.ModelUses++;

            sb.AppendLine($"{tabs}{nodeId}.{eventName} += (sender, e) => {{ if ({XnlBindings.MODEL_FIELD} != null) " +
                $"{{ {expression}; }} }};");
        }

        static void AddRowHandler(Region row, string nodeId, string eventName, IEventSymbol handler, string body)
        {
            string field = $"On_{nodeId}_{eventName}";

            row.Handlers.Add((handler.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), field));
            row.Wiring.Add($"{nodeId}.{eventName} += (sender, e) => {field}?.Invoke(sender, e);");
            row.Assignments.Add((field, body));
        }

        static bool AddWriteBack(StringBuilder sb, string tabs, string nodeId, string propertyName,
            IPropertySymbol property, INamedTypeSymbol elementSymbol, XnlNodeParameter param,
            List<BindingPart> parts, FileContext file)
        {
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
            IEventSymbol changed = FindEvent(elementSymbol, eventName);

            if (changed == null)
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

            if (file.CurrentRow != null)
            {
                AddRowHandler(file.CurrentRow, nodeId, eventName, changed,
                    $"{XnlBindings.Qualify(path, file.ModelMembers)} = " +
                    $"(({elementSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})sender)" +
                    $".{propertyName}; " +
                    $"((global::{BOUND_MODEL_METADATA_NAME}){XnlBindings.MODEL_PARAMETER}).SetState()");

                return true;
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

            file.Bind($"{nodeId}.{propertyName} = {expression};");
        }

        static string StringLiteral(string value)
            => SymbolDisplay.FormatLiteral(value ?? string.Empty, true);

        static void AddClasses(StringBuilder sb, string tabs, string nodeId, string value)
        {
            foreach (string name in XnlClasses.Split(value))
            {
                sb.AppendLine($"{tabs}{nodeId}.Classes.Add({StringLiteral(name)});");
            }
        }

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
