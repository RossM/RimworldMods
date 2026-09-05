# Disharmony analyzers

A build-time Roslyn analyzer for patch discovery, patch-method validation, and parameter bindings that do not require target reflection.
The analyzer targets .NET Standard 2.0 and does not load Disharmony, Harmony, or RimWorld at runtime.

| ID | Warning |
| --- | --- |
| DH0001 | Patch method or an enclosing type has open generic parameters. |
| DH0002 | Patch method is not static. |
| DH0003 | Prefix does not return bool or void (by-ref bool is not bool). |
| DH0004 | Postfix does not return void. |
| DH0005 | Prefix with AlwaysRun does not return void. |
| DH0006 | Prefix/postfix has no [Patch] or [HarmonyPatch] on its containing class. |
| DH0007 | Prefix/postfix has no [Target] or [Targets] on the method or containing class. |
| DH0008 | Method has a direct Disharmony attribute but no [Prefix] or [Postfix]. |
| DH0009 | Multiple patch type attributes would make SingleOrDefault throw. |
| DH0010 | Multiple inner target attributes would make SingleOrDefault throw. |
| DH0011 | Selector has no explicit/default declaring type or qualified member name. |
| DH0012 | InnerConstant has a null value, which the registry does not support. |
| DH0014 | Multiple patch markers or categories, including mixed Disharmony/Harmony attributes. |
| DH0015 | Target/inner selector lacks a member name without selecting a constructor. |
| DH0016 | Parameter has multiple binding attributes. |
| DH0017 | Parameter uses __caller or explicit Scope.Inner without an inner patch. |
| DH0018 | AlwaysRun prefix binds the return value. |
| DH0019 | Exception binding is not in an AlwaysRun postfix. |
| DH0020 | Method/base-method binding is not a concrete delegate passed by value. |
| DH0021 | Parameter type/modifier is incompatible with an exception or known inner constant result. |
| DH0022 | Parameters sharing a state key in a patch class have incompatible types. |
| DH0024 | Parameter requests an instance, argument, or field from an inner constant. |

The analyzer assumes assembly discovery through Patcher.PatchAll or Patcher.PatchCategory.
Methods are identified by the built-in Disharmony Prefix/Postfix attributes. User-defined attribute subclasses are ignored.
Discovery markers and targets follow reflection inheritance and can be on another part of a partial class.
DH0008 checks only attributes directly on the method, from the built-in set;
it does not flag helpers for inherited method attributes, class defaults, return attributes, or parameter bindings.
Method-level PatchOptions replace class-level options; options follow reflection inheritance from base classes, but not enclosing classes,
matching PatchRegistry.GetAttributes. DH0005 takes precedence over DH0003 for an AlwaysRun prefix.
Warnings point to the method, parameter, selector attribute, or discovery class and can be configured individually in .editorconfig.
Multiplicity checks follow the built-in attributes' inheritance and multiplicity rules, including suppression of overridden attributes.
Discovery checks treat [Patch]/[HarmonyPatch] as one group and [Category]/[HarmonyPatchCategory] as another.
Duplicate discovery warnings include effective inherited attributes, even when their values agree.
Duplicate categories also warn on classes without a discovery marker. Runtime precedence remains unchanged.
Class targets are added to method targets rather than replaced, so every effective selector is checked.

Qualified selectors such as Namespace.Type:Member and Namespace.Type.Member can resolve their declaring type at runtime.
DH0011 accepts both forms without attempting that lookup. Inner selectors do not inherit the outer target's type.

This analyzer does not analyze programmatic PatchConfig registrations,
resolve target methods, validate target-dependent parameter bindings/signature filters, or enforce style.
Lookup-dependent failures (missing/ambiguous members, field-versus-method selection, target method restrictions,
and nested-member resolution) and patch application failures remain runtime checks.
Explicit built-in parameter binding attributes override reserved names.
Known-type checks follow the runtime's ref/in/out rules and AllowUnsafe reference-type bypass; delegate shape and state-type checks do not bypass validation.
State keys are compared within each declaring patch class across its declared patch methods, including methods with different targets.
State parameters need not have a writer, and ref/in/out modifiers do not change the stored state type.
Ordinary argument index bounds, argument/result/instance/field type compatibility, delegate signatures, iterator-state-machine restrictions,
and writable-reference restrictions that depend on target parameters remain runtime checks.
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
