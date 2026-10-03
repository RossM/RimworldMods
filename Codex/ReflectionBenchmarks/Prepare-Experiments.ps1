param([string] $OutputDirectory = (Join-Path $PSScriptRoot 'obj/Experiments'))
$ErrorActionPreference = 'Stop'
$outputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Force $outputDirectory | Out-Null
$baseline = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'BaselineReflectionTools.cs'))
$named = $baseline.Replace('BaselineReflectionTools', 'NamedReflectionTools')
# GetMember recognizes a trailing wildcard, so retain the exact-name predicate.
$named = $named.Replace('type?.GetMembers(DeclaredOnly).Any(m => m.Name == nameParts[0])', 'type?.GetMember(nameParts[0], DeclaredOnly).Any(m => m.Name == nameParts[0])')
$named = $named.Replace('type.GetMembers(DeclaredOnly).Where(m => m.Name == nameParts[0])', 'type.GetMember(nameParts[0], DeclaredOnly).Where(m => m.Name == nameParts[0])')
$parsed = $named.Replace('NamedReflectionTools', 'ParsedReflectionTools')
$start = $parsed.IndexOf('        // Harmony uses')
$end = $parsed.IndexOf('        return GetResults(type, nameParts', $start)
$replacement = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'ResolveName.cs.txt'))
$parsed = $parsed.Substring(0, $start) + $replacement + $parsed.Substring($end)
$cached = $parsed.Replace('ParsedReflectionTools', 'CachedReflectionTools')
$cached = $cached.Replace('    private static Assembly[]?', @'
    private static readonly object CacheLock = new();
    private static Dictionary<string, Type?> _resolvedTypes = new(StringComparer.Ordinal);

    private static Assembly[]?
'@)
$cached = $cached.Replace('        _allAssemblies = null;', @'
        lock (CacheLock) _resolvedTypes = new(StringComparer.Ordinal);
        _allAssemblies = null;
'@)
$cached = $cached.Replace('    public static Type? GetTypeByName(string name)', @'
    // Experiment only: AssemblyLoad does not invalidate entries when an existing dynamic
    // assembly defines another type. The verification suite demonstrates this limitation.
    public static Type? GetTypeByName(string name)
    {
        Dictionary<string, Type?> cache;
        Type? result;
        lock (CacheLock)
        {
            cache = _resolvedTypes;
            if (cache.TryGetValue(name, out result)) return result;
        }
        // Enumeration can load assemblies on PLINQ workers. Holding CacheLock here
        // deadlocks those workers against the AssemblyLoad handler.
        result = ResolveTypeByName(name);
        lock (CacheLock) cache[name] = result;
        return result;
    }

    private static Type? ResolveTypeByName(string name)
'@)
$indexed = $parsed.Replace('ParsedReflectionTools', 'IndexedReflectionTools')
$indexed = $indexed.Replace('    private static Assembly[]? _allAssemblies = null;', '    private static TypeLookupGeneration _typeLookup = new();')
$indexed = $indexed.Replace('    private static Dictionary<string, Type>? _typesByName = null;', '')
$indexed = $indexed.Replace("        _allAssemblies = null;`r`n        _typesByName = null;", '        System.Threading.Interlocked.Exchange(ref _typeLookup, new TypeLookupGeneration());')
$start = $indexed.IndexOf('    // This is equivalent to AccessTools.GetTypeByName')
$end = $indexed.IndexOf('    private static IEnumerable<MethodBase> FilterMethods', $start)
$indexed = $indexed.Substring(0, $start) + [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'TypeLookup.cs.txt')) + $indexed.Substring($end)
$streamlined = $indexed.Replace('IndexedReflectionTools', 'StreamlinedReflectionTools')
$start = $streamlined.IndexOf('    private static List<MemberInfo> GetResults')
$end = $streamlined.IndexOf('    // Replace the entire generation', $start)
$streamlined = $streamlined.Substring(0, $start) + [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'MemberResults.cs.txt')) + $streamlined.Substring($end)
$streamlined = $streamlined.Replace('                if (!parameters.Zip(parameterTypes, (p, t) => (p, t)).All(x => ParameterTypeMatcher(x.p, x.t)))', @'
                bool matches = true;
                for (int i = 0; i < parameters.Length; i++)
                    if (!ParameterTypeMatcher(parameters[i], parameterTypes[i])) { matches = false; break; }
                if (!matches)
'@)
$lazy = $streamlined.Replace('StreamlinedReflectionTools', 'LazyReflectionTools')
$start = $lazy.IndexOf('    // Replace the entire generation')
$end = $lazy.IndexOf('    private static IEnumerable<MethodBase> FilterMethods', $start)
$lazy = $lazy.Substring(0, $start) + [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'LazyTypeLookup.cs.txt')) + $lazy.Substring($end)
$exact = $streamlined.Replace('StreamlinedReflectionTools', 'ExactReflectionTools')
$exact = $exact.Replace('private static TypeLookupGeneration _typeLookup = new();', 'private static Assembly[]? _allAssemblies;')
$exact = $exact.Replace('System.Threading.Interlocked.Exchange(ref _typeLookup, new TypeLookupGeneration());', '_allAssemblies = null;')
$start = $exact.IndexOf('    // Replace the entire generation')
$end = $exact.IndexOf('    private static IEnumerable<MethodBase> FilterMethods', $start)
$exact = $exact.Substring(0, $start) + @'
    // Deliberately narrower contract: runtime type names only, no global short-name
    // search and no flattened Namespace.NestedType aliases.
    public static Type? GetTypeByName(string name)
    {
        if (Type.GetType(name, throwOnError: false) is { } runtimeType) return runtimeType;
        Assembly[] assemblies = _allAssemblies ??= [.. AccessTools.AllAssemblies()];
        foreach (Assembly assembly in assemblies)
            if (assembly.GetType(name, throwOnError: false) is { } found) return found;
        return null;
    }

'@ + $exact.Substring($end)
$members = $streamlined.Replace('StreamlinedReflectionTools', 'MemberReflectionTools')
$members = $members.Replace('private static TypeLookupGeneration _typeLookup = new();', "private static Assembly[]? _allAssemblies;`r`n    private static Dictionary<string, Type>? _typesByName;")
$members = $members.Replace('System.Threading.Interlocked.Exchange(ref _typeLookup, new TypeLookupGeneration());', "_allAssemblies = null;`r`n        _typesByName = null;")
$start = $members.IndexOf('    // Replace the entire generation')
$end = $members.IndexOf('    private static IEnumerable<MethodBase> FilterMethods', $start)
$originalStart = $baseline.IndexOf('    // This is equivalent to AccessTools.GetTypeByName')
$originalEnd = $baseline.IndexOf('    private static IEnumerable<MethodBase> FilterMethods', $originalStart)
$members = $members.Substring(0, $start) + $baseline.Substring($originalStart, $originalEnd - $originalStart) + $members.Substring($end)
$hybrid = $members.Replace('MemberReflectionTools', 'HybridReflectionTools')
$hybrid = $hybrid.Replace('    public static Type? GetTypeByName(string name)', @'
    public static Type? GetTypeByName(string name) => GetExactTypeByName(name) ?? GetTypeByAlias(name);

    private static Type? GetExactTypeByName(string name)
    {
        if (Type.GetType(name, throwOnError: false) is { } runtimeType) return runtimeType;
        Assembly[] assemblies = _allAssemblies ??= [.. AccessTools.AllAssemblies()];
        foreach (Assembly assembly in assemblies)
            if (assembly.GetType(name, throwOnError: false) is { } found) return found;
        return null;
    }

    private static Type? GetTypeByAlias(string name)
'@)
$hybrid = $hybrid.Replace("                for (int dot = firstDot; dot >= 0; dot = name.IndexOf('.', dot + 1))", "                for (int pass = 0; pass < 2 && memberStart == 0; pass++)`r`n                for (int dot = firstDot; dot >= 0; dot = name.IndexOf('.', dot + 1))")
$hybrid = $hybrid.Replace('Type? foundType = GetTypeByName(name.Substring(0, dot));', 'Type? foundType = pass == 0 ? GetExactTypeByName(name.Substring(0, dot)) : GetTypeByAlias(name.Substring(0, dot));')
$sources = @{ NamedReflectionTools = $named; ParsedReflectionTools = $parsed; CachedReflectionTools = $cached; IndexedReflectionTools = $indexed; StreamlinedReflectionTools = $streamlined; LazyReflectionTools = $lazy; ExactReflectionTools = $exact; MemberReflectionTools = $members; HybridReflectionTools = $hybrid }
$tests = [IO.File]::ReadAllText((Join-Path $PSScriptRoot '../../Disharmony.Tests/Unit/ReflectionToolsTests.cs'))
foreach ($entry in $sources.GetEnumerator()) {
    [IO.File]::WriteAllText((Join-Path $outputDirectory ($entry.Key + '.cs')), ($entry.Value -replace '\r?\n', "`r`n"))
}
foreach ($variant in @('Baseline', 'Named', 'Parsed', 'Cached', 'Indexed', 'Streamlined', 'Lazy', 'Member', 'Hybrid')) {
    $source = $tests.Replace('ReflectionToolsTests', ($variant + 'ReflectionToolsTests')).Replace('ReflectionTools.Get', ($variant + 'ReflectionTools.Get'))
    [IO.File]::WriteAllText((Join-Path $outputDirectory ($variant + 'Tests.cs')), ($source -replace '\r?\n', "`r`n"))
}
