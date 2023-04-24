using Ixen.Core.Language.Xns;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Ixen.Generators.Xns
{
    [Generator]
    public class XnsGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<AdditionalText> textFiles = context.AdditionalTextsProvider
                .Where(static file => file.Path.ToLower()
                .EndsWith(".xns"));

            IncrementalValuesProvider<(string name, string content)> namesAndContents = textFiles
                .Select((text, cancellationToken) => (name: Path.GetFileNameWithoutExtension(text.Path), content: text.GetText(cancellationToken)!.ToString()));

            IncrementalValueProvider<(Compilation, ImmutableArray<(string name, string content)>)> compilationAndNC
                = context.CompilationProvider.Combine(namesAndContents.Collect());

            context.RegisterSourceOutput(compilationAndNC, (spc, source) => Execute(source.Item1, source.Item2, spc));
        }

        static void Execute(Compilation compilation, ImmutableArray<(string name, string content)> texts, SourceProductionContext context)
        {
            //if (!Debugger.IsAttached) { Debugger.Launch(); }

            Debug.WriteLine("Execute Ixen XNS code generator");

            foreach ((string name, string content) in texts)
            {
                var xnsSource = new XnsSource(content);
                var sheet = xnsSource.Compile();

                var sb = new StringBuilder();

                sb.AppendLine("using Ixen.Core.Visual.Classes;");
                sb.AppendLine("using Ixen.Core.Visual.Styles.Descriptors;");
                sb.AppendLine("using System.Collections.Generic;");
                sb.AppendLine();
                sb.AppendLine("namespace Ixen.StyleSheets");
                sb.AppendLine("{");

                sb.AppendLine($"\tpublic class {name}_StyleSheet : StyleSheet");
                sb.AppendLine("\t{");

                sb.AppendLine($"\t\tpublic {name}_StyleSheet() ");
                sb.AppendLine("\t\t{");
                foreach (var c in sheet.Classes)
                {
                    sb.AppendLine($"\t\t\tAddClass(new StyleClass(StyleClassTarget.{c.Target}, " +
                        $"null, " +
                        $"{(!string.IsNullOrWhiteSpace(c.Scope) ? $"\"{c.Scope}\"" : "null")}, " +
                        $"{(!string.IsNullOrWhiteSpace(c.Name) ? $"\"{c.Name}\"" : "null")}, " +
                        $"new List<StyleDescriptor>()");
                    sb.AppendLine("\t\t\t{");

                    foreach (var style in c.Styles)
                    {
                        if (style.CanGenerateSource)
                        {
                            sb.AppendLine($"\t\t\t\t{style.ToSource()},");
                        }
                    }

                    sb.AppendLine("\t\t\t}));");
                    sb.AppendLine();
                }

                sb.AppendLine("\t\t}");
                sb.AppendLine("\t}");
                sb.AppendLine("}");

                context.AddSource($"{name}.styles.g.cs", sb.ToString());
            }
        }
    }
}
