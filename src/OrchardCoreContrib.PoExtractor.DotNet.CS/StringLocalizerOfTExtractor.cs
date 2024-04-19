using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;
using Microsoft.Extensions.Localization;

namespace OrchardCoreContrib.PoExtractor.DotNet.CS;

/// <summary>
/// Extracts <see cref="LocalizableStringOccurence"/> with the singular text from the C# AST node
/// </summary>
/// <remarks>
/// The localizable string is identified by the name convention - T["TEXT TO TRANSLATE"]
/// </remarks>
public class StringLocalizerOfTExtractor : LocalizableStringExtractor<SyntaxNode>
{
    private readonly SemanticModel semanticModel;
    private readonly INamedTypeSymbol stringLocalizerType;

    /// <summary>
    /// Creates a new instance of a <see cref="SingularStringExtractor"/>.
    /// </summary>
    /// <param name="metadataProvider">The <see cref="IMetadataProvider{TNode}"/>.</param>
    public StringLocalizerOfTExtractor(
        IMetadataProvider<SyntaxNode> metadataProvider,
        SemanticModel semanticModel) : base(metadataProvider)
    {
        this.semanticModel = semanticModel;
        stringLocalizerType = semanticModel.Compilation.GetTypeByMetadataName(typeof(IStringLocalizer<>).FullName!);
    }

    /// <inheritdoc/>
    public override bool TryExtract(SyntaxNode node, out LocalizableStringOccurence result)
    {
        ArgumentNullException.ThrowIfNull(node);

        result = null;
        
        if (node is ElementAccessExpressionSyntax accessor &&
            semanticModel.GetTypeInfo(accessor.Expression).Type is INamedTypeSymbol accessedType &&
            stringLocalizerType.Equals(accessedType.OriginalDefinition, SymbolEqualityComparer.Default))
        {
            var resourceType = accessedType.TypeArguments.Single();
            
            if(accessor.ArgumentList.Arguments[0].Expression is LiteralExpressionSyntax literal &&
               literal.IsKind(SyntaxKind.StringLiteralExpression))
            {
                result = CreateLocalizedString(literal.Token.ValueText, null, node);
                return true;
            }

            var location = MetadataProvider.GetLocation(node);
            throw new Exception(
                $"Detected access to {nameof(IStringLocalizer<object>)}[] where the first argument is not a string literal at {location.SourceFile}:{location.SourceFileLine}.");
        }

        return false;
    }

}
