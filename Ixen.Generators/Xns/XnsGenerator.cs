using Ixen.Core.Language.Xns;
using Ixen.Core.Visual.Styles;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Ixen.Generators.Xnl
{
    [Generator]
    public class XnsGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<AdditionalText> textFiles = context.AdditionalTextsProvider
                .Where(static file => file.Path.ToLower().EndsWith(".xns"));

            IncrementalValuesProvider<(string name, string content)> namesAndContents = textFiles
                .Select((text, cancellationToken) => (name: Path.GetFileNameWithoutExtension(text.Path), content: text.GetText(cancellationToken)!.ToString()));

            IncrementalValueProvider<(Compilation, ImmutableArray<(string name, string content)>)> compilationAndNC
                = context.CompilationProvider.Combine(namesAndContents.Collect());

            context.RegisterSourceOutput(compilationAndNC, (spc, source) => Execute(source.Item1, source.Item2, spc));
        }

        static void Execute(Compilation compilation, ImmutableArray<(string name, string content)> texts, SourceProductionContext context)
        {
            //if (!Debugger.IsAttached)
            //{
            //    Debugger.Launch();
            //}

            var callingEntrypoint = compilation.GetEntryPoint(context.CancellationToken);

            foreach ((string name, string content) in texts)
            {
                var xnsSource = XnsSource.FromSource(content);
                var sheet = xnsSource.Compile();

                var sb = new StringBuilder();

                sb.AppendLine("using Ixen.Core; ");
                sb.AppendLine("using Ixen.Core.Visual.Classes;");
                sb.AppendLine("using Ixen.Core.Visual.Styles;");
                sb.AppendLine("using Ixen.Core.Visual.Styles.Descriptors;");
                sb.AppendLine("using System.Collections.Generic;");
                sb.AppendLine();
                sb.AppendLine("namespace Ixen.Generated.StyleSheets");
                sb.AppendLine("{");

                sb.AppendLine($"\tpublic class {name}_StyleSheet : StyleSheet");
                sb.AppendLine("\t{");

                sb.AppendLine($"\t\tpublic {name}_StyleSheet() ");
                sb.AppendLine("\t\t{");

                foreach (var c in sheet.Classes)
                {
                    sb.AppendLine($"\t\t\tAddClass(new StyleClass(\"{c.Name}\", new List<StyleDescriptor>()");
                    sb.AppendLine("\t\t\t{");

                    foreach (var style in c.Styles)
                    {
                        switch (style.Identifier)
                        {
                            case StyleIdentifier.Background:
                                var backgroundStyle = (BackgroundStyleDescriptor)style;
                                sb.AppendLine($"\t\t\t\tnew BackgroundStyleDescriptor {{ Color = \"{backgroundStyle.Color}\" }},");
                                break;

                            case StyleIdentifier.Border:
                                var borderStyle = (BorderStyleDescriptor)style;
                                sb.AppendLine($"\t\t\t\tnew BorderStyleDescriptor {{ Color = \"{borderStyle.Color}\", Thickness = {borderStyle.Thickness}, Type = {borderStyle.Type} }},");
                                break;

                            case StyleIdentifier.Height:
                                var heightStyle = (HeightStyleDescriptor)style;
                                sb.AppendLine($"\t\t\t\tnew HeightStyleDescriptor {{ Unit = SizeUnit.{heightStyle.Unit}, Value = {heightStyle.Value} }},");
                                break;

                            case StyleIdentifier.Layout:
                                var layoutStyle = (LayoutStyleDescriptor)style;
                                sb.AppendLine($"\t\t\t\tnew LayoutStyleDescriptor {{ Type = LayoutType.{layoutStyle.Type} }},");
                                break;

                            case StyleIdentifier.Margin:
                                var marginStyle = (MarginStyleDescriptor)style;
                                sb.AppendLine($"\t\t\t\tnew MarginStyleDescriptor {{");
                                sb.AppendLine($"\t\t\t\t\tTop = new SizeStyleDescriptor {{ Unit = SizeUnit.{marginStyle.Top.Unit}, Value = {marginStyle.Top.Value} }},");
                                sb.AppendLine($"\t\t\t\t\tRight = new SizeStyleDescriptor {{ Unit = SizeUnit.{marginStyle.Right.Unit}, Value = {marginStyle.Right.Value} }},");
                                sb.AppendLine($"\t\t\t\t\tBottom = new SizeStyleDescriptor {{ Unit = SizeUnit.{marginStyle.Bottom.Unit}, Value = {marginStyle.Bottom.Value} }},");
                                sb.AppendLine($"\t\t\t\t\tLeft = new SizeStyleDescriptor {{ Unit = SizeUnit.{marginStyle.Left.Unit}, Value = {marginStyle.Left.Value} }}");
                                sb.AppendLine($"\t\t\t\t}},");
                                break;

                            case StyleIdentifier.Mask:
                                var maskStyle = (MaskStyleDescriptor)style;
                                sb.AppendLine($"\t\t\t\tnew MaskStyleDescriptor {{");
                                sb.AppendLine($"\t\t\t\t\tTop = {(maskStyle.Top ? "true" : "false")},");
                                sb.AppendLine($"\t\t\t\t\tRight = {(maskStyle.Right ? "true" : "false")},");
                                sb.AppendLine($"\t\t\t\t\tBottom = {(maskStyle.Bottom ? "true" : "false")},");
                                sb.AppendLine($"\t\t\t\t\tLeft = {(maskStyle.Left ? "true" : "false")}");
                                sb.AppendLine($"\t\t\t\t}},");
                                break;

                            case StyleIdentifier.Padding:
                                var paddingStyle = (PaddingStyleDescriptor)style;
                                sb.AppendLine($"\t\t\t\tnew PaddingStyleDescriptor {{");
                                sb.AppendLine($"\t\t\t\t\tTop = new SizeStyleDescriptor {{ Unit = SizeUnit.{paddingStyle.Top.Unit}, Value = {paddingStyle.Top.Value} }},");
                                sb.AppendLine($"\t\t\t\t\tRight = new SizeStyleDescriptor {{ Unit = SizeUnit.{paddingStyle.Right.Unit}, Value = {paddingStyle.Right.Value} }},");
                                sb.AppendLine($"\t\t\t\t\tBottom = new SizeStyleDescriptor {{ Unit = SizeUnit.{paddingStyle.Bottom.Unit}, Value = {paddingStyle.Bottom.Value} }},");
                                sb.AppendLine($"\t\t\t\t\tLeft = new SizeStyleDescriptor {{ Unit = SizeUnit.{paddingStyle.Left.Unit}, Value = {paddingStyle.Left.Value} }}");
                                sb.AppendLine($"\t\t\t\t}},");
                                break;

                            case StyleIdentifier.Width:
                                var widthStyle = (WidthStyleDescriptor)style;
                                sb.AppendLine($"\t\t\t\tnew WidthStyleDescriptor {{ Unit = SizeUnit.{widthStyle.Unit}, Value = {widthStyle.Value} }},");
                                break;

                            default:
                                throw new NotSupportedException();
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
