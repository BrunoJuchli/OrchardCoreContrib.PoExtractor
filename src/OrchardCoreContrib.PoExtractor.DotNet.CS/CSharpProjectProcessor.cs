using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using OrchardCoreContrib.PoExtractor.DotNet.CS.MetadataProviders;
using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Localization;

namespace OrchardCoreContrib.PoExtractor.DotNet.CS;

/// <summary>
/// Extracts localizable strings from all *.cs files in the project path.
/// </summary>
public class CSharpProjectProcessor : IProjectProcessor
{
    private static readonly string _cSharpExtension = "*.cs";

    /// <inheritdoc/>
    public virtual void Process(string path, string basePath, LocalizableStringCollection localizableStrings)
    {
        if (string.IsNullOrEmpty(path))
        {
            throw new ArgumentException($"'{nameof(path)}' cannot be null or empty.", nameof(path));
        }

        if (string.IsNullOrEmpty(basePath))
        {
            throw new ArgumentException($"'{nameof(basePath)}' cannot be null or empty.", nameof(basePath));
        }

        ArgumentNullException.ThrowIfNull(localizableStrings);

        var csharpMetadataProvider = new CSharpMetadataProvider(basePath);

        foreach (var file in Directory.EnumerateFiles(path, $"*{_cSharpExtension}", SearchOption.AllDirectories).OrderBy(file => file))
        {
            if (file.StartsWith(Path.Combine(path, "obj")))
            {
                continue;
            }

            // var csharpWalker = new ExtractingCodeWalker(
            // [
            //     new SingularStringExtractor(csharpMetadataProvider),
            //     new PluralStringExtractor(csharpMetadataProvider),
            //     new ErrorMessageAnnotationStringExtractor(csharpMetadataProvider),
            //     new DisplayAttributeDescriptionStringExtractor(csharpMetadataProvider),
            //     new DisplayAttributeNameStringExtractor(csharpMetadataProvider),
            //     new DisplayAttributeGroupNameStringExtractor(csharpMetadataProvider),
            //     new DisplayAttributeShortNameStringExtractor(csharpMetadataProvider)
            // ], localizableStrings);
            
            using var stream = File.OpenRead(file);
            using var reader = new StreamReader(stream);
            var syntaxTree = CSharpSyntaxTree.ParseText(reader.ReadToEnd(), path: file);
            
            var compilation = CSharpCompilation.Create("arg")
                .AddReferences(MetadataReference.CreateFromFile(typeof(IStringLocalizer<>).Assembly.Location))
                .AddSyntaxTrees(syntaxTree);

            var model = compilation.GetSemanticModel(syntaxTree);
                    
            var csharpWalker = new ExtractingCodeWalker(
                new IStringExtractor<SyntaxNode>[]
                {
                    new StringLocalizerOfTExtractor(
                        csharpMetadataProvider,
                        model),
                }, 
                localizableStrings);

            csharpWalker.Visit(syntaxTree.GetRoot());
        }
    }
}
