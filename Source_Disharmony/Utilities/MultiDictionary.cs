using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Disharmony.Utilities;

/// <summary>
///     Associates each key with an insertion-ordered collection of values.
/// </summary>
internal sealed class MultiDictionary<TKey, TElement> : ILookup<TKey, TElement>
{
    private class Grouping(TKey key, IEnumerable<TElement> values) : IGrouping<TKey, TElement>
    {
        public TKey Key { get; } = key;
        public IEnumerator<TElement> GetEnumerator() => values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)values).GetEnumerator();
    }

    private IEnumerable<IGrouping<TKey, TElement>> EnumerableImplementation => valuesByKey.Select(kvp => new Grouping(kvp.Key, kvp.Value));

    public int Count => valuesByKey.Count;

    
    private readonly Dictionary<TKey, List<TElement>> valuesByKey = [];

    // Creating and destroying empty lists causes GC pressure, so save any lists that become empty to reuse
    private readonly Stack<List<TElement>> emptyLists = [];
    
    public IEnumerable<TElement> this[TKey key] => valuesByKey.TryGetValue(key, out var values) ? values : Array.Empty<TElement>();

    public bool Contains(TKey key) => valuesByKey.ContainsKey(key);

    public void Add(TKey key, TElement value)
    {
        if (!valuesByKey.TryGetValue(key, out List<TElement>? values))
        {
            values = emptyLists.Count > 0 ? emptyLists.Pop() : [];
            valuesByKey.Add(key, values);
        }

        values.Add(value);
    }

    public bool Remove(TKey key, TElement value)
    {
        if (!valuesByKey.TryGetValue(key, out var values))
            return false;
        bool result = values.Remove(value);
        FreeIfEmpty(key, values);
        return result;
    }

    public int RemoveAll(TKey key, Predicate<TElement> predicate)
    {
        if (!valuesByKey.TryGetValue(key, out var values))
            return 0;
        int result = values.RemoveAll(predicate);
        FreeIfEmpty(key, values);
        return result;
    }

    public int RemoveAll(Func<TKey, TElement, bool> predicate)
    {
        int removed = 0;
        foreach (var kvp in valuesByKey.ToList())
        {
            removed += kvp.Value.RemoveAll(value => predicate(kvp.Key, value));
            FreeIfEmpty(kvp.Key, kvp.Value);
        }

        return removed;
    }

    private void FreeIfEmpty(TKey key, List<TElement> values)
    {
        if (values.Count != 0)
            return;
        valuesByKey.Remove(key);
        emptyLists.Push(values);
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

    public void Clear()
    {
        valuesByKey.Clear();
        emptyLists.Clear();
    }

    public IEnumerator<IGrouping<TKey, TElement>> GetEnumerator() => EnumerableImplementation.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)EnumerableImplementation).GetEnumerator();
}
