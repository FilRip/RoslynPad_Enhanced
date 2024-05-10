namespace RoslynPad.Roslyn.Diagnostics;

public interface IDiagnosticService
{
    event EventHandler<DiagnosticsUpdatedEventArgs> DiagnosticsUpdated;
}
