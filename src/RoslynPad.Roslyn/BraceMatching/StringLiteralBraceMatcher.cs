﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace RoslynPad.Roslyn.BraceMatching;

[ExportBraceMatcher(LanguageNames.CSharp)]
internal class StringLiteralBraceMatcher : IBraceMatcher
{
    public async Task<BraceMatchingResult?> FindBracesAsync(Document document, int position, CancellationToken cancellationToken = default)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var token = root!.FindToken(position);

        if (!token.ContainsDiagnostics)
        {
            if (token.IsKind(SyntaxKind.StringLiteralToken))
            {
                if (token.IsVerbatimStringLiteral())
                {
                    return new BraceMatchingResult(
                        new TextSpan(token.SpanStart, 2),
                        new TextSpan(token.Span.End - 1, 1));
                }

                return new BraceMatchingResult(
                    new TextSpan(token.SpanStart, 1),
                    new TextSpan(token.Span.End - 1, 1));
            }

            if (token.IsKind(SyntaxKind.InterpolatedStringStartToken) || token.IsKind(SyntaxKind.InterpolatedVerbatimStringStartToken))
            {
                if (token.Parent is InterpolatedStringExpressionSyntax interpolatedString)
                {
                    return new BraceMatchingResult(token.Span, interpolatedString.StringEndToken.Span);
                }
            }
            else if (token.IsKind(SyntaxKind.InterpolatedStringEndToken) &&
                token.Parent is InterpolatedStringExpressionSyntax interpolatedString)
            {
                return new BraceMatchingResult(interpolatedString.StringStartToken.Span, token.Span);
            }
        }

        return null;
    }
}
