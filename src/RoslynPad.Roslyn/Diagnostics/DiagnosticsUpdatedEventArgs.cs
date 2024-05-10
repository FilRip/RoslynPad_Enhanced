using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

namespace RoslynPad.Roslyn.Diagnostics;

public class DiagnosticsUpdatedEventArgs : UpdatedEventArgs
{
    private readonly Microsoft.CodeAnalysis.Diagnostics.DiagnosticsUpdatedArgs _inner;

    public DiagnosticsUpdatedKind Kind { get; }
    public Solution? Solution { get; }
    public ImmutableArray<DiagnosticData> Diagnostics { get; private set; }

    internal DiagnosticsUpdatedEventArgs(Microsoft.CodeAnalysis.Diagnostics.DiagnosticsUpdatedArgs inner, ImmutableArray<DiagnosticData>? diagnostics = null) : base(inner)
    {
        _inner = inner;
        Solution = inner.Solution;
        Diagnostics = diagnostics ?? inner.Diagnostics.Select(x => new DiagnosticData(x)).ToImmutableArray();
        Kind = (DiagnosticsUpdatedKind)inner.Kind;
    }

    public DiagnosticsUpdatedEventArgs WithDiagnostics(ImmutableArray<DiagnosticData> diagnostics)
    {
        return new DiagnosticsUpdatedEventArgs(_inner, diagnostics);
    }
}
