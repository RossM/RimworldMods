# Yobai in-game tests

This project runs Yobai's tests inside RimWorld 1.7 using [SmashPhil's DevTools](https://github.com/SmashPhil/DevTools/tree/develop/DevTools/Testing).

## Running the tests

Use Windows PowerShell 5.1 or PowerShell 7, with the .NET 10 SDK. The command also works in Visual Studio's PowerShell console. From the repository root, run:

```powershell
.\Yobai.Tests\Run-Tests.ps1 -RimWorldPath "C:\Program Files (x86)\Steam\steamapps\common\RimWorld\RimWorld.exe"
```

Point the command at your Steam installation, with the normal Workshop versions of [Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077) and [Disharmony](https://steamcommunity.com/sharedfiles/filedetails/?id=3818084845) installed. Tests use those installed dependencies and the game Steam updates. No separate test installation or preconfigured test environment is needed.

The command builds Yobai in Release configuration against the released Disharmony NuGet package, downloads and builds DevTools, and starts the installed game in headless mode. Yobai keeps its normal package ID and dependency declarations; Disharmony runs as a separate Workshop mod. Local build settings are ignored so a development checkout of Disharmony cannot replace the released package.

Each run gets a fresh user-data directory and mod configuration, leaving your normal saves and settings alone. The command temporarily installs links to Yobai, DevTools and the test mod in the game's `Mods` directory. Existing folders with those names are moved aside for the run, then restored when the command exits. Workshop dependencies are used in place, without copies or metadata changes.

The first run needs internet access for DevTools and NuGet restore. The command also needs permission to create junctions and temporarily move local mod folders within the game directory. DevTools is cached under `temp/Yobai.TestDependencies`; the cache can be deleted and will be recreated on the next run. No local build settings, installed test mods, prior build outputs or dependency checkouts are required.

All installed DLC is enabled by default. Optional arguments:

- `-CoreOnly` runs without DLC.
- `-Filter 'method == IncomingOwnerKeepsAssignedSleepingSlot'` runs one test. DevTools filter expressions containing spaces are supported.
- `-OutputDirectory "C:\path\to\new-results-folder"` chooses a new output folder.
- `-TimeoutMinutes 20` changes the default ten-minute game timeout.

Each run keeps its `Test.log`, `stdout.log`, `stderr.log`, isolated user data and a copy of the test mod under a new folder in `TestResults/Yobai`. A failed build, game failure, timeout, empty test selection or incomplete report makes the command fail. Temporary game-directory links are cleaned up after successful and failed runs; reports remain available for diagnosis.

The test project is included in the solution but unchecked for default Debug and Release builds, so normal mod builds do not need DevTools. After running the command once, the project can resolve DevTools from the downloaded dependency build without additional configuration.

See [Verification results](VERIFICATION.md) for headless results and remaining checks.

## Test coverage

Active-assignment regressions check that temporary sleepers release their permanent slots for night visits, transfers leave one assignment across all bed comps, rejected transfers preserve the previous stay, and returning owners cannot displace temporary sleepers. They also distinguish actual travel and occupancy from native reservations, including a path redirected to a different slot. Ordinary unowned sleepers can claim beds; temporary sleepers remain unowned. Bedroom selection protects a child roommate's permanent place while allowing an unowned place in a polycule member's bed. Added and revised tests explain the assignment or feature rule behind their expectations.

Think-tree regressions cover automatic visits starting vanilla lovin' and resuming sleep, both partners' cooldowns, the hourly opportunity limit, starvation, a partner leaving or going on cooldown during travel, reservation rollback, and the absence of bedtime visits. A visitor can also leave a current bed shared with a non-romantic companion, including one still on the way; those cases run through lovin' and the return to sleep while preserving ownership. A save regression checks that cancelled assignments are discarded without a periodic map scan. A test-only three-place bed checks visits with multiple sleeping partners, selection of a ready host when another is on cooldown, compatibility with occupants and incoming owners, and booking the last free place. Visits with one romantic host are allowed when another willing bedmate is already there or on the way. Separate ideology-restricted cases check that an unwilling bedmate still blocks the visit. These tests do not require another bed mod.

The tests check who belongs to a polycule, when rooms count as bedrooms, which beds pawns choose, and whether two arriving pawns receive different sleeping slots. They run the game's rest job giver and check that temporary sleeping assignments work before a job starts, survive saving and loading, and are released when a trip is cancelled. A separate case uses a normal vanilla sleep job with a temporary assignment. Lovin' tests check that partners keep their sleeping positions and bed assignments, then return to sleep. Night visit tests check that guests need a free unassigned place, while pawns sharing a polycule bedroom can still use assigned places. The toggle, Ideology, medical beds and forbidden beds have separate cases. Further cases check that ordinary bed and ground sleep retain vanilla reservations, temporary sleepers use their assigned slots without reserving map cells, vanilla reservation failures are preserved, admission rules are not repeated during continuation, a rejected booking preserves an existing stay, and bedmate compatibility still applies after relationships change.

Bedroom regressions also check that prohibited spouses, lovers and fiancés still count as vanilla bedroom groups when those relationships do not connect Yobai's physical-partner polycule.

Review regressions also cover a returning owner's preference for an empty bed, ground-sleep fallback, caravan restrictions, medical conversion, parent changes, and resurrection. Last-resort sharing tests cover occupants and incoming sleepers, preference for empty beds and direct partners, unchanged ownership, the bedroom boundary, unrelated occupants, and spouse-only ideoligions. A separate case runs prohibited lovers through vanilla lovin' and checks its penalties.

A separate regression reserves a sleeping cell for another job, then checks that a temporary sleeper can still use that slot without changing its bed assignment or the other job's reservation. Vanilla ground sleep retains its normal cell reservation.

Room-role tests explicitly request a fresh calculation after changing relationships or resurrecting a pawn. They check vanilla's classification rules; vanilla manages its room-role cache.

Arrival regressions cover permanent owners and temporary sleepers competing for the same place in either order, compatible owners using a different free place, cancellation, relationship changes before either sleeper arrives, incoming-partner preference, and a host staying put for an incoming partner. Owner-slot regressions check each of a three-place bed's owners prefers their assigned place when all places are free, and can use another place if their own is occupied or booked. Returning-owner night visits also prefer the assigned place, including when the bed is fully assigned or its unassigned place is booked. A booked owner place is unavailable to its owner, and the toggle can prevent return visits. Reservations for work on a bed do not count as sleep bookings. An unexpected-occupant case checks that a booked place remains unusable if someone moves into it after admission.

Bedmate rejection tests use explicit spouse-only ideoligions and check vanilla's willingness rule before exercising Yobai. Separate cases cover willing non-partners, sharing after a breakup, and incoming owners without ideology restrictions. Polycule bed selection still requires the assigned bedroom, and night visits still require romantic partners.

Each test sets up its own pawns and beds. Automatic rest and lovin' decisions are postponed so they do not interfere with the jobs the test starts.

See [Verification results](VERIFICATION.md) for the game version tested, results and checks still to do.
