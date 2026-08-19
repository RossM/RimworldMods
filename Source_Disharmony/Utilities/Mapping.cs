using System.Collections;

namespace Disharmony.Utilities;

/// <summary>
///     Represents a mapping from elements of <typeparamref name="T" /> to other elements of <typeparamref name="T" />,
///     where an element not explicitly set maps to itself.
/// </summary>
/// <typeparam name="T"></typeparam>
internal class Mapping<T> : IEnumerable<KeyValuePair<T, T>>
{
    private readonly Dictionary<T, T> elements = [];

    public T this[T key]
    {
        get => elements.TryGetValue(key, out T value) ? value : key;
        set
        {
            if (Equals(key, value))
                elements.Remove(key);
            else
                elements[key] = value;
        }
    }

    /// <summary>
    ///     Returns a mapping that produces the effect of first applying <paramref name="first" /> and then <paramref name="second" />.
    /// </summary>
    /// <remarks>
    ///     <c>Merge(first, second)[x]</c> will give the same result as <c>second[first[x]]</c>.
    /// </remarks>
    /// <param name="first"></param>
    /// <param name="second"></param>
    /// <returns></returns>
    public static Mapping<T> Merge(Mapping<T> first, Mapping<T> second)
    {
        Mapping<T> result = [];
        foreach (var kvp in first)
            result[kvp.Key] = second[kvp.Value];
        foreach (var kvp in second)
            if (!result.elements.ContainsKey(kvp.Key))
                result[kvp.Key] = kvp.Value;
        return result;
    }

    public IEnumerator<KeyValuePair<T, T>> GetEnumerator() => elements.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)elements).GetEnumerator();
}
