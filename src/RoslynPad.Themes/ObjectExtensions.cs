﻿using System.Runtime.CompilerServices;

namespace RoslynPad.Themes;

internal static class ObjectExtensions
{
    public static T NotNull<T>(this T? value, [CallerArgumentExpression(nameof(value))] string expression = "") =>
        value ?? throw new InvalidOperationException("Expression not expected to be null: " + expression);
}
