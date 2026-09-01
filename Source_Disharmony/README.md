# Disharmony

## Description

Disharmony is a game-modding patch framework built on top of Harmony. It keeps the familiar prefix-and-postfix style,
while making patches that would normally require a transpiler much easier to write and maintain.

## Features

* **Familiar Harmony-style patches.** Prefixes and postfixes can inspect or change arguments and return values, share
  state, access instances and fields, skip the original method, and target methods, constructors, and properties.

* **Patch individual calls inside a method.** Inner prefixes and postfixes run around a particular method call, field
  or property access, or constant value within the target method. This covers many common transpiler use cases without
  requiring you to search and rewrite raw IL instructions.

* **Reach compiler-generated code more easily.** Disharmony provides ways to target local functions, lambdas, nested
  classes, captured variables, and iterator methods. These are possible to patch with Harmony, but often require
  knowledge of compiler-generated names and state-machine internals.

* **Clearer parameter binding.** Optional attributes explicitly identify arguments, instances, fields, return values,
  shared state, and base methods. Disharmony also provides convenient ways to distinguish ordinary, `ref`, `in`, and
  `out` parameters when selecting overloaded methods.

* **Patch one or many targets.** A patch can target a specific overload, several explicitly named members, or every
  matching overload. Patches can be registered by method, type, assembly, or Harmony patch category.

* **Just-in-time patch application.** Disharmony can defer the more expensive construction of a patch until the
  affected method is first called, avoiding wasteful redundant work when several mods patch the same method. Patches
  can also be applied immediately when required.

* **Harmony interoperability.** Disharmony uses Harmony underneath and understands selected Harmony conventions,
  including patch classes and categories. It is intended as an extension of the Harmony ecosystem rather than an
  entirely unrelated replacement.

## Getting started

Add a reference to `Disharmony.dll`, make sure the host provides a compatible `0Harmony`, and import the `Disharmony`
namespace. Disharmony has an attribute-focused API for patches known at compile time and a fluent API for patches
chosen at runtime. Most patches are simplest to write with attributes.

### The patch model

A patch is a static method that Disharmony inserts around existing code. Each patch definition answers three
questions:

1. What is the **outer target**? This is the method or constructor Disharmony will modify.
2. Does the patch surround that outer target, or an **inner operation** such as a call or field access inside it?
3. Does the patch run before (**prefix**) or after (**postfix**) the selected operation?

Those choices produce four common forms:

| Form | Runs | Typical uses |
| --- | --- | --- |
| Outer prefix | Before the selected method or constructor | Inspect or replace arguments, initialize state, or return `false` to skip the target |
| Outer postfix | After the selected method or constructor | Observe side effects or inspect and replace its return value |
| Inner prefix | Before each matching call or member access inside the selected outer method | Change inputs to, or return `false` to skip, only that operation |
| Inner postfix | After each matching call, member access, or constant inside the selected outer method | Inspect or replace that operation's result without changing the rest of the method |

An ordinary patch is outer. It becomes inner only when its definition includes an inner selector.

### Write an attribute-focused patch

Suppose the game provides `float PriceCalculator.GetPrice(Pawn buyer, int quantity)`. The following patch clamps its
`quantity` argument before the method runs, then discounts the result afterward:

```csharp
using System;
using Disharmony;

[Patch(typeof(PriceCalculator))]
public static class PriceCalculatorPatches
{
    [Prefix]
    [Target(nameof(PriceCalculator.GetPrice), typeof(Pawn), typeof(int))]
    public static void ClampQuantity(ref int quantity)
    {
        quantity = Math.Max(0, quantity);
    }

    [Postfix]
    [Target(nameof(PriceCalculator.GetPrice), typeof(Pawn), typeof(int))]
    public static void ApplyMemberDiscount(Pawn buyer, [ReturnValue] ref float result)
    {
        if (buyer.IsColonyMember)
            result *= 0.9f;
    }
}
```

Here `[Patch(typeof(PriceCalculator))]` declares a patch container and supplies its default outer-target type.
`[Target]` selects one method; the parameter types disambiguate its overload. `[Prefix]` and `[Postfix]` select when
each patch method runs.

Apply every patch method declared by the class during mod initialization:

```csharp
PatchHandle pricePatches = Patcher.PatchAll(typeof(PriceCalculatorPatches));
```

The patches are active before `PatchAll` returns. Keep the handle only if you need to remove exactly this group later:

```csharp
Patcher.Unpatch(pricePatches);
```

`[Target]` must resolve to exactly one member. Apply multiple `[Target]` attributes to name several members, or use
`[Targets]` when every match, such as every overload, should deliberately be patched.

### Bind values to patch parameters

Disharmony supplies the arguments to a patch method. An ordinary parameter binds to a target parameter with the same
name: `quantity` in the example binds to `GetPrice`'s `quantity`. Reading by value observes the current value; passing
it by `ref` allows the patch to replace it.

Binding attributes cover values that cannot be identified by an ordinary parameter name:

* `[Parameter("name")]` or `[Parameter(index)]` binds an argument.
* `[Instance]` binds the target instance, and `[Field("name")]` binds one of its fields. The familiar `__instance`
  and `___fieldName` conventions also work; in an inner patch, `__caller` names the outer instance.
* `[ReturnValue]` binds the current result. The conventional name `__result` works without the attribute.
* `[State]` passes per-invocation data between patches applied in the same `Patch` or `PatchAll` call. The conventional
  name `__state` also works.
* `[BaseMethod]` and `[Method]` bind delegates for calling otherwise awkward base or inaccessible methods.
* `[Exception]` binds an exception in an `AlwaysRun` postfix.

Prefixes may return `bool`: returning `false` skips the selected operation, while its postfixes still run. A prefix can
set a skipped method's result through a `ref` return-value binding. Prefixes otherwise return `void`, and postfixes
always return `void`.

In an inner patch, an unqualified binding normally prefers the inner operation and then falls back to the outer target
where supported. Use `Scope.Inner` or `Scope.Outer` on a binding attribute when the intended source is ambiguous.

### Patch an operation inside a method

An inner patch adds a second selector. `[Target]` still identifies the outer method Disharmony modifies, while
`[Inner]` identifies the operation within it. This example discounts every matching `GetPrice` call inside
`Checkout.Total`, rather than changing the final result returned by `Total`:

```csharp
[Patch(typeof(Checkout))]
public static class CheckoutPatches
{
    [Postfix]
    [Target(nameof(Checkout.Total))]
    [Inner(typeof(PriceCalculator), nameof(PriceCalculator.GetPrice),
        typeof(Pawn), typeof(int))]
    public static void DiscountEachPrice([ReturnValue] ref float result)
    {
        result *= 0.9f;
    }
}
```

If the selected call occurs more than once, the patch runs once per occurrence. An inner prefix returning `false`
skips only its matched operation, not the entire outer method. Use `MemberType.Getter` or `MemberType.Setter` in
`[Inner]` to select property or field access, and use `[Postfix, InnerConstant(value)]` to select and replace a
constant.

### Write a patch fluently

Use the fluent API when reflection or runtime conditions determine the patch. Build a `PatchConfig` by selecting a
prefix or postfix, patch method, and outer target, then pass it to `Patcher.Patch`:

```csharp
using System;
using System.Reflection;
using Disharmony;

public static class RuntimePatches
{
    public static void CapPrice([ReturnValue] ref float result)
    {
        result = Math.Min(result, 100f);
    }

    public static PatchHandle Apply()
    {
        MethodInfo target = typeof(PriceCalculator).GetMethod(
            nameof(PriceCalculator.GetPrice),
            new[] { typeof(Pawn), typeof(int) })!;
        MethodInfo patchMethod = typeof(RuntimePatches).GetMethod(nameof(CapPrice))!;

        return Patcher.Patch(
            Patch.Postfix
                .With(patchMethod)
                .Of(target));
    }
}
```

Add `.Inner(innerMethod)` to make the configured patch inner. `.InnerGet(...)`, `.InnerSet(...)`, and
`.InnerConstant(...)` select other inner operations. Definition attributes such as `[Prefix]`, `[Target]`, and
`[Inner]` are ignored when using a `PatchConfig`, but parameter-binding attributes such as `[ReturnValue]` still apply.

Several configurations or targets can be supplied to one `Patcher.Patch` call. They share one `PatchHandle`, are
removed together, and can share `[State]` during each outer invocation.

### Apply and manage patches

Choose the narrowest registration method appropriate for the patch set:

* `Patcher.Patch(config)` applies fluent configurations.
* `Patcher.Patch(methodInfos)` applies specifically selected attributed patch methods.
* `Patcher.PatchAll(type)` applies every attributed patch method declared by one type. The type does not need a
  `[Patch]` marker when registered directly.
* `Patcher.PatchAll(assembly)` discovers patch containers marked with `[Patch]` or a recognized Harmony patch marker.
* `Patcher.PatchCategory(assembly, category)` applies only containers in a `[Category("name")]` or recognized Harmony
  category.

Every registration call returns a handle that removes only the patches in that call. Patches affect every caller in
the current process; they are not scoped to the mod instance that registered them.

Disharmony may defer expensive preparation until a patched method is first called. The patch is already active, but
`Patcher.ForceApply()` can move that preparation to a predictable initialization or idle period.

### Advanced features at a glance

* **Precise target selection:** target methods, constructors, property getters and setters, and overloads. Use
  `Ref<T>`, `In<T>`, and `Out<T>` in attribute signatures to distinguish by-reference parameter forms.
* **Compiler-generated code:** target nested types with dotted names, a local function with `OuterMethod.LocalFunction`,
  or lambdas in a method with `OuterMethod.*`. Disharmony also exposes captured variables and understands iterator
  state-machine methods.
* **Ordering and behavior:** use `[Priority]` or `.Priority(...)` to order interacting patches. `[PatchOptions]` or
  `.Options(...)` can inline the patch into its target, request `AlwaysRun` prefix or postfix semantics (including an
  exception-aware postfix), enable the experimental optimizer, or turn on `Debug` IL and JIT logging.
* **Diagnostics:** `Patcher.RuntimeExceptionHandler` receives recoverable runtime patching errors. The `Debug` patch
  option writes modified IL and available Mono JIT assembly to Harmony's debug log.
* **Harmony compatibility:** Disharmony recognizes selected Harmony patch markers and categories, so an existing mod
  can migrate incrementally.

Inner patches are easier to maintain than raw IL rewrites, but they still match compiled operations. Confirm that the
call, access, or constant exists in the compiled target, select overloads explicitly when possible, and test alongside
other mods that patch the same code.

## Migrating from Harmony

For an existing Harmony mod, switching does not mean that every patch must be rewritten. Straightforward prefixes and
postfixes remain conceptually similar. The greatest benefit comes from replacing fragile transpilers and awkward
patches of compiler-generated code with patches that directly describe the call or behavior you want to change.
