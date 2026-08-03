using System.Diagnostics.CodeAnalysis;

namespace Disharmony;

/// <summary>
///     Associates each key with an insertion-ordered collection of values. The public API exposes
///     no collection mutators, so additions remain centralized in <see cref="Add"/>.
/// </summary>
internal sealed class MultiDictionary<TKey, TValue> where TKey : notnull
{
    private readonly Dictionary<TKey, List<TValue>> valuesByKey = [];

    public void Add(TKey key, TValue value)
    {
        if (!valuesByKey.TryGetValue(key, out List<TValue>? values))
        {
            values = [];
            valuesByKey.Add(key, values);
        }

        values.Add(value);
    }

    public bool TryGetValues(
        TKey key,
        [NotNullWhen(true)] out IReadOnlyList<TValue>? values)
    {
        if (valuesByKey.TryGetValue(key, out List<TValue>? mutableValues))
        {
            values = mutableValues;
            return true;
        }

        values = null;
        return false;
    }

    public void Clear() => valuesByKey.Clear();
}
