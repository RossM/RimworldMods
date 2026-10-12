# Building the mods

Use the .NET 10 SDK (10.0.400 or later) and run this command from the repository root:

```powershell
dotnet build .\XylRimworldMods.sln
```

Builds use the released Disharmony NuGet package by default. A Disharmony source checkout is optional.

## Running your build

The mods are written to `ModBin/XylXenos` and `ModBin/XylIdeos`. Enable the official [Disharmony mod](https://steamcommunity.com/sharedfiles/filedetails/?id=3818084845) when running a build made with the NuGet package: these builds include Harmony, but do not bundle the Disharmony assemblies.

Shared mod data belongs in `Xylib.Data`. Building either mod includes this data alongside its own data project; building `Xylib.Data` directly copies its files to both mod outputs. Shared XML files use distinct filenames to avoid collisions with mod-specific data.

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

Build the solution as usual. It builds the local Disharmony library, analyzer, and RimWorld integration projects. These builds bundle `Disharmony.dll` and `Disharmony.RimWorld.dll` with each mod, so you can test them without enabling the separate Disharmony mod.

## Returning to the released package

Remove `Directory.Build.local.props` and rebuild. The build removes any previously bundled Disharmony DLLs from the mod output, so enable the official Disharmony mod before testing it.

For a single NuGet build without removing your settings file, use:

```powershell
dotnet build .\XylRimworldMods.sln -p:DisharmonySourceDirectory=
```
