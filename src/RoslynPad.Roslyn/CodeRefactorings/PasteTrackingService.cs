﻿using System.Composition;

using Microsoft.CodeAnalysis.PasteTracking;
using Microsoft.CodeAnalysis.Text;

namespace RoslynPad.Roslyn.CodeRefactorings;

[Export(typeof(IPasteTrackingService)), Shared]
internal class PasteTrackingService : IPasteTrackingService
{
    public bool TryGetPastedTextSpan(SourceTextContainer sourceTextContainer, out TextSpan textSpan)
    {
        textSpan = default;
        return false;
    }
}
