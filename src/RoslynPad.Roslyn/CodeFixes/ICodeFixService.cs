using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;

namespace RoslynPad.Roslyn.CodeFixes;

public interface ICodeFixService
{
    IAsyncEnumerable<CodeFixCollect> StreamFixesAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken);

    CodeFixProvider? GetSuppressionFixer(string language, IEnumerable<string> diagnosticIds);
}
