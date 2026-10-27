# Shared in-game test infrastructure

[Xylib.Tests](../Xylib.Tests/README.md), [XylXenos.Tests](../XylXenos.Tests/README.md), and [Yobai.Tests](../Yobai.Tests/README.md) use SmashPhil's DevTools inside the installed Steam game. Each suite has a small `Run-Tests.ps1` entry point forwarding to [Run-Tests.ps1](Run-Tests.ps1). All suites share MSBuild configuration; Xylib and XylXenos also share a linked pawn fixture; no test infrastructure ships in gameplay assemblies.

## Dependencies and execution

Use PowerShell 5.1 or 7 and .NET SDK 10.0.400 or later. Install the Workshop [Harmony mod](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077). Runs using the published Disharmony package also need the Workshop [Disharmony mod](https://steamcommunity.com/sharedfiles/filedetails/?id=3818084845). The first build downloads a pinned DevTools revision and restores NuGet packages. DevTools source remains cached in `temp/Yobai.TestDependencies`; its mod is built to `ModBin/DevTools`.

Build the solution normally in Visual Studio or with `dotnet build .\XylRimworldMods.sln`. All three test projects are enabled in Debug and Release. Their DevTools project dependency downloads and builds the pinned source automatically, so the launcher is not needed to prepare a Visual Studio build. For a nonstandard game installation, set `RimWorldDirectory` in `Directory.Build.local.props` (or pass `-p:RimWorldDirectory=...` to MSBuild).

Each test mod is written to `ModBin/<suite>.Tests`, including its definitions, default test configuration, DLL, and PDB. Xylib.Tests contains its own copy of Xylib and the shared definitions, so it loads without either gameplay mod. Local Disharmony builds bundle the matching runtime assemblies, just like the gameplay mods.

The launcher builds the selected suite in Debug by default, creates a new profile under `TestResults/<suite>`, and launches RimWorld headlessly. It creates persistent directory junctions from the game's `Mods` folder to the required ModBin outputs and reuses existing junctions or symbolic links with the same targets. Links stay installed after success or failure, so the mods remain available for debugging and manual runs through DevTools. Conflicting folders or links cause an error; update them manually. Normal saves and settings are untouched. Run suites sequentially because they share game mod paths, configurations, and build outputs.

`-RimWorldPath` defaults to `C:\Program Files (x86)\Steam\steamapps\common\RimWorld\RimWorld.exe`. Omit it for a standard Steam installation, or pass a different path to override it. Creating links requires write access to the game's `Mods` folder.

The launcher uses the same `Directory.Build.local.props` as Visual Studio. Pass `-DisharmonySourceDirectory <checkout>` to override the local source path (relative paths are resolved from the shell's working directory), or `-DisharmonySourceDirectory ''` to use the released package. Local builds load the bundled runtime without enabling a separate Disharmony mod or changing Workshop files.

Use `-Configuration Release` for a Release build. Use `-SkipBuild` to run the existing ModBin output after building in Visual Studio. The launcher selects bundled or Workshop Disharmony based on that output. The launcher writes the report destination into the built mod's `Configs/UnitTestConfig.xml` and leaves it there for debugging. A normal build restores the default test configuration, which writes reports to the game's user-data folder.

```powershell
.\Xylib.Tests\Run-Tests.ps1 -SkipBuild -Expansions None
```

All installed expansions are enabled by default. Use `-Expansions` with a comma-separated list of case-insensitive names to select expansions, or `-Expansions None` to enable Core without expansions. Core is always enabled. Unknown or uninstalled expansions are rejected before building or launching the game. Only XylXenos requires Biotech; Xylib and Yobai support Core-only runs. Duplicate names are loaded once, in the game's normal expansion order. PowerShell arrays and quoted comma-separated strings are both accepted.

```powershell
.\XylXenos.Tests\Run-Tests.ps1 -Expansions Biotech
.\XylXenos.Tests\Run-Tests.ps1 -Expansions Royalty,Biotech
.\Xylib.Tests\Run-Tests.ps1 -Expansions None
.\Yobai.Tests\Run-Tests.ps1 -Expansions None
```

The new tests mostly invoke one callback or one interval directly instead of simulating long periods. Full suites initially took about 3–5 seconds each after map creation; game startup, dependency restore, and compilation are additional.

## Filtering and results

```powershell
.\Xylib.Tests\Run-Tests.ps1 -RimWorldPath "C:\path\to\RimWorld.exe" -DisharmonySourceDirectory ..\Disharmony -Filter 'method == TypedCallbacksReceiveTargetAndData'
```

`-OutputDirectory` selects a new results folder and `-TimeoutMinutes` changes the ten-minute game timeout. Each run produces `Test.log`, `stdout.log`, `stderr.log`, and its isolated `UserData` profile. Start with `Test.log`; failed assertions and unexpected warnings/errors fail the run. Build failures, empty selections, timeouts, process failures, and incomplete reports also fail the command.

XylXenos allows the exact `Failed to find faction base tile for <generated faction name>` error through DevTools' `errorsAllowed` filter. Its default 5% test world can lack the pollution required for a Trog settlement. The error remains in game logs; other errors and warnings still fail tests. This exception does not apply to standalone Xylib or Yobai runs.

During launcher extraction, Yobai's unchanged Core-only suite had 106 passes and one failure in `UnrelatedAdultMakesRoomBarracks`. That case also failed with the original launcher: `IsBedInAssignedPolyculeBedroom` checks room membership and partner ownership without checking the room's barracks classification. Its test and product code were left unchanged.

## Adding cases

Create input definitions in C#, specifying values relevant to the behavior under test. `PawnTestFixture` registers synthetic gene and hediff defs, initializes a healthy adult pawn (a baseliner when Biotech is active), and removes test objects, listeners, and definitions afterward. Its shared builders are used by many tests. Keep individual cases self-contained and restore any map/global state they change in `finally`. Use DevTools' `[LoadIfBiotechActive]`, `[LoadIfRoyaltyActive]`, and similar attributes only for cases that actually require the DLC.

Use real public game operations for integration checks, such as adding/removing genes or hediffs. Reflection is limited to inaccessible game setup/cleanup, protected entry points, and isolated generator logic. Save tests should use `Scribe_Deep` when reference resolution is required, register reused live objects, and always stop Scribe and delete their temporary files. Keep `RandBlock` inside a single frame. Do not weaken a valid test to accommodate a product bug.
