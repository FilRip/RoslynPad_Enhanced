﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.PooledObjects;

using Roslyn.Utilities;

namespace RoslynPad.Roslyn.Completion.Providers;

internal abstract class AbstractLoadDirectiveCompletionProvider : AbstractDirectivePathCompletionProvider
{
    private static readonly CompletionItemRules s_rules = CompletionItemRules.Create(
         filterCharacterRules: [],
         commitCharacterRules: [CharacterSetModificationRule.Create(CharacterSetModificationKind.Replace, GetCommitCharacters())],
         enterKeyRule: EnterKeyRule.Never,
         selectionBehavior: CompletionItemSelectionBehavior.HardSelection);

    private static ImmutableArray<char> GetCommitCharacters()
    {
        var builder = ArrayBuilder<char>.GetInstance();
        builder.Add('"');
        if (PathUtilities.IsUnixLikePlatform)
        {
            builder.Add('/');
        }
        else
        {
            builder.Add('/');
            builder.Add('\\');
        }

        return builder.ToImmutableAndFree();
    }

    protected override async Task ProvideCompletionsAsync(CompletionContext context, string pathThroughLastSlash)
    {
        var extension = Path.GetExtension(context.Document.FilePath);
        if (extension == null)
        {
            return;
        }

        var helper = GetFileSystemCompletionHelper(context.Document, Microsoft.CodeAnalysis.Glyph.CSharpFile, [extension], s_rules);
        context.AddItems(await helper.GetItemsAsync(pathThroughLastSlash, context.CancellationToken).ConfigureAwait(false));
    }
}
