using Microsoft.CodeAnalysis;

using RoslynPad.Roslyn;

namespace RoslynPad.Editor;

public sealed class RoslynHighlightingColorizer(DocumentId documentId, IRoslynHost roslynHost, IClassificationHighlightColors highlightColors) : HighlightingColorizer
{
    private readonly DocumentId _documentId = documentId;
    private readonly IRoslynHost _roslynHost = roslynHost;
    private readonly IClassificationHighlightColors _highlightColors = highlightColors;

    protected override IHighlighter CreateHighlighter(TextView textView, TextDocument document) =>
        new RoslynSemanticHighlighter(textView, document, _documentId, _roslynHost, _highlightColors);
}
