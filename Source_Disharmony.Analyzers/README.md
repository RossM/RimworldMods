# Disharmony analyzers

A build-time Roslyn analyzer for the patch-method rules in `PatchRegistry.Validate`.
The analyzer targets .NET Standard 2.0 and does not load Disharmony, Harmony, or RimWorld at runtime.

| ID | Warning |
| --- | --- |
| DH0001 | Patch method or an enclosing type has open generic parameters. |
| DH0002 | Patch method is not static. |
| DH0003 | Prefix does not return bool or void (by-ref bool is not bool). |
| DH0004 | Postfix does not return void. |
| DH0005 | Prefix with AlwaysRun does not return void. |

Methods are identified by Disharmony Prefix/Postfix attributes, including derived attributes.
Method-level PatchOptions replace class-level options; options follow reflection inheritance from base classes, but not enclosing classes,
matching PatchRegistry.GetAttributes. DH0005 takes precedence over DH0003 for an AlwaysRun prefix.
Warnings point to the method name and can be configured individually in .editorconfig.

This first version does not analyze programmatic PatchConfig registrations, execute custom attribute constructors,
validate targets or parameter bindings, enforce style, or require a class discovery marker.
Custom attributes derived from PatchOptionsAttribute have unknown options, so DH0005 is skipped for them.
Harmony-only patches are not subject to Disharmony's return-type rules.
Runtime validation remains necessary, including for methods registered through reflection or constructed generic types.

Reference this project from each project that contains patches (analyzer project references are not transitive):

```xml
<ProjectReference Include="..\Source_Disharmony.Analyzers\Source_Disharmony.Analyzers.csproj"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false" />
```

Run tests with `dotnet test Source_Disharmony.Analyzers.Tests/Source_Disharmony.Analyzers.Tests.csproj`.
Tests use minimal attribute metadata and do not execute patches or load the game.
