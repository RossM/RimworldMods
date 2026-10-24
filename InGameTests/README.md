# Shared in-game test infrastructure

[Xylib.Tests](../Xylib.Tests/README.md), [XylXenos.Tests](../XylXenos.Tests/README.md), and [Yobai.Tests](../Yobai.Tests/README.md) use SmashPhil's DevTools inside the installed Steam game. Each suite has a small `Run-Tests.ps1` entry point forwarding to [Run-Tests.ps1](Run-Tests.ps1). The new suites share MSBuild configuration and a linked pawn fixture; no test infrastructure ships in gameplay assemblies.

## Dependencies and execution

Use PowerShell 5.1 or 7 and .NET SDK 10.0.400 or later. Install the Workshop [Harmony mod](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077). Runs using the published Disharmony package also need the Workshop [Disharmony mod](https://steamcommunity.com/sharedfiles/filedetails/?id=3818084845). The first run downloads a pinned DevTools revision and restores NuGet packages. DevTools remains cached in `temp/Yobai.TestDependencies` so existing Yobai builds keep working.

The launcher builds the selected suite, creates a new profile under `TestResults/<suite>`, and launches RimWorld headlessly. It temporarily junctions the required mods into the game's `Mods` folder and restores existing local folders in `finally`. Normal saves and settings are untouched. Run suites sequentially because they share game mod paths and build outputs.

`-RimWorldPath` defaults to `C:\Program Files (x86)\Steam\steamapps\common\RimWorld\RimWorld.exe`. Omit it for a standard Steam installation, or pass a different path to override it.

The launcher defaults to the released Disharmony package, ignoring `Directory.Build.local.props` for reproducibility. Pass `-DisharmonySourceDirectory <checkout>` to Xylib/XylXenos when testing a local Disharmony version. Relative paths are resolved from the shell's working directory. This builds the source reference and stages matching runtime assemblies in the isolated run directory, without editing Workshop files.

All installed expansions are enabled by default. Xylib and XylXenos need Biotech; `-CoreOnly` remains available for Yobai. The new tests mostly invoke one callback or one interval directly instead of simulating long periods. Full suites initially took about 3–5 seconds each after map creation; game startup, dependency restore, and compilation are additional.

## Filtering and results

```powershell
.\Xylib.Tests\Run-Tests.ps1 -RimWorldPath "C:\path\to\RimWorld.exe" -DisharmonySourceDirectory ..\Disharmony -Filter 'method == TypedCallbacksReceiveTargetAndData'
```

`-OutputDirectory` selects a new results folder and `-TimeoutMinutes` changes the ten-minute game timeout. Each run produces `Test.log`, `stdout.log`, `stderr.log`, and its isolated `UserData` profile. Start with `Test.log`; failed assertions and unexpected warnings/errors fail the run. Build failures, empty selections, timeouts, process failures, and incomplete reports also fail the command. The two new suites retain known product regressions, documented in their READMEs, so their full runs currently return failure.

During launcher extraction, Yobai's unchanged Core-only suite had 106 passes and one failure in `UnrelatedAdultMakesRoomBarracks`. That case also failed with the original launcher: `IsBedInAssignedPolyculeBedroom` checks room membership and partner ownership without checking the room's barracks classification. Its test and product code were left unchanged.

## Adding cases

Create input definitions in C#, specifying values relevant to the behavior under test. `PawnTestFixture` registers synthetic gene and hediff defs, initializes a healthy adult baseliner, and removes test objects, listeners, and definitions afterward. Its shared builders are used by many tests. Keep individual cases self-contained and restore any map/global state they change in `finally`.

Use real public game operations for integration checks, such as adding/removing genes or hediffs. Reflection is limited to inaccessible game setup/cleanup, protected entry points, and isolated generator logic. Save tests should use `Scribe_Deep` when reference resolution is required, register reused live objects, and always stop Scribe and delete their temporary files. Keep `RandBlock` inside a single frame. Do not weaken a valid test to accommodate a product bug.
