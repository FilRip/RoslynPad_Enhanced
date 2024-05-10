// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;

namespace RoslynPad.Roslyn.Completion.Providers;

[ExportCompletionProvider("LoadDirectiveCompletionProvider", LanguageNames.CSharp)]
internal sealed class LoadDirectiveCompletionProvider : AbstractLoadDirectiveCompletionProvider
{
    protected override bool TryGetStringLiteralToken(SyntaxTree tree, int position, out SyntaxToken stringLiteral, CancellationToken cancellationToken)
        => tree.TryGetStringLiteralToken(position, SyntaxKind.LoadDirectiveTrivia, out stringLiteral, cancellationToken);
}
