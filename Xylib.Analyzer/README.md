# Xylib analyzer

Xylib.Analyzer checks mod source while editing or building, without loading the game. It follows Disharmony.Analyzers: a `netstandard2.0` Roslyn component, compilation-scoped symbol checks, and a separate NUnit test project. It requires Roslyn 5.9 or newer (the .NET 10.0.400 SDK provides it).

Xylib, XylXenos, and XylIdeos reference it as a compiler analyzer. To use it in another project:

```xml
<ProjectReference Include="..\Xylib.Analyzer\Xylib.Analyzer.csproj"
                  OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
```

It adds no runtime assembly dependency. The existing `Xylib.Analyzer` runtime API remains available.

All diagnostics default to warnings. Use standard `.editorconfig` settings to configure individual IDs or the `Style` and `Correctness` categories.

| ID | Category | Check |
| --- | --- | --- |
| XYLIB0001 | Style | Disharmony `[Patch]` classes should be static. |
| XYLIB0002 | Style | Methods with `[Prefix]`, `[Postfix]`, `[Target]`, or `[Targets]` should have `[Feature]`. |
| XYLIB0003 | Style | `[Feature]` belongs on a patch method. |
| XYLIB0004 | Style | Prefix names should be `Prefix` or end with `_Prefix`. |
| XYLIB0005 | Style | Postfix names should be `Postfix` or end with `_Postfix`. |
| XYLIB0006 | Style | Concrete XML types should declare their own `[UsedFromXml]`. |
| XYLIB0007 | Correctness | `ConfigErrors` implementations should call the matching, non-abstract base method. |
| XYLIB0008 | Correctness | Mutable instance fields appear unsaved. |
| XYLIB0009 | Correctness | Save methods should call the base implementation when its type has fields to save. |

Patch conventions apply within Disharmony `[Patch]` classes. Harmony patch attributes are not checked. XML types include the RimWorld/Verse bases listed by the runtime checker and subclasses of abstract classes marked `[UsedFromXml]`, including bases from referenced assemblies. Abstract classes need no annotation; concrete subclasses must declare it directly.

Persistence checks recognize `IExposable.ExposeData`, `ThingComp.PostExposeData`, and `CompExposeData` on types whose ancestry includes a name ending in `Comp`. They inspect declared public and private instance fields, excluding constants, static fields, readonly fields, compiler-generated fields, and fields marked `[Unsaved]`.

The persistence checks are heuristics. A reference to an instance field in the save method counts as possible saving; `nameof` does not. They do not prove that a Scribe call saves a field, follow calls into helper methods, or verify that a base call executes on every path. Iterator bodies, expression bodies, and explicit interface implementations are supported. Generated code is excluded. Test the mod in game as well.

Run the tests with `dotnet test Xylib.Analyzer.Tests/Xylib.Analyzer.Tests.csproj`.
