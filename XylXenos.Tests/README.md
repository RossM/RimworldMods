# XylXenos in-game tests

Run from the repository root with PowerShell 5.1 or 7 and the .NET 10 SDK:

```powershell
.\XylXenos.Tests\Run-Tests.ps1 -RimWorldPath "C:\Program Files (x86)\Steam\steamapps\common\RimWorld\RimWorld.exe" -DisharmonySourceDirectory ..\Disharmony
```

See [the shared runner documentation](../InGameTests/README.md) for dependencies, filters, results, and isolation. Biotech is required. Royalty-specific cases use `[LoadIfRoyaltyActive]` and are loaded only when Royalty is enabled. The example uses a local Disharmony checkout for both compilation and runtime. Omit `-DisharmonySourceDirectory` to test the released package.

Coverage includes every gene comp and hediff comp, specialized hediffs and givers, wetness/lovin' needs, thought workers, gene tracker aggregation, ability effects, blindness damage, gene/entropy conditionals, ability targeting, wetness/diet job selection, milking eligibility, mental-state targeting, hostility cooldowns, psylink and xenotype scenario helpers, petrification surgery, name generation, skin-shader selection, gene templates, stock delegation, and spectrum interpolation.

Tests create synthetic gene/hediff definitions and configure their own thresholds, rates, counts, and factors. Assertions do not depend on XylXenos XML values remaining unchanged. Vanilla definitions supply normal pawns, injuries, food, and other game primitives; expected nutrition and body-part health are calculated from the fixture at runtime.

The initial suite intentionally retains `SpectrumCanInterpolateEveryAdjacentColorPair` as a failing regression. A fixed random seed samples a synthetic three-color spectrum; the last segment is never selected because `ColorGenerator_Spectrum.NewRandomizedColor` uses `colors.Count - 2` as the exclusive upper bound of `Rand.Range`. Product code is unchanged.
