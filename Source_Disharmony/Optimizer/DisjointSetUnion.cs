using System.Collections;

namespace Disharmony.Optimizer;

internal class DisjointSetUnion<T> : IEnumerable<IGrouping<T, T>> where T : class
{
    private readonly Dictionary<T, T?> parents = [];

    public bool Add(T value)
    {
        if (parents.ContainsKey(value))
            return false;

        parents.Add(value, null);
        return true;
    }

    public T GetRoot(T value)
    {
        T? parent = parents[value];
        if (parent == null)
            return value;
        return parents[value] = GetRoot(parent);
    }

    public T this[T value] => GetRoot(value);

    public void Merge(T left, T right)
    {
        var rootLeft = GetRoot(left);
        var rootRight = GetRoot(right);
        if (!Equals(rootLeft, rootRight))
            parents[rootLeft] = rootRight;
    }

    public IEnumerator<IGrouping<T, T>> GetEnumerator() => parents.Keys.ToArray().GroupBy(GetRoot).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)parents.Keys.ToArray().GroupBy(GetRoot)).GetEnumerator();
}
