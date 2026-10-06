using Disharmony.Tests.ReflectionFixtures;
using Disharmony.Tests;

namespace Disharmony.Benchmarks;

internal delegate IReadOnlyList<MemberInfo> Lookup(Type? type, string? name, MemberType kind, Type[]? parameters, Type[]? generics, bool bases);

internal sealed record Variant(string Name, Type Implementation, Lookup Members, Func<string, Type?> TypeByName);

internal static class Program
{
    internal static readonly Variant[] Variants =
    [
        new("baseline", typeof(BaselineReflectionTools), BaselineReflectionTools.GetMembers, BaselineReflectionTools.GetTypeByName),
        new("named", typeof(NamedReflectionTools), NamedReflectionTools.GetMembers, NamedReflectionTools.GetTypeByName),
        new("parsed", typeof(ParsedReflectionTools), ParsedReflectionTools.GetMembers, ParsedReflectionTools.GetTypeByName),
        new("cached", typeof(CachedReflectionTools), CachedReflectionTools.GetMembers, CachedReflectionTools.GetTypeByName),
        new("indexed", typeof(IndexedReflectionTools), IndexedReflectionTools.GetMembers, IndexedReflectionTools.GetTypeByName),
        new("streamlined", typeof(StreamlinedReflectionTools), StreamlinedReflectionTools.GetMembers, StreamlinedReflectionTools.GetTypeByName),
        new("lazy", typeof(LazyReflectionTools), LazyReflectionTools.GetMembers, LazyReflectionTools.GetTypeByName),
        new("members", typeof(MemberReflectionTools), MemberReflectionTools.GetMembers, MemberReflectionTools.GetTypeByName),
        new("hybrid", typeof(HybridReflectionTools), HybridReflectionTools.GetMembers, HybridReflectionTools.GetTypeByName),
        new("current", typeof(ReflectionTools), ReflectionTools.GetMembers, ReflectionTools.GetTypeByName),
    ];

    private static object? sink;

    public static int Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        if (args.Contains("--verify"))
            return new NUnitLite.AutoRun().Execute(args.Where(a => a != "--verify").ToArray());

        string selected = args.Length > 0 ? args[0] : "baseline";
        Variant variant = selected == "exact"
            ? new("exact", typeof(ExactReflectionTools), ExactReflectionTools.GetMembers, ExactReflectionTools.GetTypeByName)
            : Variants.Single(v => v.Name == selected);
        int extraAssemblies = args.Length > 1 ? int.Parse(args[1]) : 0;
        for (int i = 0; i < extraAssemblies; i++)
        {
#if NET10_0
            const string framework = "net10.0";
#else
            const string framework = "net472";
#endif
            string paddingPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..",
                "Fixtures", "Padding", "bin", "Release", framework, "Padding.dll"));
            Assembly.Load(File.ReadAllBytes(paddingPath));
        }

        Console.WriteLine($"# runtime={System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}; variant={selected}; padding={extraAssemblies}; processorCount={Environment.ProcessorCount}; utc={DateTime.UtcNow:O}");
        var target = typeof(LookupTarget);
        string full = target.FullName!;
        string qualified = full + ".Method";
        string colonName = full + ":Method";
        string nestedName = full + ".NestedTarget.Method";
        string localName = full + ".CapturedLocalMethodContainer.CapturedLocalMethod";
        string nestedTypeName = full + "+NestedTarget";
        Type[] ints = [typeof(int)];
        // First lookup includes JIT and initial type-index construction. Use a fresh process per run.
        var cold = Stopwatch.StartNew();
        sink = variant.Members(null, full + ".Method", MemberType.Method, ints, null, false);
        cold.Stop();
        Console.WriteLine($"# cold-qualified-ms={cold.Elapsed.TotalMilliseconds:F3}; assemblies={AppDomain.CurrentDomain.GetAssemblies().Length}");
        if (args.Contains("--cold-only")) return 0;

        // Load every prototype's framework dependencies outside warm measurements so
        // assembly-scan costs compare the same universe, including the exact-name variant.
        foreach (Variant preparation in Variants) preparation.TypeByName("AbsentBenchmarkWarmupType");

        var jobs = new List<(string Name, Func<object?> Run)>
        {
            ("members/direct", () => variant.Members(target, "Method", MemberType.Method, null, null, false)),
            ("members/signature", () => variant.Members(target, "OverloadedMethod", MemberType.Method, ints, null, false)),
            ("members/generic", () => variant.Members(target, "GenericMethod", MemberType.Method, ints, ints, false)),
            ("members/qualified", () => variant.Members(null, qualified, MemberType.Method, null, null, false)),
            ("members/short-type", () => variant.Members(null, "LookupTarget.Method", MemberType.Method, null, null, false)),
            ("members/colon", () => variant.Members(null, colonName, MemberType.Method, null, null, false)),
            ("members/nested-relative", () => variant.Members(target, "NestedTarget.Method", MemberType.Method, null, null, false)),
            ("members/nested-qualified", () => variant.Members(null, nestedName, MemberType.Method, null, null, false)),
            ("members/local", () => variant.Members(target, "StaticLocalMethodContainer.StaticLocalMethod", MemberType.Method, null, null, false)),
            ("members/captured-local", () => variant.Members(target, "CapturedLocalMethodContainer.CapturedLocalMethod", MemberType.Method, null, null, false)),
            ("members/qualified-local", () => variant.Members(null, localName, MemberType.Method, null, null, false)),
            ("members/lambda", () => variant.Members(target, "LambdaContainer.*", MemberType.Method, null, null, false)),
            ("members/base", () => variant.Members(typeof(MethodLookupDerived), "Inherited", MemberType.Method, ints, null, true)),
            ("members/inherited-local", () => variant.Members(typeof(DerivedFixture), "LocalContainer.Local", MemberType.Method, null, null, true)),
            ("members/missing-local", () => variant.Members(target, "AbsentContainer.Local", MemberType.Method, null, null, false)),
            ("members/property", () => variant.Members(target, "Property", MemberType.Getter, null, null, false)),
            ("members/constructor", () => variant.Members(typeof(MethodLookupTarget), null, MemberType.Constructor, null, null, false)),
            ("types/runtime-hit", () => variant.TypeByName("System.String")),
            ("types/short-hit", () => variant.TypeByName("LookupTarget")),
            ("types/full-hit", () => variant.TypeByName(full)),
            ("types/nested-plus", () => variant.TypeByName(nestedTypeName)),
            ("types/namespace-miss", () => variant.TypeByName("Disharmony.Tests.ReflectionFixtures")),
            ("types/miss", () => variant.TypeByName("AbsentBenchmarkType")),
            ("primitive/Type.GetType-miss", () => Type.GetType("Disharmony.Tests.ReflectionFixtures", false)),
            ("primitive/Assembly.GetType-miss", () => target.Assembly.GetType("Disharmony.Tests.ReflectionFixtures", false)),
            ("primitive/GetMembers", () => target.GetMembers(AccessTools.all | BindingFlags.DeclaredOnly)),
            ("primitive/GetMember", () => target.GetMember("Method", AccessTools.all | BindingFlags.DeclaredOnly)),
            ("primitive/GetNestedTypes", () => target.GetNestedTypes(AccessTools.all | BindingFlags.DeclaredOnly)),
            ("primitive/GetMethods", () => target.GetMethods(AccessTools.all | BindingFlags.DeclaredOnly)),
            ("primitive/WrapParameterTypes", () => BaselineReflectionTools.WrapParameterTypes(target.GetMethod("RefMethod")!)),
        };

        var results = variant.Implementation.GetMethod("GetResults", BindingFlags.NonPublic | BindingFlags.Static)!;
        var getResults = (Func<Type, List<string>, MemberType, Type[]?, Type[]?, bool, IReadOnlyList<MemberInfo>>)Delegate.CreateDelegate(
            typeof(Func<Type, List<string>, MemberType, Type[]?, Type[]?, bool, IReadOnlyList<MemberInfo>>), results);
        var parts = new List<string> { "Method" };
        var localParts = new List<string> { "CapturedLocalMethodContainer", "CapturedLocalMethod" };
        jobs.Add(("helper/GetResults-direct", () => getResults(target, parts, MemberType.Method, null, null, false)));
        jobs.Add(("helper/GetResults-local", () => getResults(target, localParts, MemberType.Method, null, null, false)));
        var filter = (Func<IEnumerable<MemberInfo>, Type[]?, Type[]?, IEnumerable<MethodBase>>)Delegate.CreateDelegate(
            typeof(Func<IEnumerable<MemberInfo>, Type[]?, Type[]?, IEnumerable<MethodBase>>),
            variant.Implementation.GetMethod("FilterMethods", BindingFlags.NonPublic | BindingFlags.Static)!);
        var methods = target.GetMember("OverloadedMethod", AccessTools.all);
        jobs.Add(("helper/FilterMethods", () => filter(methods, ints, null).ToArray()));
        jobs.Add(("helper/index-rebuild", () =>
        {
            variant.Implementation.GetMethod("AssemblyLoadHandler", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, new object?[] { null, null });
            // Call the uncached core in the memoization experiment to actually rebuild its index.
            var core = variant.Implementation.GetMethod("ResolveTypeByName", BindingFlags.NonPublic | BindingFlags.Static);
            return core is null ? variant.TypeByName("LookupTarget") : core.Invoke(null, new object[] { "LookupTarget" });
        }));

        // AllTypes itself loads dependencies. Settle that universe before comparing warm
        // assembly scans; otherwise different designs would scan different assembly sets.
        int assemblyCount;
        do
        {
            assemblyCount = AppDomain.CurrentDomain.GetAssemblies().Length;
            foreach (Assembly assembly in AccessTools.AllAssemblies().ToArray())
                AccessTools.GetTypesFromAssembly(assembly);
        } while (assemblyCount != AppDomain.CurrentDomain.GetAssemblies().Length);
        variant.Implementation.GetMethod("AssemblyLoadHandler", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, new object?[] { null, null });
        Console.WriteLine($"# warm-assemblies={AppDomain.CurrentDomain.GetAssemblies().Length}; tieredCompilation={Environment.GetEnvironmentVariable("DOTNET_TieredCompilation") ?? "default"}");
        Console.WriteLine("case,iterations,median_ns,min_ns,max_ns,bytes_per_op");
        foreach (var job in jobs)
        {
            if (selected == "exact" && (job.Name == "members/short-type" || job.Name == "types/short-hit" || job.Name == "helper/index-rebuild")) continue;
            Measure(job.Name, job.Run);
        }
        GC.KeepAlive(sink);
        return 0;
    }

    private static void Measure(string name, Func<object?> run)
    {
        // Populate this case's caches before calibration. Otherwise an index rebuild
        // can end calibration at one iteration, leaving sub-microsecond cases unbatched.
        sink = run();
        int iterations = 1;
        int minimumIterations = name == "helper/index-rebuild" ? 1 : 64;
        double elapsed;
        do
        {
            var warm = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++) sink = run();
            elapsed = warm.Elapsed.TotalMilliseconds;
            if (elapsed < 25 || iterations < minimumIterations) iterations *= 2;
        } while ((elapsed < 25 || iterations < minimumIterations) && iterations < 1048576);

        var times = new double[7];
        var allocations = new double[7];
        for (int sample = 0; sample < times.Length; sample++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
#if NET10_0
            long before = GC.GetAllocatedBytesForCurrentThread();
#endif
            long start = Stopwatch.GetTimestamp();
            for (int i = 0; i < iterations; i++) sink = run();
            times[sample] = (Stopwatch.GetTimestamp() - start) * (1e9 / Stopwatch.Frequency) / iterations;
#if NET10_0
            allocations[sample] = (GC.GetAllocatedBytesForCurrentThread() - before) / (double)iterations;
#else
            allocations[sample] = double.NaN;
#endif
        }
        Array.Sort(times);
        Array.Sort(allocations);
        Console.WriteLine($"{name},{iterations},{times[3]:F1},{times[0]:F1},{times[6]:F1},{allocations[3]:F1}");
    }
}

public class BaseFixture
{
    public static Func<int> LocalContainer() { int Local() => 42; return Local; }
    public void Hidden(int x) { }
    public int ReadOnly => 1;
}

public class DerivedFixture : BaseFixture
{
    public void Hidden(string x) { }
    public new int ReadOnly { set { } }
    public void Constrained<T>(T value) where T : struct { }
    public class Nested : BaseFixture
    {
        public static Func<int> Captured(int value) { int Local() => value; return Local; }
        public static Func<int> Lambda(int value) => () => value;
        public class Deep { public void Method(int value) { } }
    }
}
