# Xylib in-game tests

Run from the repository root with PowerShell 5.1 or 7 and the .NET 10 SDK:

```powershell
.\Xylib.Tests\Run-Tests.ps1 -RimWorldPath "C:\Program Files (x86)\Steam\steamapps\common\RimWorld\RimWorld.exe" -DisharmonySourceDirectory ..\Disharmony
```

See [the shared runner documentation](../InGameTests/README.md) for dependencies, filters, results, and isolation. Xylib ships inside XylXenos, so this suite loads that mod and requires Biotech. The example uses a local Disharmony checkout for both compilation and runtime. Omit `-DisharmonySourceDirectory` to test the released package.

Coverage includes GeneWithComps initialization, inherited tick dispatch, activation caching and restrictions, reset/removal, child event listeners, gizmos, stats, vacuum immunity, and a real Scribe deep round trip. EventManager cases cover callback signatures, target routing, priority, duplicate registration, ownership, removal, reset, and Scribe gating. Other cases cover every concrete gene comp, hediff comps, extended stages, lookup cache invalidation, pawn data, tracker aggregation, all gene-set maker implementations, genetic stats, and both XML patch operations.

Tests create synthetic definitions and explicit property values in C#. They assert code behavior; they do not snapshot mod definitions or assert mod XML balance values. The XML patch tests operate on small XML documents created inside the tests.

The initial suite intentionally retains `BiostatTotalMakerRetainsExistingGenesWhileAddingValidSet` as a failing regression: `GeneSetMaker_BiostatTotal.AddGenesInt` calls `RemoveRange(initialGeneCount, genes.Count)`, which throws when existing genes make the starting index nonzero. Product code is unchanged.
