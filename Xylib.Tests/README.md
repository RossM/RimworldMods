# Xylib in-game tests

Run from the repository root with PowerShell 5.1 or 7 and the .NET 10 SDK:

```powershell
.\Xylib.Tests\Run-Tests.ps1 -Expansions None -DisharmonySourceDirectory ..\Disharmony
```

See [the shared runner documentation](../InGameTests/README.md) for dependencies, filters, results, and isolation. This suite is a standalone test mod containing Xylib and its shared definitions. It loads neither XylXenos nor XylIdeos and has no DLC requirement. The example uses a local Disharmony checkout for both compilation and runtime. Omit `-DisharmonySourceDirectory` to use your normal build settings, or pass an empty string to test the released package. The test project builds normally in Visual Studio and writes its mod to `ModBin`; use `-SkipBuild` to run that output. Mod links remain installed for debugging.

General gene/component tests remain available without Biotech: human pawns have a gene tracker even when the DLC is disabled. Six gene-generation cases use `[LoadIfBiotechActive]` because they exercise vanilla `GeneSet.CanAddGeneDuringGeneration`, which rejects generation unless Biotech is active. Run with `-Expansions Biotech` to include those cases, or omit `-Expansions` to enable all installed expansions.

Coverage includes GeneWithComps initialization, inherited tick dispatch, activation caching and restrictions, reset/removal, child event listeners, gizmos, stats, vacuum immunity, and a real Scribe deep round trip. EventManager cases cover callback signatures, target routing, priority, duplicate registration, ownership, removal, reset, and Scribe gating. Other cases cover every concrete gene comp, hediff comps, extended stages, lookup cache invalidation, pawn data, tracker aggregation, all gene-set maker implementations, genetic stats, and both XML patch operations.

Tests create synthetic definitions and explicit property values in C#. They assert code behavior; they do not snapshot mod definitions or assert mod XML balance values. The XML patch tests operate on small XML documents created inside the tests.

`BiostatTotalMakerRetainsExistingGenesWhileAddingValidSet` covers generation for a pawn that already has genes, preserving regression coverage for the range-removal bug found by the initial suite.
