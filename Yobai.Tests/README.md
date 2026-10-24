# Yobai in-game tests

The tests run inside RimWorld using [SmashPhil's DevTools](https://github.com/SmashPhil/DevTools/tree/develop/DevTools/Testing). They exercise the game's jobs, think trees, reservations and save system alongside Yobai. The launcher is shared with Xylib and XylXenos in [InGameTests](../InGameTests/README.md).

## Running the tests

Use Windows PowerShell 5.1 or PowerShell 7 with the .NET 10 SDK. Install the normal Workshop versions of [Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077) for DevTools and [Disharmony](https://steamcommunity.com/sharedfiles/filedetails/?id=3818084845) for Yobai. From the repository root, run:

```powershell
.\Yobai.Tests\Run-Tests.ps1 -RimWorldPath "C:\Program Files (x86)\Steam\steamapps\common\RimWorld\RimWorld.exe"
```

Point the command at your Steam installation. The script builds Yobai and DevTools, creates an isolated user-data directory, and runs the installed game headlessly. It temporarily links the test mods into the game's `Mods` directory and restores existing local mod folders on exit. Your normal saves and settings are kept separate.

The first run needs internet access for DevTools and NuGet restore, plus permission to create junctions and move local mod folders in the game directory. The script creates its own build outputs, dependency cache and test profile. DevTools is pinned to the revision specified in [the shared runner](../InGameTests/Run-Tests.ps1) and cached under `temp/Yobai.TestDependencies`; deleting the cache causes it to be downloaded and built again.

Tests use the released Disharmony NuGet package and the installed Workshop mods. Local Disharmony build overrides are ignored. The test project is included in the solution but excluded from its default builds. After running the script, Visual Studio can resolve DevTools from the dependency cache.

All installed DLC is enabled by default. Options:

| Argument | Purpose |
| --- | --- |
| `-CoreOnly` | Run without DLC. |
| `-Filter 'method == IncomingOwnerKeepsAssignedSleepingSlot'` | Run a selected test using a DevTools filter expression. |
| `-OutputDirectory "C:\path\to\new-results-folder"` | Choose a new output folder. |
| `-TimeoutMinutes 20` | Change the default ten-minute game timeout. |

## Results and failures

Each run writes `Test.log`, `stdout.log`, `stderr.log` and its isolated profile to a new directory under `TestResults/Yobai`. Start with `Test.log` for failed assertions and use the game logs for startup failures or logged errors. Reports are local build artifacts and are ignored by Git.

The command fails for build errors, game failures, timeouts, an empty test selection, or an incomplete or failing report. Temporary mod links are cleaned up when the script exits, including after a failed run.

## Coverage

- Polycule membership, vanilla bedroom classification, Ideology restrictions and parent-child bedrooms.
- Bed preferences, random choice, owner-slot preference, incoming sleepers and last-resort sharing.
- Map-scoped active assignments, transfers, cancellation, ownership preservation and ordinary bed claims.
- Night-visit chance and cooldowns, compatible non-romantic companions, multiple hosts and beds with more than two slots.
- Travel cancellation, vanilla lovin' effects, sleep continuation, bed restrictions and the night-visit toggle.
- Scribe round trips for bed-comp settings and temporary assignments.

The three-slot bed is defined in [Defs/TestBeds.xml](Defs/TestBeds.xml), so those cases do not need another bed mod. DLC-specific cases are skipped when their DLC is disabled.

## Writing tests

Fixtures create their own pawns and beds and postpone autonomous sleep and lovin' decisions so tests can control job startup. Use the real rest job giver when testing bed selection, and start jobs when a fixture needs an ongoing incoming sleeper.

- A native reservation alone does not retain a temporary assignment. After its creation tick, the pawn must occupy the slot or travel to it. Advance a tick when testing expiry.
- Use explicit ideoligions when testing restrictions. Vanilla can allow non-partners to share a bed. Set spouse-count precepts before converting a pawn, since conversion can remove relationships.
- Keep `RandBlock` scopes within a single frame; do not yield inside them.
- Jobs are pooled. Use job IDs when tracking a particular job across ticks.
- A forbidden-bed fixture needs `CompForbiddable`; verify that forbidding took effect before testing rejection.

Comp-only Scribe tests do not replace whole-colony save/load checks. For persistence or UI changes, also load a saved colony and check the bed toggle in game. For compatibility work, test with the relevant gameplay mods and bed definitions.
