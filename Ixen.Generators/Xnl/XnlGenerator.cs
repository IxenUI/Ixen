using Ixen.Core.Language.Xnl;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Ixen.Generators.Xnl
{
    [Generator]
    public class XnlGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<AdditionalText> textFiles = context.AdditionalTextsProvider
                .Where(static file => file.Path.EndsWith(".xnl"));

            IncrementalValuesProvider<(string name, string content)> namesAndContents = textFiles
                .Select((text, cancellationToken) => (name: Path.GetFileNameWithoutExtension(text.Path), content: text.GetText(cancellationToken)!.ToString()));

            IncrementalValueProvider<(Compilation, ImmutableArray<(string name, string content)>)> compilationAndNC
                = context.CompilationProvider.Combine(namesAndContents.Collect());

            context.RegisterSourceOutput(compilationAndNC, (spc, source) => Execute(source.Item1, source.Item2, spc));
        }

        static void Execute(Compilation compilation, ImmutableArray<(string name, string content)> texts, SourceProductionContext context)
        {
            //if (!Debugger.IsAttached) { Debugger.Launch(); }

            Debug.WriteLine("Execute Ixen XNL code generator");

            foreach ((string name, string content) in texts)
            {
                var xnlSource = new XnlSource(content);
                var node = xnlSource.Nodify();

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

                foreach (var child in node.Children)
                {
                    string childId = child.Name != null ? $"el{child.Id}_{child.Name}" : $"el{child.Id}";
                    AddDeclaration(sb, child, 3);
                    sb.AppendLine($"\t\t\tAddChild({childId});");
                }

                sb.AppendLine("\t\t}");
                sb.AppendLine("\t}");
                sb.AppendLine("}");

                context.AddSource($"{name}.layout.g.cs", sb.ToString());
            }
        }

        static void AddDeclaration(StringBuilder sb, XnlNode node, int tabLevel)
        {
            string tabs = new string('\t', tabLevel);
            string nodeId = node.Name != null ? $"el{node.Id}_{node.Name}" : $"el{node.Id}";
            string childId;

            sb.AppendLine($"{tabs}var {nodeId} = new VisualElement();");

            foreach (var param in node.Properties)
            {
                switch (param.Name)
                {
                    case "class":
                        sb.AppendLine($"{tabs}{nodeId}.Classes.Add(\"{param.Value}\");");
                        break;
                }
            }

            if (node.Children.Count > 0)
            {
                sb.AppendLine();
            }

            foreach (var child in node.Children)
            {
                childId = child.Name != null ? $"el{child.Id}_{child.Name}" : $"el{child.Id}";
                AddDeclaration(sb, child, tabLevel);
                sb.AppendLine($"{tabs}{nodeId}.AddChild({childId});");
                sb.AppendLine();
            }

            sb.AppendLine();
        }
    }
}
