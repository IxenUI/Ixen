using Ixen.Core.Language.Base;
using Ixen.Core.Language.Xns;
using Ixen.Core.Visual.Classes;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
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

            IncrementalValuesProvider<(string name, string content, string path)> namesAndContents = textFiles
                .Select((text, cancellationToken) => (
                    name: Path.GetFileNameWithoutExtension(text.Path),
                    content: text.GetText(cancellationToken)!.ToString(),
                    path: text.Path));

            IncrementalValueProvider<(Compilation, ImmutableArray<(string name, string content, string path)>)> compilationAndNC
                = context.CompilationProvider.Combine(namesAndContents.Collect());

            context.RegisterSourceOutput(compilationAndNC, (spc, source) => Execute(source.Item1, source.Item2, spc));
        }

        private const string DEFAULTS_ATTRIBUTE = "Ixen.Core.Visual.Classes.IxenDefaultStylesAttribute";

        private static bool ShipsDefaultStyles(Compilation compilation)
        {
            INamedTypeSymbol attribute = compilation.GetTypeByMetadataName(DEFAULTS_ATTRIBUTE);

            if (attribute == null)
            {
                return false;
            }

            foreach (AttributeData data in compilation.Assembly.GetAttributes())
            {
                if (SymbolEqualityComparer.Default.Equals(data.AttributeClass, attribute))
                {
                    return true;
                }
            }

            return false;
        }

        private static List<LanguageError> DroppedDefaults(ClassesSet sheet)
        {
            var dropped = new List<LanguageError>();

            foreach (StyleClass styleClass in sheet.Classes)
            {
                if (StyleRegistry.CanBeDefault(styleClass))
                {
                    continue;
                }

                string reason = styleClass.Media != null
                    ? "sits inside an @media block"
                    : "is nested inside another selector";

                dropped.Add(new LanguageError(
                    LanguageErrorCode.DROPPED_DEFAULT,
                    $"This rule {reason}, and this assembly ships its stylesheets as defaults "
                    + "([assembly: IxenDefaultStyles]). A default rule can be neither scoped nor "
                    + "conditional, so this one is dropped and will never apply to anything. "
                    + "Write it at the top level; if it has to change with its parent's state, "
                    + "put the state on the part itself.",
                    styleClass.SourceIndex,
                    styleClass.SourceLength,
                    LanguageErrorSeverity.Warning));
            }

            return dropped;
        }

        static void Execute(Compilation compilation, ImmutableArray<(string name, string content, string path)> texts, SourceProductionContext context)
        {
            Debug.WriteLine("Execute Ixen XNS code generator");

            bool defaults = ShipsDefaultStyles(compilation);

            foreach ((string name, string content, string path) in texts)
            {
                var xnsSource = new XnsSource(content);
                var sheet = xnsSource.Compile();

                Diagnostics.Report(context, path, content, xnsSource.Diagnostics);

                if (sheet == null)
                {
                    continue;
                }

                if (defaults)
                {
                    Diagnostics.Report(context, path, content, DroppedDefaults(sheet));
                }

                var sb = new StringBuilder();

                sb.AppendLine("using Ixen.Core.Visual.Classes;");
                sb.AppendLine("using Ixen.Core.Visual.Styles.Descriptors;");
                sb.AppendLine("using System.Collections.Generic;");
                sb.AppendLine();
                sb.AppendLine("namespace Ixen.StyleSheets");
                sb.AppendLine("{");

                sb.AppendLine($"\tpublic class {name}_StyleSheet : StyleSheet");
                sb.AppendLine("\t{");

                sb.AppendLine("\t\tpublic override int FormatVersion "
                    + "=> global::Ixen.Core.Visual.Classes.StyleFormat.VERSION;");
                sb.AppendLine();

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

                    sb.AppendLine(c.Media != null
                        ? $"\t\t\t}}, global::Ixen.Core.Visual.Classes.MediaQuery.Parse(\"{c.Media.Source}\")));"
                        : "\t\t\t}));");
                    sb.AppendLine();
                }

                foreach (var keyframes in sheet.Keyframes)
                {
                    sb.AppendLine($"\t\t\tAddKeyframes(new KeyframesSet(\"{keyframes.Name}\", new List<Keyframe>()");
                    sb.AppendLine("\t\t\t{");

                    foreach (var frame in keyframes.Frames)
                    {
                        sb.AppendLine($"\t\t\t\tnew Keyframe({frame.Offset.ToString(CultureInfo.InvariantCulture)}f, new List<StyleDescriptor>()");
                        sb.AppendLine("\t\t\t\t{");

                        foreach (var style in frame.Styles)
                        {
                            if (style.CanGenerateSource)
                            {
                                sb.AppendLine($"\t\t\t\t\t{style.ToSource()},");
                            }
                        }

                        sb.AppendLine("\t\t\t\t}),");
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
