using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Disharmony.Utilities;

/// <summary>
///     Associates each key with an insertion-ordered collection of values.
/// </summary>
internal sealed class MultiDictionary<TKey, TElement> : IEnumerable<IGrouping<TKey, TElement>>
{
    private class Grouping(TKey key, IEnumerable<TElement> values) : IGrouping<TKey, TElement>
    {
        public TKey Key { get; } = key;
        public IEnumerator<TElement> GetEnumerator() => values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)values).GetEnumerator();
    }

    private IEnumerable<IGrouping<TKey, TElement>> Groups => valuesByKey.Select(kvp => new Grouping(kvp.Key, kvp.Value));

    private readonly Dictionary<TKey, List<TElement>> valuesByKey = [];

    public IEnumerable<TElement> this[TKey key] => valuesByKey.TryGetValue(key, out var values) ? values : Array.Empty<TElement>();

    public void Add(TKey key, TElement value)
    {
        if (!valuesByKey.TryGetValue(key, out List<TElement>? values))
        {
            values = [];
            valuesByKey.Add(key, values);
        }

        values.Add(value);
    }

    public bool Remove(TKey key, TElement value) => valuesByKey.TryGetValue(key, out var values) && values.Remove(value);

    public int RemoveAll(TKey key, Predicate<TElement> predicate) =>
        valuesByKey.TryGetValue(key, out var values) ? values.RemoveAll(predicate) : 0;

    public int RemoveAll(Func<TKey, TElement, bool> predicate)
    {
        int removed = 0;
        foreach (var kvp in valuesByKey)
            removed += kvp.Value.RemoveAll(value => predicate(kvp.Key, value));
        return removed;
    }

    public bool TryGetValues(
        TKey key,
        [NotNullWhen(true)] out IReadOnlyList<TElement>? values)
    {
        if (valuesByKey.TryGetValue(key, out List<TElement>? mutableValues))
        {
            values = mutableValues;
            return true;
        }

        values = null;
        return false;
    }

    public void Clear() => valuesByKey.Clear();

    public IEnumerator<IGrouping<TKey, TElement>> GetEnumerator() => Groups.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Groups).GetEnumerator();
}
