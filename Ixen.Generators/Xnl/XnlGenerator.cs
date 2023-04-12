using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;

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
            //Debug.WriteLine("Execute code generator");

            //var callingEntrypoint = compilation.GetEntryPoint(context.CancellationToken);

            //foreach ((string name, string content) in texts)
            //{
            //    context.AddSource($"{name}.g.cs", content);
            //}
        }
    }
}
