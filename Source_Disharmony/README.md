# Disharmony

Disharmony is a C# game-modding framework built on Harmony. It lets you change existing game code at runtime by
writing **patches**: small methods that run before or after the code you want to change. Its main addition is
**inner patches**, which apply that same approach to individual operations inside a method, such as a method call,
field or property access, or constant.

For example, you could change the price returned by `GetPrice` everywhere it is used, or change only the prices
calculated by calls to `GetPrice` inside `Checkout.Total`. Disharmony lets you express both as prefixes and postfixes,
handling the underlying instruction changes for you.

## New to code patching?

A game method takes inputs, does some work, and may return a result. A patch lets your mod intervene without editing
the game's source files:

* A **prefix** runs before the selected code. It can read or change inputs, or skip the operation.
* A **postfix** runs afterward. It can inspect or change the result.
* An **inner patch** runs before or after a matching operation within a method, letting you change one part of its
  behavior while leaving the surrounding logic in place.

Harmony provides runtime patching for .NET code. For changes inside a method, Harmony modders often write a
**transpiler**: code that rewrites the method's compiled intermediate language (IL) instructions. Disharmony handles
many of those changes through selectors and ordinary C# patch methods, so you can start without learning IL.
You still need basic C# knowledge, a mod project that references the game's assemblies, and a way to identify the
method you want to change.

Start with [your first patch](#your-first-patch), then try [an inner patch](#patch-an-operation-inside-a-method).

## Coming from Harmony?

Disharmony is especially useful when a method-level prefix or postfix is too broad and a transpiler would be hard to
write or maintain. It keeps familiar parameter conventions such as `__instance`, `__result`, `__state`, and
`___fieldName`, and adds explicit binding attributes and selectors for inner operations.

It also helps target compiler-generated code: local functions, lambdas, nested types, captured variables, and iterator
methods. You can describe these targets without working directly with their generated names and state machines.

Adopt it one patch at a time. Existing patches can continue to use Harmony's registration API, while Disharmony
patches use `Patcher`. Disharmony recognizes `[HarmonyPatch]` for container discovery and a default declaring type,
and `[HarmonyPatchCategory]` for categories. That support does not make `Patcher.PatchAll` a replacement for Harmony's
`PatchAll`: attributed Disharmony patches use Disharmony's `[Prefix]` or `[Postfix]` and `[Target]` or `[Targets]`.

If you already know the prefix/postfix model, jump to [inner patches](#patch-an-operation-inside-a-method) or the
[fluent API](#write-a-patch-fluently).

## Your first patch

Add a reference to `Disharmony.dll` and the game's assemblies, ensure the host loads Disharmony and a compatible
`0Harmony.dll`, and import the `Disharmony` namespace. The current project targets .NET Framework 4.7.2 and references
Harmony 2.4.2; deployment and initialization depend on the game's mod loader.

Disharmony offers an attribute API for patches known at compile time and a fluent API for patches chosen at runtime.
Start with attributes. The examples below use fictional game types; substitute the types and members from your game.

Suppose a game provides `float PriceCalculator.GetPrice(Character buyer, int quantity)`. The following patch clamps its
`quantity` argument before the method runs, then discounts the result afterward:

```csharp
using System;
using Disharmony;

[Patch(typeof(PriceCalculator))]
public static class PriceCalculatorPatches
{
    [Prefix]
    [Target(nameof(PriceCalculator.GetPrice), typeof(Character), typeof(int))]
    public static void ClampQuantity(ref int quantity)
    {
        quantity = Math.Max(0, quantity);
    }

    [Postfix]
    [Target(nameof(PriceCalculator.GetPrice), typeof(Character), typeof(int))]
    public static void ApplyMemberDiscount(Character buyer, [ReturnValue] ref float result)
    {
        if (buyer.IsColonyMember)
            result *= 0.9f;
    }
}
```

Here `[Patch(typeof(PriceCalculator))]` declares a patch container and supplies its default outer-target type.
`[Target]` selects one method; the parameter types disambiguate its overload. `[Prefix]` and `[Postfix]` select when
each patch method runs. Parameters named `quantity` and `buyer` receive the matching game arguments; `[ReturnValue]`
receives the result. Using `ref` lets a patch change the value the game receives.

Attributes describe a patch but do not activate it. Call this once during mod initialization to apply every patch
method declared by the class:

```csharp
PatchHandle pricePatches = Patcher.PatchAll(typeof(PriceCalculatorPatches));
```

The patches are active before `PatchAll` returns. Keep the handle only if you need to remove exactly this group later:

```csharp
Patcher.Unpatch(pricePatches);
```

`[Target]` must resolve to exactly one member. Apply multiple `[Target]` attributes to name several members, or use
`[Targets]` when every match, such as every overload, should deliberately be patched.

## Patch an operation inside a method

An inner patch adds a second selector. `[Target]` still identifies the outer method Disharmony modifies, while
`[Inner]` identifies the operation within it. As an alternative to the first patch, this example discounts every
matching `GetPrice` call inside `Checkout.Total`:

```csharp
using Disharmony;

[Patch(typeof(Checkout))]
public static class CheckoutPatches
{
    [Postfix]
    [Target(nameof(Checkout.Total))]
    [Inner(typeof(PriceCalculator), nameof(PriceCalculator.GetPrice),
        typeof(Character), typeof(int))]
    public static void DiscountEachPrice([ReturnValue] ref float result)
    {
        result *= 0.9f;
    }
}
```

Activate this patch during initialization with `Patcher.PatchAll(typeof(CheckoutPatches))`. Calls to `GetPrice` from
other methods are unaffected by this inner patch. If you also apply the first example's outer postfix, both discounts
apply to calls inside `Total` when the buyer is a colony member.

The patch runs each time a matching operation executes, including on repeated loop iterations. An inner prefix
returning `false` skips only its matched operation, not the entire outer method. Use `MemberType.Getter` or `MemberType.Setter` in
`[Inner]` to select property or field access, and use `[Postfix, InnerConstant(value)]` to select and replace a
constant.

## How patches work

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

### Bind values to patch parameters

Disharmony supplies the arguments to a patch method. An ordinary parameter binds to a target parameter with the same
name: `quantity` in the example binds to `GetPrice`'s `quantity`. Reading by value observes the current value; passing
it by `ref` allows the patch to replace it.

Binding attributes cover values that cannot be identified by an ordinary parameter name:

* `[Parameter("name")]` or `[Parameter(index)]` binds an argument.
* `[Instance]` binds the target instance. The familiar `__instance` convention also works; in an inner patch,
  `__caller` names the outer instance.
* `[Field("name")]` binds one of the target instance's fields, even if it isn't public. The `___fieldName` convention
  works here too.
* `[ReturnValue]` binds the current result. The conventional name `__result` works without the attribute.
* `[State]` passes per-invocation data between patches applied in the same `Patch` or `PatchAll` call. The conventional
  name `__state` also works.
* `[BaseMethod]` binds a delegate for calling the base method of the method being patched.
* `[Method]` binds a delegate for calling a possibly non-public method on the target instance.
* `[Exception]` binds an exception in an `AlwaysRun` postfix.

Prefixes may return `bool`: returning `false` skips the selected operation, while its postfixes still run. A prefix can
set a skipped method's result through a `ref` return-value binding. Prefixes otherwise return `void`, and postfixes
always return `void`.

In an inner patch, an unqualified binding normally prefers the inner operation and then falls back to the outer target
where supported. Use `Scope.Inner` or `Scope.Outer` on a binding attribute when the intended source is ambiguous.

## Write a patch fluently

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
            new[] { typeof(Character), typeof(int) })!;
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

## Apply and manage patches

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

## Advanced features at a glance

* **Precise target selection:** target methods, constructors, property getters and setters, and overloads. Use
  `Ref<T>`, `In<T>`, and `Out<T>` in attribute signatures to distinguish by-reference parameter forms.
* **Compiler-generated code:** target nested types with dotted names, a local function with `OuterMethod.LocalFunction`,
  or lambdas in a method with `OuterMethod.*`. Disharmony also exposes captured variables and understands iterator
  state-machine methods.
* **Ordering and behavior:** use `[Priority]` or `.Priority(...)` to order interacting patches. `[PatchOptions]` or
  `.Options(...)` can inline the patch into its target, request `AlwaysRun` prefix or postfix semantics (including an
  exception-aware postfix), enable the experimental optimizer, or turn on `Debug` IL and JIT logging.
* **Diagnostics:** `Patcher.RuntimeExceptionHandler` reports errors while generating or applying patched IL, not
  exceptions thrown during normal execution of the target or patch. The `Debug` patch
  option writes modified IL and available Mono JIT assembly to Harmony's debug log.

Inner patches are easier to maintain than raw IL rewrites, but they still match compiled operations. Confirm that the
call, access, or constant exists in the compiled target, select overloads explicitly when possible, and test alongside
other mods that patch the same code.

## Where to go next

* [Attribute and parameter-binding reference](Attributes.cs): selector forms, binding scopes, and patch options.
* [Fluent API reference](Patch.cs) and [patch registration reference](Patcher.cs): configuration and lifecycle details.
* [Disharmony analyzers](../Source_Disharmony.Analyzers/README.md): optional build-time checks for patch definitions
  and bindings; target resolution still happens at runtime.
* [Tests](../Source_Disharmony.Tests/README.md): how to run the suite on the CLR and Mono.
