namespace Disharmony.Tests;

[TestFixture]
public sealed class MultiDictionaryTests
{
    [Test]
    public void AddAssociatesMultipleValuesWithEachKeyInInsertionOrder()
    {
        MultiDictionary<string, int> dictionary = new();

        dictionary.Add("first", 1);
        dictionary.Add("second", 2);
        dictionary.Add("first", 3);

        Assert.Multiple(() =>
        {
            Assert.That(dictionary.TryGetValues("first", out var first), Is.True);
            Assert.That(first, Is.EqualTo(new[] { 1, 3 }));
            Assert.That(dictionary.TryGetValues("second", out var second), Is.True);
            Assert.That(second, Is.EqualTo(new[] { 2 }));
        });
    }

    [Test]
    public void ClearRemovesEveryAssociation()
    {
        MultiDictionary<string, int> dictionary = new();
        dictionary.Add("key", 1);

        dictionary.Clear();

        Assert.Multiple(() =>
        {
            Assert.That(dictionary.TryGetValues("key", out var values), Is.False);
            Assert.That(values, Is.Null);
        });
    }
}
