# Yobai in-game tests

This project runs Yobai's tests inside RimWorld 1.7 using [SmashPhil's DevTools](https://github.com/SmashPhil/DevTools/tree/develop/DevTools/Testing). The project is included in the solution, but is unchecked for the default Debug and Release builds so normal mod builds do not need DevTools. Build it separately using the command below, or enable it in Visual Studio's Configuration Manager after configuring DevTools.

## Configuring DevTools

Build the `develop` branch of DevTools against RimWorld 1.7. Set its DLL path in `Directory.Build.local.props` at the repository root so Visual Studio and command-line builds can find it:

```xml
<Project>
  <PropertyGroup>
    <DevToolsAssemblyPath>C:\path\to\DevTools.dll</DevToolsAssemblyPath>
  </PropertyGroup>
</Project>
```

If that file already contains local Disharmony settings, add the property to its existing `PropertyGroup`. The file is ignored by Git. Reload the test project if Visual Studio does not pick up the new setting.

## Running with PowerShell

Use PowerShell 7. Configure DevTools and a test installation as described here, then run this command from the repository root:

```powershell
.\Yobai.Tests\Run-Tests.ps1 -RimWorldPath "C:\path\to\test-installation\RimWorld.exe"
```

The runner reads `DevToolsAssemblyPath` from the project settings; `-DevToolsAssemblyPath` overrides it for one run. The script builds Yobai and its test mod, runs the tests without opening a window, and prints the result and report location. It uses `temp/YobaiSaveData` for RimWorld's user data by default. Use `-SaveDataDirectory` to select another isolated directory with its own mod configuration.

Add `-CoreOnly` to temporarily disable DLC. Add `-Filter 'method == IncomingOwnerKeepsAssignedSleepingSlot'` to run a particular test. `-SkipBuild` uses the existing build, and `-TimeoutMinutes` changes the default ten-minute limit.

Each run writes `Test.log`, `stdout.log` and `stderr.log` to a new folder under `TestResults/Yobai`. Use `-OutputDirectory` to choose a different new folder. The script fails if the build or game fails, no tests pass, or the test runner does not finish. It restores the mod list and DevTools reporting configuration when it exits.

## Setting up the test installation

Build the test project once to create `ModBin/Yobai` and `ModBin/Yobai.Tests`:

```powershell
dotnet build .\Yobai.Tests\Yobai.Tests.csproj
```

Link those two mod folders into the test installation's `Mods` directory so the game uses the current build and the runner's reporting configuration. Install Harmony and DevTools there too. Enable these mods in order:

1. Harmony for RimWorld 1.7
2. Core and any DLC you want to test
3. Disharmony, if Yobai was built using the NuGet package
4. Yobai
5. DevTools
6. Yobai tests

DevTools needs the full Harmony mod for its stack-trace settings, even when Yobai already includes the Harmony DLL.

Use RimWorld's `-savedatafolder` option to create the test installation's settings in an isolated directory. The runner expects its `Config/ModsConfig.xml` to exist. Include all installed DLC IDs in `knownExpansions`; otherwise RimWorld may enable newly discovered expansions automatically. The runner handles this for `-CoreOnly` and restores the original file afterwards.

Run once with Ideology and Biotech enabled to cover ideology and preservation of vanilla bedroom rules, then once with `-CoreOnly`.

## Running manually

You can also run the tests from DevTools' Test Explorer or launch RimWorld directly:

```powershell
& "C:\path\to\RimWorld.exe" -NoWindow -savedatafolder="C:\path\to\isolated-test-data" --pid xylthixlm.yobai.tests --test --exit
```

The runner and this command use `-NoWindow` because the tested RimWorld build logs a zero-resolution UI error with `-Headless`, which DevTools treats as a test failure. See [Verification results](VERIFICATION.md) for the version and results.

For manual runs, DevTools chooses its report folder separately from RimWorld's save folder. To keep the report with your test data, create `Configs/UnitTestConfig.xml` inside the installed Yobai tests mod:

```xml
<TestConfig>
  <randSeed>1234567</randSeed>
  <log><folder>C:\path\to\isolated-test-data</folder></log>
</TestConfig>
```

## Test coverage

The tests check who belongs to a polycule, when rooms count as bedrooms, which beds pawns choose, and whether two arriving pawns receive different sleeping slots. They run the game's rest job giver and check that temporary sleeping assignments work before a job starts, survive saving and loading, and are released when a trip is cancelled. A separate case uses a normal vanilla sleep job with a temporary assignment. Lovin' tests check that partners keep their sleeping positions and bed assignments, then return to sleep. Night visit tests check that visitors need a free unassigned place, while pawns sharing a polycule bedroom can still use assigned places. The toggle, Ideology, medical beds and forbidden beds have separate cases. Further cases check that ordinary bed and ground sleep retain vanilla reservations, temporary sleepers use their assigned slots without reserving map cells, vanilla reservation failures are preserved, admission rules are not repeated during continuation, a rejected booking preserves an existing stay, and bedmate compatibility still applies after relationships change.

Bedroom regressions also check that prohibited spouses, lovers and fiancés still count as vanilla bedroom groups when those relationships do not connect Yobai's physical-partner polycule.

Review regressions also cover a returning owner's preference for an empty bed, ground-sleep fallback, caravan restrictions, medical conversion, parent changes, and resurrection. Last-resort sharing tests cover occupants and incoming sleepers, preference for empty beds and direct partners, unchanged ownership, the bedroom boundary, unrelated occupants, and spouse-only ideoligions. A separate case runs prohibited lovers through vanilla lovin' and checks its penalties.

A separate regression reserves a sleeping cell for another job, then checks that a temporary sleeper can still use that slot without changing its bed assignment or the other job's reservation. Vanilla ground sleep retains its normal cell reservation.

Room-role tests explicitly request a fresh calculation after changing relationships or resurrecting a pawn. They check vanilla's classification rules; vanilla manages its room-role cache.

Arrival regressions cover permanent owners and temporary sleepers competing for the same place in either order, compatible owners using a different free place, cancellation, relationship changes before either sleeper arrives, incoming-partner preference, and a host staying put for an incoming partner. Reservations for work on a bed do not count as sleep bookings. An unexpected-occupant case checks that a booked place remains unusable if someone moves into it after admission.

Each test sets up its own pawns and beds. Automatic rest and lovin' decisions are postponed so they do not interfere with the jobs the test starts.

See [Verification results](VERIFICATION.md) for the game version tested, results and checks still to do.
