using Disharmony.Tests.ReflectionFixtures;

namespace Disharmony.Benchmarks;

[TestFixture]
public sealed class CompatibilityTests
{
    [Test]
    public void LookupCombinationsMatchBaselineIncludingOrderAndErrors()
    {
        var lookup = typeof(LookupTarget);
        var derived = typeof(DerivedFixture);
        var paths = new (Type? Type, string? Name)[]
        {
            (lookup, "Method"), (lookup, "OverloadedMethod"), (lookup, "GenericMethod"),
            (lookup, "MixedMethod"), (lookup, "RefMethod"), (lookup, "InMethod"), (lookup, "OutMethod"),
            (lookup, "Field"), (lookup, "Property"), (lookup, "WriteOnlyProperty"), (lookup, "ReadOnlyProperty"),
            (lookup, "NestedTarget.Method"), (lookup, "StaticLocalMethodContainer.StaticLocalMethod"),
            (lookup, "CapturedLocalMethodContainer.CapturedLocalMethod"), (lookup, "LambdaContainer.*"),
            (lookup, "Missing.Local"), (lookup, "Method*"), (lookup, "Method*.Local"),
            (lookup, ""), (lookup, "."), (lookup, "Method."), (lookup, null), (null, null),
            (null, "LookupTarget.Method"), (null, lookup.FullName + ".NestedTarget.Method"),
            (derived, lookup.FullName + ":Method"), (null, lookup.AssemblyQualifiedName + ":Method"),
            (null, lookup.FullName + ":NestedTarget.Method"),
            (null, lookup.FullName + ".StaticLocalMethodContainer.StaticLocalMethod"),
            (null, lookup.FullName + ":CapturedLocalMethodContainer.CapturedLocalMethod"),
            (null, lookup.FullName + ".LambdaContainer.*"), (null, lookup.FullName + "+NestedTarget:Method"),
            (derived, "Hidden"), (derived, "Constrained"), (derived, "ReadOnly"), (derived, null),
            (derived, "LocalContainer.Local"), (derived, "Nested.LocalContainer.Local"),
            (derived, "Nested.Captured.Local"), (derived, "Nested.Lambda.*"), (derived, "Nested.Deep.Method"),
            (null, derived.FullName + ".Nested.Captured.Local"),
            (null, derived.FullName + ":Nested.Lambda.*"),
            (derived, "Nested.Captured.Local.TooDeep"), (null, "MissingType:Method"),
            (lookup, "Method:Extra:Part"), (null, "System.String:Substring"),
            // A colon currently permits a second global type lookup in the member portion.
            (null, derived.FullName + ":" + lookup.FullName + ".Method"),
        };
        Type[]?[] parameters = [null, [], [typeof(int)], [typeof(string)], [typeof(Ref<int>)], [typeof(In<int>)], [typeof(Out<int>)]];
        Type[]?[] generics = [null, [], [typeof(int)], [typeof(string)], [typeof(int), typeof(string)]];
        int checks = 0;
        foreach (var path in paths)
        foreach (MemberType kind in new[] { MemberType.Any, MemberType.Method, MemberType.Getter, MemberType.Setter, MemberType.Constructor, (MemberType)999 })
        foreach (var parameterTypes in parameters)
        foreach (var genericTypes in generics)
        foreach (bool bases in new[] { false, true })
        {
            List<MemberInfo>? expected = null;
            Exception? error = null;
            try { expected = BaselineReflectionTools.GetMembers(path.Type, path.Name, kind, parameterTypes, genericTypes, bases); }
            catch (Exception ex) { error = ex; }
            foreach (var variant in Program.Variants.Skip(1))
            {
                IReadOnlyList<MemberInfo>? actual = null;
                Exception? actualError = null;
                try { actual = variant.Members(path.Type, path.Name, kind, parameterTypes, genericTypes, bases); }
                catch (Exception ex) { actualError = ex; }
                string context = $"{variant.Name}: {path.Type}, {path.Name}, {kind}, bases={bases}";
                Assert.That(actualError?.GetType(), Is.EqualTo(error?.GetType()), context);
                Assert.That(actualError?.Message, Is.EqualTo(error?.Message), context);
                Assert.That(actual, Is.EqualTo(expected), context);
                checks++;
            }
        }
        TestContext.Out.WriteLine($"Compared {checks} lookup outcomes (including ordered members and exception messages).");
    }

    [Test]
    public void AllVariantsResolveNewAssembliesAfterAnEarlierMiss()
    {
        string name = "FreshAssembly.Target" + Guid.NewGuid().ToString("N");
        foreach (var variant in Program.Variants) Assert.That(variant.TypeByName(name), Is.Null);
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString("N")), AssemblyBuilderAccess.Run);
        Type type = assembly.DefineDynamicModule("Main").DefineType(name, TypeAttributes.Public).CreateType()!;
        foreach (var variant in Program.Variants) Assert.That(variant.TypeByName(name), Is.EqualTo(type), variant.Name);
    }

    [Test]
    public void MemoizedExperimentDemonstratesStaleMissWithinExistingDynamicAssembly()
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString("N")), AssemblyBuilderAccess.Run);
        var module = assembly.DefineDynamicModule("Main");
        module.DefineType("DynamicSeed" + Guid.NewGuid().ToString("N"), TypeAttributes.Public).CreateType();
        string name = "DynamicLate.Target" + Guid.NewGuid().ToString("N");
        foreach (var variant in Program.Variants) Assert.That(variant.TypeByName(name), Is.Null);
        // Ensure any assemblies loaded while constructing another variant's index have settled.
        Assert.That(CachedReflectionTools.GetTypeByName(name), Is.Null);
        Type type = module.DefineType(name, TypeAttributes.Public).CreateType()!;
        foreach (var variant in Program.Variants.Where(v => v.Name != "cached"))
            Assert.That(variant.TypeByName(name), Is.EqualTo(type), variant.Name);
        Assert.That(CachedReflectionTools.GetTypeByName(name), Is.Null,
            "This intentionally records the prototype's compatibility defect; do not promote this cache.");
    }

    [Test]
    public void IndexedExperimentDemonstratesMissingTypeForwarder()
    {
#if NET10_0
        const string framework = "net10.0";
#else
        const string framework = "net472";
#endif
        string path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..",
            "Fixtures", "Forwarder", "bin", "Release", framework, "Forwarder.dll"));
        Assembly.LoadFrom(path);
        Assert.That(IndexedReflectionTools.GetTypeByName("BenchmarkForwarding.Target"), Is.Null,
            "The prototype cannot prove a plain-name miss using GetTypes alone: it omits forwarders.");
        Assert.That(LazyReflectionTools.GetTypeByName("BenchmarkForwarding.Target"), Is.Not.Null);
        Assert.That(BaselineReflectionTools.GetTypeByName("BenchmarkForwarding.Target"), Is.Not.Null);
    }

    [Test]
    public void TypeSyntaxMatchesBaseline()
    {
        foreach (string name in new[]
        {
            "System.Int32[]", "System.Int32[,]", "System.Int32&", "System.Int32*",
            "System.Collections.Generic.List`1[[System.Int32]]", typeof(LookupTarget).AssemblyQualifiedName!,
            typeof(LookupTarget.NestedTarget).FullName!,
        })
        {
            Type? expected = BaselineReflectionTools.GetTypeByName(name);
            Assert.That(expected, Is.Not.Null, name);
            foreach (var variant in Program.Variants.Skip(1))
                Assert.That(variant.TypeByName(name), Is.EqualTo(expected), variant.Name + ": " + name);
        }
    }

    [Test, NUnit.Framework.Category("BaselineLimitation")]
    public void EmittedDottedNestedTypeAliasesCanBeFound()
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString("N")), AssemblyBuilderAccess.Run);
        var module = assembly.DefineDynamicModule("Main");
        var outer = module.DefineType("AliasNamespace.Outer" + Guid.NewGuid().ToString("N"), TypeAttributes.Public);
        string innerName = "Inner" + Guid.NewGuid().ToString("N");
        var inner = outer.DefineNestedType("Dotted." + innerName, TypeAttributes.NestedPublic);
        outer.CreateType();
        Type nested = inner.CreateType()!;
        foreach (string name in new[]
        {
            nested.FullName!, nested.Name,
        })
        {
            Type? expected = BaselineReflectionTools.GetTypeByName(name);
            Assert.That(expected, Is.Not.Null, name);
            foreach (var variant in Program.Variants.Skip(1))
                Assert.That(variant.TypeByName(name), Is.EqualTo(expected), variant.Name + ": " + name);
        }
        // Earlier prototypes retain the accidental namespace + nested Name spelling.
        // The production contract requires the containing types when a namespace is given.
        string incomplete = nested.Namespace + "." + nested.Name;
        foreach (var variant in Program.Variants.Where(v => v.Name != "current"))
            Assert.That(variant.TypeByName(incomplete), Is.EqualTo(nested), variant.Name);
        Assert.That(ReflectionTools.GetTypeByName(incomplete), Is.Null);
        // Reflection strips the dotted prefix from Name; it remains only in FullName.
        foreach (var variant in Program.Variants)
        {
            Assert.That(variant.TypeByName("Dotted." + innerName), Is.Null, variant.Name);
            Assert.That(variant.TypeByName("AliasNamespace.Dotted." + innerName), Is.Null, variant.Name);
        }
    }

    [Test]
    public void ExactExperimentDocumentsItsNarrowerTypeNameContract()
    {
        Assert.That(ExactReflectionTools.GetTypeByName(typeof(LookupTarget).FullName!), Is.EqualTo(typeof(LookupTarget)));
        Assert.That(ExactReflectionTools.GetTypeByName("LookupTarget"), Is.Null);
        Assert.That(ExactReflectionTools.GetTypeByName("Disharmony.Tests.ReflectionFixtures.NestedTarget"), Is.Null);
        Assert.That(BaselineReflectionTools.GetTypeByName("Disharmony.Tests.ReflectionFixtures.NestedTarget"), Is.EqualTo(typeof(LookupTarget.NestedTarget)));
        Assert.That(ExactReflectionTools.GetMembers(typeof(LookupTarget), "CapturedLocalMethodContainer.CapturedLocalMethod", MemberType.Method, null, null), Has.Count.EqualTo(1));
    }

    [Test]
    public void HybridFastPathDocumentsExactTypeVersusFlattenedAliasPrecedence()
    {
#if NET10_0
        const string framework = "net10.0";
#else
        const string framework = "net472";
#endif
        string path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..",
            "Fixtures", "Padding", "bin", "Release", framework, "Padding.dll"));
        Assembly assembly = Assembly.LoadFrom(path);
        Type exact = assembly.GetType("BenchmarkPrecedence.Target", true)!;
        Type alias = assembly.GetType("BenchmarkPrecedence.Outer+Target", true)!;
        Assert.That(BaselineReflectionTools.GetTypeByName("BenchmarkPrecedence.Target"), Is.EqualTo(alias));
        Assert.That(HybridReflectionTools.GetTypeByName("BenchmarkPrecedence.Target"), Is.EqualTo(exact));
        Assert.That(BaselineReflectionTools.GetMembers(null, "BenchmarkPrecedence.Target.Method", MemberType.Method, null, null).Single().DeclaringType, Is.EqualTo(alias));
        Assert.That(HybridReflectionTools.GetMembers(null, "BenchmarkPrecedence.Target.Method", MemberType.Method, null, null).Single().DeclaringType, Is.EqualTo(exact));
        Assert.That(HybridReflectionTools.GetMembers(null, "LookupTarget.Method", MemberType.Method, null, null), Has.Count.EqualTo(1));
    }
}
