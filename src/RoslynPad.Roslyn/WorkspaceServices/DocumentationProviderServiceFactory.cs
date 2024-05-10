using System.Collections.Concurrent;
using System.Composition;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace RoslynPad.Roslyn.WorkspaceServices;

[ExportWorkspaceServiceFactory(typeof(IDocumentationProviderService), ServiceLayer.Host), Shared]
[method: ImportingConstructor]
internal sealed class DocumentationProviderServiceFactory(IDocumentationProviderService service) : IWorkspaceServiceFactory
{
    private readonly IDocumentationProviderService _service = service;

    public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices) => _service;
}

[Export(typeof(IDocumentationProviderService)), Shared]
internal sealed class DocumentationProviderService : IDocumentationProviderService
{
    private readonly ConcurrentDictionary<string, DocumentationProvider> _assemblyPathToDocumentationProviderMap = new();

    public DocumentationProvider GetDocumentationProvider(string assemblyFullPath)
    {
        string? finalPath = Path.ChangeExtension(assemblyFullPath, "xml");

        return _assemblyPathToDocumentationProviderMap.GetOrAdd(assemblyFullPath, _ =>
        {
            if (!File.Exists(finalPath))
            {
                return DocumentationProvider.Default;
            }

            return XmlDocumentationProvider.CreateFromFile(finalPath);
        });
    }
}
