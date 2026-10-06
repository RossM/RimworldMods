# Disharmony reflection benchmarks

This standalone harness compares reflection lookup implementations using synthetic fixtures.
It targets the same runtimes as the Disharmony test suite and does not deploy to mod folders.

## Run

PowerShell 7 and the .NET 10 SDK are required. Run from the repository root:

```powershell
./Codex/ReflectionBenchmarks/Run-Benchmarks.ps1 -Variants baseline,current -Verify
./Codex/ReflectionBenchmarks/Run-Benchmarks.ps1 -Variants baseline,current -ColdOnly -Repetitions 5
./Codex/ReflectionBenchmarks/Run-Benchmarks.ps1 -Variants baseline,hybrid,cached,indexed -AdditionalAssemblies 100
./Codex/ReflectionBenchmarks/Run-Benchmarks.ps1 -Runtime Mono -Variants baseline,current
```

Results are written under ignored `TestResults/ReflectionBenchmarks`. Each variant runs in a fresh
process. Cold measurements include JIT and first-lookup initialization, but exclude process launch.
Warm measurements use seven timed batches after calibration, with tiered compilation disabled.
Allocation figures count only the calling thread, excluding parallel workers. Additional assemblies
are copies of a small static fixture for studying scaling.

## Implementations

| Variant | Purpose |
|---|---|
| `baseline` | Original reflection lookup implementation. |
| `named` | Look up members by name while retaining exact-name filtering. |
| `parsed` | Reduce repeated string splitting and joining. |
| `members` | Also streamline local-function lookup and parameter matching. |
| `lazy` | Build short-name indexes lazily, replacing generations after assembly loads. |
| `cached` | Experimental result cache; misses types added to an existing dynamic assembly. |
| `indexed` | Experimental assembly-scan elimination; misses type forwarders. |
| `streamlined` | Combine member processing with `indexed`; retains its forwarding defect. |
| `exact` | Runtime names only; deliberately omits short-name aliases. |
| `hybrid` | Try exact type names before legacy aliases. |
| `current` | Source-linked production implementation. |

The production implementation prefers complete runtime type names and resolves ambiguous short names
in a deterministic order. Namespace-qualified nested types must include their containing types.
Use `+` to disambiguate nesting and an assembly-qualified type before `:` to select an assembly.
Short-name lookups may pay for unsuccessful exact scans before building or consulting the alias index.

Generated prototype sources live under `obj/Release/<framework>/Experiments`. The compatibility suite
compares member ordering and errors across lookup combinations, and includes the production type
resolution regression tests. Passing demonstrations of the `cached` and `indexed` defects do not
make those experimental implementations compatible replacements.

## Interpretation limits

These are exploratory comparisons, subject to the guidance in `../README.md`. Their assertions are
not product requirements. In particular, the baseline comparison expects identical exceptions for
invalid member kinds, while the current implementation can return early when no candidates exist.

`EmittedDottedNestedTypeAliasesCanBeFound` is retained even though Mono can omit the emitted nested
type from dynamic `Assembly.GetTypes()`, causing its short-name lookup to fail against the baseline.
`-Verify` reports this failure. To inspect the remaining tests separately, run the NUnitLite executable
with `--verify --where 'cat != BaselineLimitation'`.
