# Building the mods

Use the .NET 10 SDK (10.0.400 or later) and run this command from the repository root:

```powershell
dotnet build .\XylRimworldMods.sln
```

Builds use the released Disharmony NuGet package by default. That package omits `Disharmony.RimWorld.dll`, whose RimWorld entry point starts patching, so NuGet builds require the separately installed Disharmony mod. A local Disharmony source checkout lets builds bundle the integration DLL instead.

## Running your build

The mods are written to `ModBin/AutoColorApparel`, `ModBin/XylXenos`, `ModBin/XylIdeos` and `ModBin/Yobai`. Enable the official [Disharmony mod](https://steamcommunity.com/sharedfiles/filedetails/?id=3818084845) when running NuGet builds. XylXenos and XylIdeos always bundle Xylib. Yobai, XylXenos and XylIdeos bundle Harmony, Disharmony and its RimWorld integration only when built with a local Disharmony checkout; AutoColorApparel always requires the separate Disharmony mod.

The solution also builds `ModBin/Xylib.Tests`, `ModBin/XylXenos.Tests`, `ModBin/Yobai.Tests`, and their pinned `ModBin/DevTools` dependency. These test mods can be built directly in Visual Studio. The [in-game test runner](InGameTests/README.md) creates persistent links to them in RimWorld's `Mods` folder; use `-SkipBuild` to run the build you made in Visual Studio.

Yobai adds polycule bedrooms and night visits. See the [Yobai README](Yobai/README.md) for how it works and the [test instructions](Yobai.Tests/README.md) for its optional DevTools tests.

Shared mod data belongs in `Xylib.Data`. Building XylXenos or XylIdeos includes this data alongside its own data project; building `Xylib.Data` directly copies its files to both mod outputs. Shared XML files use distinct filenames to avoid collisions with mod-specific data. Yobai's data is independent.

`ModBuild.targets` handles copying for both source and data projects. Each project declares its inputs and imports that file:

- `ModDestination` lists the mod output directories. Data projects set `OwnsOutput="true"` so builds remove stale data while preserving `Assemblies`, and clean removes the mod output. Shared data destinations leave it false.
- Code projects automatically copy their own `TargetPath` assembly to each destination's `Assemblies` directory. `ModAssembly` lists only additional DLL filenames to copy from the project's build output. DLLs outside this combined set are removed from that directory.
- Data projects copy their SDK `None` items to the mod root, plus `SharedModDataDirectory` when specified. AutoColorApparel uses `ModDataDirectory` to copy its `Data` contents to the root instead.

Dependencies, analyzers and framework settings stay in the individual project files. Xylib itself does not deploy an assembly; XylXenos and XylIdeos copy their built Xylib dependency.

## Developing with local Disharmony source

To try changes in your own [Disharmony checkout](https://github.com/RossM/Disharmony), create `Directory.Build.local.props` in this repository's root:

```xml
<Project>
  <PropertyGroup>
    <DisharmonySourceDirectory>..\Disharmony</DisharmonySourceDirectory>
  </PropertyGroup>
</Project>
```

Change the path to your checkout. Relative paths start at this repository's root; absolute paths also work. The file is ignored by Git and applies to Visual Studio and command-line builds.

Build the solution as usual. It builds the local Disharmony library, analyzer, and RimWorld integration projects. Yobai, XylXenos and XylIdeos bundle `0Harmony.dll`, `Disharmony.dll` and `Disharmony.RimWorld.dll`, so you can test them without enabling the separate Disharmony mod. AutoColorApparel still requires the separate mod.

## Returning to the released package

Remove `Directory.Build.local.props` and rebuild, then enable the official Disharmony mod before testing any of the mods. Yobai, XylXenos and XylIdeos remove previously bundled `0Harmony.dll`, `Disharmony.dll` and `Disharmony.RimWorld.dll`; the installed Disharmony mod supplies all three.

For a single NuGet build without removing your settings file, use:

```powershell
dotnet build .\XylRimworldMods.sln -p:DisharmonySourceDirectory=
```
