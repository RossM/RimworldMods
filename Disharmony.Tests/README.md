# Disharmony tests

The library, test project, and test-target fixtures multi-target .NET Framework 4.7.2 (`net472`) and .NET 10
(`net10.0`). Use the .NET 10 SDK to build them. Running `net472` tests through `dotnet test` requires Windows and
.NET Framework 4.7.2 or later; the NUnitLite runner also supports Mono.

## Build and test

Run these commands from the repository root. Omit `--framework` to build or test both targets, or select one:

```powershell
dotnet build .\Disharmony\Disharmony.csproj -p:DeployToMods=false
dotnet test .\Disharmony.Tests\Disharmony.Tests.csproj -p:DeployToMods=false
dotnet test .\Disharmony.Tests\Disharmony.Tests.csproj --framework net472 -p:DeployToMods=false
dotnet test .\Disharmony.Tests\Disharmony.Tests.csproj --framework net10.0 -p:DeployToMods=false
```

Build outputs are separated by configuration and framework under `bin`. The .NET Framework output directory is
`net472` (previously `net4.7.2`). Project references select the matching target automatically. Only the `net472`
library build deploys to the RimWorld mod assembly directories; `-p:DeployToMods=false` disables that copy.
Visual Studio Test Explorer and `dotnet test` run the tests for each target through the NUnit test adapter.

## NUnitLite runners

The script builds only the target required by the selected runtime and runs it in a fresh process:

* `Disharmony.Tests\Run-DisharmonyTests.ps1 -Runtime Clr` runs `net472` on the Microsoft .NET Framework CLR.
* `Disharmony.Tests\Run-DisharmonyTests.ps1 -Runtime Mono` runs `net472` on Mono (the default).
* `Disharmony.Tests\Run-DisharmonyTests.ps1 -Runtime Dotnet` runs `net10.0` on .NET 10 via `dotnet`.

Default NUnitLite result files are saved separately for each runtime under `TestResults\Disharmony`.

Run the script from the repository root. For Mono, it checks `-MonoExecutable`, then `MONO_EXE`, then `mono` on
`PATH`, and finally the standard Windows installation directories. NUnitLite arguments can be supplied after the
script arguments, or explicitly through `-NUnitArguments`.

```powershell
.\Disharmony.Tests\Run-DisharmonyTests.ps1 -Runtime Clr
.\Disharmony.Tests\Run-DisharmonyTests.ps1 -Runtime Dotnet
.\Disharmony.Tests\Run-DisharmonyTests.ps1 -Runtime Mono
.\Disharmony.Tests\Run-DisharmonyTests.ps1 -Runtime Mono --where 'test =~ Optimizer'
```

Pass `-Profile` to request a sampling profile under `TestResults\Disharmony`. `-MonoProfile` accepts another Mono
profiler specification and adds an output path under that directory when the specification does not provide one. The
script verifies that the requested file was created, because some Mono distributions do not include every profiler
module. In particular, the official Mono 6.12 x64 Windows package runs the tests but does not include the `log`
profiler used by these examples.

```powershell
.\Disharmony.Tests\Run-DisharmonyTests.ps1 -Runtime Mono -Configuration Release -Profile
.\Disharmony.Tests\Run-DisharmonyTests.ps1 -Runtime Mono -MonoProfile 'log:alloc,nocalls'
```

## Why the runtime matters

The Microsoft CLR and Mono do not reject all malformed generated IL in the same circumstances. A previous regression
left an unreachable `stloc` with an empty stack after an `AlwaysRun` postfix suppressed an exception and supplied a
result. The Microsoft CLR accepted the method, while Mono correctly rejected it as invalid IL. The defect is fixed, but
it demonstrates why a successful CLR run is not a substitute for running the end-to-end suite on Mono.

Use fresh processes for each runtime. Trampolines, resolved methods, and static state are process-wide and can otherwise
hide first-use behavior.

## Test environment and dependency caveats

Reflection-sensitive targets and all inline patch methods belong in `Disharmony.TestTargets`, which is always
compiled in Release mode so their generated IL is predictable.

At the start of each test run, the assembly-wide setup redirects Harmony's `FileLog` output to
`disharmony-tests.log` beside the built test executable and truncates the previous file. This captures both Harmony
logging and Disharmony logging written through `FileLog`. The exact path is printed to the test runner's progress
output. The writer permits concurrent readers, so the log can be inspected while a test run is still active.

Harmony coexistence tests use explicit, valid `Harmony.Patch` calls and a fixture-specific Harmony ID. Do not use an
invalid Harmony patch or a throwing Harmony transpiler merely to provoke an error: Harmony can emit invalid IL or retain
broken global patch state instead of rolling back, contaminating later tests.

Several boundary expectations reflect limitations below Disharmony rather than intended CLR restrictions. Constructed
generic methods are rejected because MonoMod 1.2.3 reaches an unimplemented `MethodTable.GetMethodDescForSlot` path on
the .NET Framework runtime. Varargs methods are rejected because Harmony generates invalid IL while resolving their
trampolines. The static-constructor test remains ignored because Harmony/MonoMod prepares the target in a way that runs
the type initializer before its patch can be installed.

Tests that inject failures through `HarmonyInterface.ApplyPatchHookForTesting` are compiled only in Debug builds,
matching the lifetime of that test hook. The normal Debug `dotnet test` run includes them; a Release test build does not.
