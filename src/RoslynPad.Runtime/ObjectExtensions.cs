using System.Collections;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace RoslynPad.Runtime;

public static class ObjectExtensions
{
    public static T Dump<T>(this T o, int maxDepth = DumpQuotas.DefaultMaxDepth, int maxExpandedDepth = DumpQuotas.DefaultMaxExpandedDepth, int maxEnumerableLength = DumpQuotas.DefaultMaxEnumerableLength, int maxStringLength = DumpQuotas.DefaultMaxStringLength, [CallerLineNumber] int? line = null, [CallerArgumentExpression(nameof(o))] string? header = null)
    {
        Dumped?.Invoke(new DumpData(o, header, line, new DumpQuotas(maxDepth, maxExpandedDepth, maxEnumerableLength, maxStringLength)));
        return o;
    }

    public static Span<T> Dump<T>(this Span<T> o, int maxDepth = DumpQuotas.DefaultMaxDepth, int maxExpandedDepth = DumpQuotas.DefaultMaxExpandedDepth, int maxEnumerableLength = DumpQuotas.DefaultMaxEnumerableLength, int maxStringLength = DumpQuotas.DefaultMaxStringLength, [CallerLineNumber] int? line = null, [CallerArgumentExpression(nameof(o))] string? header = null)
    {
        Dump(o.ToArray(), maxDepth, maxExpandedDepth, maxEnumerableLength, maxStringLength, line, header);
        return o;
    }

    public static ReadOnlySpan<T> Dump<T>(this ReadOnlySpan<T> o, int maxDepth = DumpQuotas.DefaultMaxDepth, int maxExpandedDepth = DumpQuotas.DefaultMaxExpandedDepth, int maxEnumerableLength = DumpQuotas.DefaultMaxEnumerableLength, int maxStringLength = DumpQuotas.DefaultMaxStringLength, [CallerLineNumber] int? line = null, [CallerArgumentExpression(nameof(o))] string? header = null)
    {
        Dump(o.ToArray(), maxDepth, maxExpandedDepth, maxEnumerableLength, maxStringLength, line, header);
        return o;
    }

    public static Memory<T> Dump<T>(this Memory<T> o, int maxDepth = DumpQuotas.DefaultMaxDepth, int maxExpandedDepth = DumpQuotas.DefaultMaxExpandedDepth, int maxEnumerableLength = DumpQuotas.DefaultMaxEnumerableLength, int maxStringLength = DumpQuotas.DefaultMaxStringLength, [CallerLineNumber] int? line = null, [CallerArgumentExpression(nameof(o))] string? header = null)
    {
        Dump(o.ToArray(), maxDepth, maxExpandedDepth, maxEnumerableLength, maxStringLength, line, header);
        return o;
    }

    public static ReadOnlyMemory<T> Dump<T>(this ReadOnlyMemory<T> o, int maxDepth = DumpQuotas.DefaultMaxDepth, int maxExpandedDepth = DumpQuotas.DefaultMaxExpandedDepth, int maxEnumerableLength = DumpQuotas.DefaultMaxEnumerableLength, int maxStringLength = DumpQuotas.DefaultMaxStringLength, [CallerLineNumber] int? line = null, [CallerArgumentExpression(nameof(o))] string? header = null)
    {
        Dump(o.ToArray(), maxDepth, maxExpandedDepth, maxEnumerableLength, maxStringLength, line, header);
        return o;
    }

    public static T DumpAs<T, TResult>(this T o, Func<T, TResult>? selector, int maxDepth = DumpQuotas.DefaultMaxDepth, int maxExpandedDepth = DumpQuotas.DefaultMaxExpandedDepth, int maxEnumerableLength = DumpQuotas.DefaultMaxEnumerableLength, int maxStringLength = DumpQuotas.DefaultMaxStringLength, [CallerLineNumber] int? line = null, [CallerArgumentExpression(nameof(o))] string? header = null)
    {
        Dump(selector != null ? (object?)selector.Invoke(o) : null, maxDepth, maxExpandedDepth, maxEnumerableLength, maxStringLength, line, header);
        return o;
    }

    public static TEnumerable DumpFirst<TEnumerable>(this TEnumerable enumerable, int maxDepth = DumpQuotas.DefaultMaxDepth, int maxExpandedDepth = DumpQuotas.DefaultMaxExpandedDepth, int maxEnumerableLength = DumpQuotas.DefaultMaxEnumerableLength, int maxStringLength = DumpQuotas.DefaultMaxStringLength, [CallerLineNumber] int? line = null, [CallerArgumentExpression(nameof(enumerable))] string? header = null)
        where TEnumerable : IEnumerable
    {
        Dump(enumerable?.Cast<object>().FirstOrDefault(), maxDepth, maxExpandedDepth, maxEnumerableLength, maxStringLength, line, header);
        return enumerable!;
    }

    public static TEnumerable DumpLast<TEnumerable>(this TEnumerable enumerable, int maxDepth = DumpQuotas.DefaultMaxDepth, int maxExpandedDepth = DumpQuotas.DefaultMaxExpandedDepth, int maxEnumerableLength = DumpQuotas.DefaultMaxEnumerableLength, int maxStringLength = DumpQuotas.DefaultMaxStringLength, [CallerLineNumber] int? line = null, [CallerArgumentExpression(nameof(enumerable))] string? header = null)
        where TEnumerable : IEnumerable
    {
        Dump(enumerable?.Cast<object>().LastOrDefault(), maxDepth, maxExpandedDepth, maxEnumerableLength, maxStringLength, line, header);
        return enumerable!;
    }

    public static TEnumerable DumpElementAt<TEnumerable>(this TEnumerable enumerable, int index, int maxDepth = DumpQuotas.DefaultMaxDepth, int maxExpandedDepth = DumpQuotas.DefaultMaxExpandedDepth, int maxEnumerableLength = DumpQuotas.DefaultMaxEnumerableLength, int maxStringLength = DumpQuotas.DefaultMaxStringLength, [CallerLineNumber] int? line = null, [CallerArgumentExpression(nameof(enumerable))] string? header = null)
        where TEnumerable : IEnumerable
    {
        Dump(enumerable?.Cast<object>().ElementAtOrDefault(index), maxDepth, maxExpandedDepth, maxEnumerableLength, maxStringLength, line, header);
        return enumerable!;
    }

    internal static event DumpDelegate? Dumped;

    internal delegate void DumpDelegate(in DumpData data);
}

internal record struct DumpData(object? Object, string? Header, int? Line, DumpQuotas Quotas);

internal record struct DumpQuotas(int MaxDepth, int MaxExpandedDepth, int MaxEnumerableLength, int MaxStringLength)
{
    internal const int DefaultMaxDepth = 4;
    internal const int DefaultMaxExpandedDepth = 1;
    internal const int DefaultMaxStringLength = 10000;
    internal const int DefaultMaxEnumerableLength = 10000;

    public static DumpQuotas Default { get; } = new DumpQuotas(DefaultMaxDepth, DefaultMaxExpandedDepth, DefaultMaxEnumerableLength, DefaultMaxStringLength);

    [Pure]
    internal DumpQuotas StepDown() => this with { MaxDepth = MaxDepth - 1, MaxExpandedDepth = MaxExpandedDepth - 1 };
}
