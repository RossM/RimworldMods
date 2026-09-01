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
