using System.Collections;
using Disharmony.Optimizer;

namespace Disharmony.Tests;

[TestFixture]
public sealed class DisjointSetUnionTests
{
    [Test]
    public void Add_NewAndExistingValues_ReturnsWhetherTheValueWasAdded()
    {
        DisjointSetUnion<object> sets = new();
        object value = new();

        Assert.Multiple(() =>
        {
            Assert.That(sets.Add(value), Is.True);
            Assert.That(sets.Add(value), Is.False);
            Assert.That(sets.GetRoot(value), Is.SameAs(value));
            Assert.That(sets[value], Is.SameAs(value));
        });
    }

    [Test]
    public void GetRoot_ValueWasNotAdded_ThrowsKeyNotFoundException()
    {
        DisjointSetUnion<object> sets = new();

        Assert.That(() => sets.GetRoot(new object()), Throws.TypeOf<KeyNotFoundException>());
    }

    [Test]
    public void Merge_TwoValues_UsesTheRightSetRoot()
    {
        DisjointSetUnion<object> sets = new();
        object left = new();
        object right = new();
        sets.Add(left);
        sets.Add(right);

        sets.Merge(left, right);

        Assert.Multiple(() =>
        {
            Assert.That(sets.GetRoot(left), Is.SameAs(right));
            Assert.That(sets.GetRoot(right), Is.SameAs(right));
        });
    }

    [Test]
    public void Merge_ChainedSets_UpdatesEveryValueToTheFinalRoot()
    {
        DisjointSetUnion<object> sets = new();
        object first = new();
        object second = new();
        object third = new();
        object fourth = new();
        sets.Add(first);
        sets.Add(second);
        sets.Add(third);
        sets.Add(fourth);

        sets.Merge(first, second);
        sets.Merge(third, fourth);
        sets.Merge(second, third);

        Assert.Multiple(() =>
        {
            Assert.That(sets.GetRoot(first), Is.SameAs(fourth));
            Assert.That(sets.GetRoot(second), Is.SameAs(fourth));
            Assert.That(sets.GetRoot(third), Is.SameAs(fourth));
            Assert.That(sets.GetRoot(fourth), Is.SameAs(fourth));
        });
    }

    [Test]
    public void Merge_ValuesAlreadyInTheSameSet_DoesNotChangeTheRoot()
    {
        DisjointSetUnion<object> sets = new();
        object first = new();
        object second = new();
        sets.Add(first);
        sets.Add(second);
        sets.Merge(first, second);

        sets.Merge(second, first);
        sets.Merge(first, first);

        Assert.Multiple(() =>
        {
            Assert.That(sets.GetRoot(first), Is.SameAs(second));
            Assert.That(sets.GetRoot(second), Is.SameAs(second));
        });
    }

    [Test]
    public void Enumeration_ReturnsOneGroupingPerDisjointSet()
    {
        DisjointSetUnion<object> sets = new();
        object first = new();
        object second = new();
        object third = new();
        object fourth = new();
        sets.Add(first);
        sets.Add(second);
        sets.Add(third);
        sets.Add(fourth);
        sets.Merge(first, second);
        sets.Merge(third, fourth);

        var groups = sets.ToList();

        Assert.Multiple(() =>
        {
            Assert.That(groups, Has.Count.EqualTo(2));
            Assert.That(groups.Single(group => ReferenceEquals(group.Key, second)),
                Is.EquivalentTo(new[] { first, second }));
            Assert.That(groups.Single(group => ReferenceEquals(group.Key, fourth)),
                Is.EquivalentTo(new[] { third, fourth }));
            Assert.That(((IEnumerable)sets).Cast<object>().Count(), Is.EqualTo(2));
        });
    }
}
