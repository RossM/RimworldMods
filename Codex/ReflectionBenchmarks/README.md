# Reflection lookup experiments

This standalone research harness supports the refactor in `Disharmony/ReflectionTools.cs`; it is not added to the solution. The frozen baseline is that file from `4993a7a0d293cef21e2967023c36adf4a93760f1`, with its class renamed. Generated variants and copies of existing tests go into `obj`; raw results go into the ignored `TestResults/ReflectionBenchmarks` directory.

## Measured RimWorld startup impact

Compared `TestResults/Report20261001-1724.diagsession` (before, RimWorld PID 46364) with `TestResults/Report20261003-1228.diagsession` (after, PID 38380). Both traces use a 1 ms CPU sampling interval and report zero lost events. The figures below estimate inclusive CPU time from samples; they are not wall-clock or instrumented call timings. Rows overlap.

| Work | Before CPU ms | After CPU ms | Reduction |
|---|---:|---:|---:|
| `ReflectionTools.GetMembers` | 210 | 19 | **91%** |
| Patch discovery through `PatchRegistry.ProcessAssembly` | 268 | 83 | **69%** |
| `Patcher.PatchAll` / `PatchCategory`, including immediate application | 747 | 466 | **38%** |
| Disharmony patching inside the XylXenos constructor | 308 | 164 | **47%** |
| Disharmony patching inside the Xylib constructor | 388 | 265 | **32%** |
| Deferred `Patcher.ForceApply` | 1,439 | 1,503 | −4% |
| All samples under Disharmony patching entry points | 2,186 | 1,969 | **10%** |

The lookup improvement is visible in the actual mod startup. `GetMembers` accounts for 106 of the XylXenos constructor's 331 samples before, versus 5 of 186 after: approximately **32% down to 3%**. The full XylXenos constructor fell from 331 to 186 sampled CPU ms (44%); Xylib's fell from 437 to 320 ms (27%). For Xylib, the lookup portion fell from 102 to 12 samples.

The old trace attributes 179 samples to `GetTypeByName`; the new trace has no samples with that method or the new type-resolution helpers as named frames. Absence of samples does not prove a method was never called, particularly with inlining. `GetResults` remains visible at 16 samples versus 27 previously. The old type-resolution hotspot is no longer a material share of patching.

The initial `PatchAll`/`PatchCategory` work saves about **281 sampled CPU ms**, while deferred application rises by 64 ms, leaving a net reduction of **217 ms** across patching. The 191 ms reduction under `GetMembers` accounts for most of that net change. Remaining patching is dominated by `ForceApply` → `ResolveAllTrampolines` / `HarmonyInterfaceImplV2.PatchDirectly` → Harmony wrapper generation. `ForceApply` now accounts for roughly 76% of patching samples; `HarmonyLib.PatchFunctions.UpdateWrapper` has 1,478 inclusive samples under patching. These nested costs must not be added together.

This is one before/after startup pair, not a controlled repeated benchmark. Total process CPU samples were nearly unchanged (102,236 versus 101,845); recorded session durations were 44.93 versus 43.68 seconds. Captured support-module metadata also differs between runs. The shorter recording cannot all be attributed to this refactor, and the profiles do not establish an exact whole-game startup speedup. They do show a large reduction in lookup work, a substantial reduction in patch registration during mod construction, and a smaller overall patching gain because deferred application remains dominant.

Raw extracted traces, tables and summaries stay under ignored `TestResults/ReflectionProfile`. To reproduce the paired analysis after extraction with `Inspect-Profile.ps1`:

```powershell
./Codex/ReflectionBenchmarks/Compare-StartupProfiles.ps1 `
    -BeforeEtl TestResults/ReflectionProfile/profile.etl `
    -AfterEtl TestResults/ReflectionProfile/20261003-1228/profile.etl
```

The comparison selects the single RimWorld process in each ETL, counts each sample once per scope, and normalizes method names by removing signatures. `Patching` includes samples with a `Disharmony.Patcher` or `Disharmony.PatchRegistry` frame. Inclusive tables and nearest-Disharmony-frame breakdowns are written under `TestResults/ReflectionProfile/Comparison`. Worker samples without those caller frames are excluded from that scope; the broader `AnyDisharmony` count falls from 2,313 to 2,072 samples (10%).

## Implemented change

The production refactor combines cheaper member selection with **longest exact type prefix first, a short-name alias on a slow path**. The fallback checks only the first component. The index stores only `Type.Name`; the old `Namespace.NestedType` spelling that omitted containing types was an implementation accident and is no longer supported. Full dotted paths and runtime names using `+` remain supported. Fully spelled dotted paths include namespaces, containing types, members and local functions; an abbreviated alias or a shorter type prefix cannot take precedence over a longer exact type prefix. Relative lookups with a supplied type retain the declared-member fast path. Namespace/nesting collisions can be disambiguated with `+`, and assembly collisions with an assembly-qualified string before `:`. No explicit `Type` is required for those cases. See the `TargetAttribute` XML doc comments in `Disharmony/Attributes.cs` for string-selector rules.

Deterministic ambiguous selection is an implementation goal, not an API guarantee. Alias selection uses ordinal assembly identity and full type name ordering, with ordered parallel enumeration retaining runtime assembly order for identical identities. Assembly-load events replace a cache generation, so an older index build cannot overwrite a newer generation. There is no cache of individual lookup hits or misses. The assembly-qualified fallback also handles loaded assemblies the runtime binder cannot locate, including emitted assemblies; the runtime still parses the type syntax.

### After removing the accidental namespace-qualified aliases

A new .NET 10 comparison with the installed game assemblies loaded measured alias-index rebuilding at **46.95 ms for the baseline and 11.52 ms for the simplified refactor** (about 4.1× faster). Calling-thread allocation fell from **19.56 MB to 6.42 MB**. This removes construction and storage of every `Namespace + "." + Name` entry; it does not cache individual lookup results.

In the same run, direct member lookup was 1.45 versus 0.41 microseconds, fully qualified lookup 43.36 versus 4.89, and short-type lookup 2.50 versus 12.47. The unchanged `GetMember` control was 0.180 versus 0.196 microseconds. Short names still pay for unsuccessful exact scans before the alias lookup. Compare these paired measurements rather than subtracting timings from the earlier run. Raw output is in ignored `TestResults/ReflectionBenchmarks/ShortAliases`.

Both complete Disharmony suites now pass **1,988 tests**, with three existing skips each. The .NET experiment suite passes **348 tests**, including 181,440 differential comparisons. A regression test verifies that the accidental namespace-qualified spelling is rejected while complete dotted names, `+` nesting, short outer-type names, and short nested-type names still resolve. Earlier prototypes keep their original behavior for comparison.

### Measurements before removing the accidental aliases

The following .NET 10 warm measurements predate the removal of namespace-qualified aliases and use 63 loaded assemblies, including the installed game. Times are microseconds per lookup. The direct/qualified/local/short rows use the same synthetic fixture; the other rows name actual game members.

| Lookup | Baseline | Refactor |
|---|---:|---:|
| Direct member, supplied type | 1.43 | **0.35** |
| Fully qualified member | 38.00 | **4.13** |
| Captured local function, supplied type | 4.50 | **1.78** |
| Short type alias + member | **2.43** | 11.02 |
| Supplied `PawnGenerator`, `GenerateSkills.CreatePassion` | 18.49 | **8.00** |
| Supplied `ConversionUtility`, `ConversionPowerFactor_MemesVsTraits.OffsetFromIdeo` | 2.25 | **1.11** |
| Supplied `GeneDef`, `GetDescriptionFull.*` | 6.91 | **2.23** |
| Supplied `Pawn`, `HasPsylink` | 8.62 | **0.35** |
| Fully qualified `Pawn.HasPsylink` | 19.83 | **1.57** |
| Fully qualified conversion local function | **14.50** | 16.96 |

Direct lookup allocation drops from 1,344 to 352 bytes, and the fully qualified fixture from 32,011 to 3,704 bytes. The unchanged `GetMember` control is 0.162 versus 0.175 microseconds. These runs support the large improvements but do not justify treating small timing differences as precise estimates.

Five fresh-process cold qualified lookups with game assemblies loaded gave a baseline median of **177.2 ms** (168.3–200.7) and a refactor median of **15.1 ms** (14.5–15.7), about **12× faster**. This includes JIT and first lookup initialization, not process launch or game assembly loading. A subsequent short alias still builds the global index; this is not a measured whole-mod or game-startup speedup.

The costs are explicit: short aliases on .NET perform unsuccessful exact scans before consulting the index, and qualified local functions can require extra failed prefix probes. Final game-assembly alias-index rebuild time is effectively unchanged (31.96 versus 31.69 ms); calling-thread allocation falls from 19.51 to 17.96 MB. On Mono, direct lookup improves from 6.40 to 1.69 microseconds, qualified lookup from 32.89 to 5.71, and captured-local lookup from 19.43 to 9.60. Short-alias timings overlap (9.19 versus 8.79); alias-index rebuilding regresses from 23.22 to 29.96 ms. No further lookup limitations are addressed in this change.

Validation before the alias simplification: the complete Disharmony suite passed **1,987 tests on each of .NET 10 and Mono**, with three existing ignored tests on each. The experiment suite passes **347 tests on .NET** and **346 on Mono**, retaining one Mono baseline failure described below. The 181,440 differential comparisons still pass. Nine new production regression tests cover complete dotted paths, deterministic concurrent alias rebuilds, namespace/nesting collisions, string-only assembly disambiguation, local functions, overload filters, and types emitted after an earlier miss. Raw final outputs are under ignored `TestResults/ReflectionBenchmarks/Refactor`.

## Earlier prototype findings

The initial experiments favored **exact type lookup first, legacy aliases on a slow path**, combined with cheaper member selection. It preserves the normal feature combinations while deliberately preferring an exact type over an ambiguous alias. It avoids building the global name index when the fast path succeeds. A short-name lookup or an unresolved name can still force that index to be built, so this is not an unconditional startup saving.

The conservative `members` prototype preserves the existing type resolver and improves direct member lookup from **1.22 to 0.35 microseconds**, and local-function lookup from **4.08 to 2.00 microseconds** in the synthetic .NET 10 run. Direct lookup allocations fall from approximately **1,344 to 352 bytes**. Qualified dotted lookup remains around **22 microseconds** because its namespace-prefix scans are unchanged.

The hybrid is a cold-start optimization, not a universal warm-lookup win. Its synthetic warm qualified lookup is **22.95 microseconds**, versus **21.91** for the baseline. Short-name member lookup gets slower: **7.12 versus 2.26 microseconds**, because unsuccessful exact searches precede the alias lookup.

### Cold qualified lookup with game assemblies loaded

Five fresh processes per variant; median first-call time, including JIT and type-index work. The lookup is the same external fixture name in every process. Dependencies discovered during index construction are part of the cost; initial assembly sets can grow differently.

| Variant | Median | Range |
|---|---:|---:|
| Baseline | 170.5 ms | 168.6–177.8 ms |
| Member-processing changes only | 168.5 ms | 162.0–178.0 ms |
| Lazy alias index | 236.4 ms | 230.1–243.6 ms |
| Exact names only | 10.7 ms | 10.2–11.1 ms |
| Exact-first hybrid with alias fallback | **10.3 ms** | 9.7–11.0 ms |

The hybrid avoids this particular index build and is about **16.6× faster** here. It does not remove the cost of an eventual slow-path lookup. The lazy index's generation retry incorporates dependencies loaded during enumeration, which helps explain why its first call does more work than the baseline despite cheaper subsequent rebuilds.

### Warm real-member cases

.NET 10 microseconds per lookup, reflecting over the installed game assembly:

| Supplied type and member | Baseline | `members` |
|---|---:|---:|
| `PawnGenerator`, `GenerateSkills.CreatePassion` | 17.03 | 9.70 |
| `ConversionUtility`, `ConversionPowerFactor_MemesVsTraits.OffsetFromIdeo` | 1.89 | 1.22 |
| `GeneDef`, `GetDescriptionFull.*` | 6.16 | 2.99 |
| `Pawn`, `HasPsylink` | 8.73 | 0.35 |

The `Pawn` case drops from approximately 10,258 to 352 bytes per lookup by avoiding enumeration of the whole class. Later warm game runs also showed slowdowns in unchanged runtime controls (`GetMember` itself increased from roughly 0.15 to 0.37 microseconds). They do not support a clean warm-game speedup claim for the hybrid; the cold comparison and synthetic comparisons are the useful evidence for that design.

### Supporting work and scaling

In the synthetic .NET run, direct `GetResults` falls from 1.07 to 0.28 microseconds, local-function `GetResults` from 3.15 to 1.57, and `FilterMethods` from 0.32 to 0.17. The lazy alias index reduces warm index reconstruction from 3.75 to 1.51 milliseconds and calling-thread allocation from 2.67 MB to 0.62 MB. That does not imply an improvement in the first cold build.

A namespace miss through the baseline costs 6.50 microseconds with 32 loaded assemblies. The underlying `Type.GetType` miss is 0.66 microseconds, and one `Assembly.GetType` miss is 0.16. With 100 additional static fixture assemblies, the full namespace miss costs 49.65 microseconds. The hybrid doubles that miss work to 99.08 because both paths fail. Result caching reduces it to 0.058 microseconds and scan elimination to 2.35, but those prototypes fail the dynamic-type and forwarding contracts described below.

On Mono, direct member lookup improves from 6.81 to 1.76 microseconds with `members`; local-function lookup improves from 44.33 to 30.19. Hybrid short-name lookup is again slower than baseline (11.21 versus 9.18 microseconds). Small differences between runs should not be treated as significant; raw min/max measurements remain in ignored output.

## Evidence from the supplied profile

`Report20261001-1724.diagsession` contains a 44.9-second trace of a Waylith/CoreCLR RimWorld process. The installed game runtime configuration identifies `10.0.10-waylithclr.dev`; the benchmarks use stock .NET **10.0.12**, plus Mono **6.12 x64** for compatibility and comparison.

There are **210 CPU samples** with `ReflectionTools.GetMembers` on the stack. **179** also contain `GetTypeByName` (85%), and **27** contain `GetResults`. Under `GetMembers`, name construction has 63 inclusive samples, dictionary insertion 21, and string concatenation 15. These counts overlap; they point to type-index construction as a substantial cost, not just repeated assembly scans.

Of 331 samples in the XylXenos constructor, 106 include `GetMembers` (32%). For the Xylib constructor the corresponding numbers are 102 of 437 (23%). These are sampled CPU proportions, not wall-clock startup savings. The trace contains 102,236 process samples overall and reports no lost events. The later paired startup analysis is reported above.

## What each prototype changes

| Variant | Change | Assessment |
|---|---|---|
| `named` | Use `GetMember(name, flags)` rather than enumerate every member; retain an exact-name filter because reflection accepts trailing wildcards. | Conservative. |
| `parsed` | Add index-based string parsing; remove repeated split/join and front-removal work. | Conservative. |
| `members` | Also construct local-function prefixes once, collect results directly, and replace parameter `Zip` with a loop. Keep the original type resolver. | Best isolated member-processing improvement. |
| `lazy` | Also index short names with links to duplicates; check namespaces only for queried names. Keep assembly fallback and replace cache generations on assembly load. | Reduces repeated index construction and allocation; first-use cost can increase while newly loaded dependencies are incorporated. |
| `cached` | Cache type hits and misses; clear on assembly load. | Very fast when warm, but hides types added to an existing dynamic assembly. Not a compatible replacement. |
| `indexed` | Skip plain-name scans of assemblies whose types were fully enumerated. | Misses type forwarders. Not a compatible replacement. |
| `streamlined` | Combine `indexed` with member-processing improvements. | Inherits the forwarding defect. |
| `exact` | Search runtime type names only. | Useful performance bound; drops global short names and flattened nested-type aliases. |
| `hybrid` | Try exact names across the candidate prefixes before the legacy alias search. Use exact-first lookup for colon syntax too. | Retains alias fallback, but changes ambiguous-name precedence. |

The hybrid precedence test uses an external assembly containing both `BenchmarkPrecedence.Target` and `BenchmarkPrecedence.Outer+Target`. The baseline's flattened alias resolves `BenchmarkPrecedence.Target` to the nested type; the hybrid chooses the actual top-level type. An exact match at a later dotted prefix can also outrank an alias at an earlier prefix. These are deliberate tradeoffs, not claims of perfect behavioral equivalence.

The broad lookup matrix covers supplied types, short/full/assembly-qualified names, colon syntax, nested types, static and captured local functions, lambdas, overloads, generic arity and constraints, ref/in/out markers, properties and fields, constructors, inheritance, missing names and invalid arguments. It compares ordered members and exception types/messages. In particular, it preserves shadowing before signature filtering and before accessor conversion.

## Earlier validation and measurement limits

- .NET 10: **338 tests passed**, including **181,440 differential lookup comparisons**.
- Mono: **337 passed, 1 failed**. The retained `EmittedDottedNestedTypeAliasesCanBeFound` test fails against the baseline: Mono's dynamic `Assembly.GetTypes()` omits the emitted nested type, so its short alias is never indexed. Full-name lookup succeeds. This is a baseline/runtime limitation, not a new candidate regression.
- Passing tests for `cached` and `indexed` include explicit demonstrations of their defects; those variants should not be considered compatible because the suite is green.
- No game code is patched by the harness. The optional game cases reflect over installed `Assembly-CSharp.dll` using lookup shapes from XylXenos, including `GenerateSkills.CreatePassion`, `ConversionPowerFactor_MemesVsTraits.OffsetFromIdeo`, and `GetDescriptionFull.*`. They are not a replay of all mod initialization.
- Warm measurements use seven timed batches after cache warmup and calibration, with tiered compilation disabled. Each variant runs in a fresh process. Framework dependencies are preloaded before warm comparisons. CSVs record medians, min/max, iteration counts and assembly counts. First-use measurements use separate processes and include JIT and index construction, but exclude process launch and initial game-assembly loading.
- Allocation figures count only the calling thread; PLINQ worker allocation is excluded. The 100-assembly experiment loads copies of a small static fixture to expose scaling, rather than claiming to model 100 real mods.

## Reproduce

PowerShell 7 and the .NET 10 SDK are required. Run from the repository root; nothing is added to the solution or deployed to mod folders.

```powershell
./Codex/ReflectionBenchmarks/Run-Benchmarks.ps1 -Variants baseline,current -Verify
./Codex/ReflectionBenchmarks/Run-Benchmarks.ps1 -Variants baseline,current -GameDirectory 'C:/Program Files (x86)/Steam/steamapps/common/RimWorld'
./Codex/ReflectionBenchmarks/Run-Benchmarks.ps1 -Variants baseline,current -GameDirectory 'C:/Program Files (x86)/Steam/steamapps/common/RimWorld' -ColdOnly -Repetitions 5
./Codex/ReflectionBenchmarks/Run-Benchmarks.ps1 -Variants baseline,hybrid,cached,indexed -AdditionalAssemblies 100
./Codex/ReflectionBenchmarks/Run-Benchmarks.ps1 -Runtime Mono -Variants baseline,current
```

`-Verify` deliberately reports the retained Mono failure. Run the standalone NUnitLite executable with `--verify --where 'cat != BaselineLimitation'` to run the remaining cases separately. `current` source-links the working production implementation for future comparisons. Generated prototype source is under `obj/Release/<framework>/Experiments`.

To reproduce the profile summary with the locally installed Visual Studio TraceEvent libraries:

```powershell
./Codex/ReflectionBenchmarks/Inspect-Profile.ps1 -TracePath TestResults/Report20261001-1724.diagsession
```

The script defaults to that trace's process ID, 46364. Supply `-ProcessId` and `-PrivateAssemblies` for another trace or Visual Studio installation. ETL extraction, conversion and summaries stay under `TestResults`.
