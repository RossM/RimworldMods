# Disharmony tests

The test project targets .NET Framework 4.7.2 and supports three ways to run the same tests:

* Visual Studio Test Explorer and `dotnet test` use the Microsoft CLR through the NUnit test adapter.
* `Source_Disharmony.Tests\Run-DisharmonyTests.ps1 -Runtime Clr` uses the embedded NUnitLite runner on the Microsoft CLR.
* `Source_Disharmony.Tests\Run-DisharmonyTests.ps1 -Runtime Mono` uses the embedded NUnitLite runner on Mono.

Run the script from the repository root. For Mono, it checks `-MonoExecutable`, then `MONO_EXE`, then `mono` on
`PATH`, and finally the standard Windows installation directories. NUnitLite arguments can be supplied after the
script arguments, or explicitly through `-NUnitArguments`.

```powershell
.\Source_Disharmony.Tests\Run-DisharmonyTests.ps1 -Runtime Mono
.\Source_Disharmony.Tests\Run-DisharmonyTests.ps1 -Runtime Mono --where 'test =~ Optimizer'
```

Pass `-Profile` to request a sampling profile under `TestResults\Disharmony`. `-MonoProfile` accepts another Mono
profiler specification and adds an output path under that directory when the specification does not provide one. The
script verifies that the requested file was created, because some Mono distributions do not include every profiler
module. In particular, the official Mono 6.12 x64 Windows package runs the tests but does not include the `log`
profiler used by these examples.

```powershell
.\Source_Disharmony.Tests\Run-DisharmonyTests.ps1 -Runtime Mono -Configuration Release -Profile
.\Source_Disharmony.Tests\Run-DisharmonyTests.ps1 -Runtime Mono -MonoProfile 'log:alloc,nocalls'
```

## Why both runtimes matter

The Microsoft CLR and Mono do not reject all malformed generated IL in the same circumstances. A previous regression
left an unreachable `stloc` with an empty stack after an `AlwaysRun` postfix suppressed an exception and supplied a
result. The Microsoft CLR accepted the method, while Mono correctly rejected it as invalid IL. The defect is fixed, but
it demonstrates why a successful CLR run is not a substitute for running the end-to-end suite on Mono.

Use fresh processes for the two runs. Trampolines, resolved methods, and static state are process-wide and can otherwise
hide first-use behavior.

## Test environment and dependency caveats

Reflection-sensitive targets and all inline patch methods belong in `Source_Disharmony.TestTargets`, which is always
compiled in Release mode so their generated IL is predictable.

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
