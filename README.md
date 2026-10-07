Various mods for RimWorld. Currently working on new xenotypes and the necessary new genes.

Current status:
* Bossaps: Engineered, sentient livestock gone feral. They can live off raw plants, enjoy being slaves, and tend to go berserk if they take damage. Minor revision complete, playtesting.
* Chyrr: A bat-based race that can fly, stun enemies, and "see" with echolocation, but goes into torpor in cold weather. Playtesting.
* Dvergr: Space dwarves. Good at what you'd expect, bad at what you'd expect. They need alcohol to get through the working day (and stay alive). Playtesting.
* Nixie: A psychic aquatic race that prefers cold, wet environments. Playtesting.
* Splicer: Mad cultists with random genes. Design in progress.
* Titan: Extremely tough giants with unique carbon-silicate biochemistry. Major redesign underway, implementing.
* Trog: Repulsive hybrids that get along with insects and produce clouds of tox gas. Playtesting.
* Warcat: Failed human-cat hybrid supersoldiers that are deadly in melee but need to eat raw meat. Playtesting.
* Vereid: A plant-based race with abilities based on absorbing sunlight. Design in progress.
* Voidborn: An anomaly-inspired race. Design in progress.
* Zeegee: Descendants of the inhabitants of ancient generation ships, adapted for life in orbit. Design in progress.

## Building with Disharmony

Use the .NET 10 SDK and clone [Disharmony](https://github.com/RossM/Disharmony) alongside this repository:

```text
Repos/
  RimworldMods/
    XylRimworldMods.sln
  Disharmony/
    Disharmony.sln
```

XylIdeos, XylXenos, and Xylib reference the sibling Disharmony library and analyzer projects through relative paths. The mod solution also loads the Disharmony projects from that repository.

```powershell
dotnet build .\XylRimworldMods.sln
```

Each mod build copies the resolved `Disharmony.dll` from its build output into its `ModBin/<mod>/Assemblies` directory. Disharmony itself does not deploy mod files.

For now, builds require the sibling source checkout. Once Disharmony is released, the project references can be replaced with references to the released library and analyzers; the mod deployment step already uses the resolved build output rather than a Disharmony source-build path.
