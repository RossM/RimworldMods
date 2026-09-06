# Disharmony

Disharmony is a C# game-modding framework built on Harmony. It lets mod authors change game behavior at runtime by
writing small methods called patches. A patch can run before or after a game method, inspect its inputs, change its
result, or replace its behavior.

Disharmony extends this model to operations **inside** a method. You can patch a particular method call, field or
property access, or constant while keeping the surrounding game logic intact. You describe the operation you want
to change, and Disharmony handles the underlying IL (intermediate language) instructions.

## Why patch inside a method?

Suppose a game calculates prices in a method called `GetPrice`. A patch that changes its return value changes prices
wherever that method is called. That is useful for a general pricing adjustment, but a discount that belongs only at
checkout needs a narrower scope: the calls to `GetPrice` inside `Checkout.Total`.

With Harmony, a patch that runs before a method is called a **prefix**, and one that runs afterward is a **postfix**.
Changes within the method often require a **transpiler**, which searches and rewrites its compiled instructions.
Disharmony lets you use prefixes and postfixes at that finer level too. These **inner patches** can express many
changes that would otherwise require a hand-written transpiler.

For new mod authors, this means you can write such patches in ordinary C# without first learning to manipulate IL.
For experienced Harmony modders, it means less instruction-matching code to maintain and a clearer statement of
what a patch is intended to change. Disharmony also provides selectors for compiler-generated code, including local
functions, lambdas, and iterator methods, which can otherwise be awkward to locate and patch.

Disharmony can be adopted alongside existing Harmony patches. The examples below introduce its attribute API;
[the fluent API](#configure-patches-in-code) provides the same model for targets selected at runtime.

## Write your first patch

To use Disharmony, your mod project needs references to `Disharmony.dll` and the relevant game assemblies. The game
must also load Disharmony and a compatible `0Harmony.dll`. The current Disharmony project targets .NET Framework
4.7.2 and references Harmony 2.4.2; the details of loading these assemblies and initializing your mod depend on the
game's mod loader.

The following examples use fictional game types to illustrate the API. Suppose `PriceCalculator` has a method
`float GetPrice(Character buyer, int quantity)`, and you want colony members to receive a 10% discount:

```csharp
using Disharmony;

[Patch(typeof(PriceCalculator))]
public static class PriceCalculatorPatches
{
    [Postfix]
    [Target(nameof(PriceCalculator.GetPrice), typeof(Character), typeof(int))]
    public static void ApplyMemberDiscount(Character buyer, [ReturnValue] ref float result)
    {
        if (buyer.IsColonyMember)
            result *= 0.9f;
    }
}
```

`[Patch(typeof(PriceCalculator))]` groups the patches in this class and supplies the type whose members they target.
`[Target]` selects `GetPrice`; the parameter types identify the intended overload. `[Postfix]` tells Disharmony to
run `ApplyMemberDiscount` after that method returns.

Disharmony supplies the patch's arguments. The `buyer` parameter receives the game method's argument with the same
name, and `[ReturnValue]` binds `result` to its return value. Passing `result` by `ref` lets the patch change the price
returned to the caller. Patch methods must be static, and only need to declare the values they use.

To activate the patch, call this once from your mod's initialization code:

```csharp
PatchHandle pricePatches = Patcher.PatchAll(typeof(PriceCalculatorPatches));
```

The patch is then active for calls to `GetPrice` throughout the game. The returned handle identifies this group of
patches and can be kept if you need to remove it later.

## Patch an operation inside a method

Now suppose the same member discount should apply only at checkout. Instead of patching `GetPrice` itself, you can
patch its calls within `Checkout.Total`:

```csharp
using Disharmony;

[Patch(typeof(Checkout))]
public static class CheckoutPatches
{
    [Postfix]
    [Target(nameof(Checkout.Total))]
    [Inner(typeof(PriceCalculator), nameof(PriceCalculator.GetPrice),
        typeof(Character), typeof(int))]
    public static void ApplyMemberDiscount(Character buyer, [ReturnValue] ref float result)
    {
        if (buyer.IsColonyMember)
            result *= 0.9f;
    }
}
```

The patch body is the same, but the selectors give it a different scope. `[Target]` identifies `Checkout.Total` as
the **outer target**: the method whose code Disharmony modifies. `[Inner]` selects the `GetPrice` calls within it.
The postfix receives each selected call's buyer and result, and adjusts the price before `Total` uses it.

For this version, register `CheckoutPatches` in place of `PriceCalculatorPatches`:

```csharp
PatchHandle pricePatches = Patcher.PatchAll(typeof(CheckoutPatches));
```

Calls to `GetPrice` elsewhere keep their original behavior. Within `Total`, the patch runs each time a matching call
executes, including repeated calls in a loop. This is the central distinction between outer and inner patches: an
outer patch surrounds the whole method, while an inner patch surrounds a selected operation within it.

Inner selectors also support field and property access through `MemberType.Getter` and `MemberType.Setter`.
For constants, a postfix can use `[InnerConstant(value)]` and a return-value binding to replace the selected value.
All of these selectors match compiled operations, so the call, access, or constant must exist in the compiled target.

## Control inputs and execution

Both examples use postfixes to change a result. Prefixes use the same targeting and binding rules, but run before
the selected operation. For example, this method could be added to `PriceCalculatorPatches` to prevent a negative
quantity from reaching `GetPrice`:

```csharp
[Prefix]
[Target(nameof(PriceCalculator.GetPrice), typeof(Character), typeof(int))]
public static void ClampQuantity(ref int quantity)
{
    if (quantity < 0)
        quantity = 0;
}
```

A prefix can also return `bool`. Returning `false` skips the selected operation, while its postfixes still run. A
prefix that supplies a replacement result can set it through `[ReturnValue] ref ...` before returning `false`.
For an inner prefix, skipping affects only the matched operation; the surrounding method continues.
Prefixes otherwise return `void`, and postfixes always return `void`.

### Access other values

Alongside arguments and results, patches can access instances, fields, and shared state. Disharmony supports both
explicit binding attributes and several familiar Harmony parameter names:

| Value | Attribute | Parameter-name convention |
| --- | --- | --- |
| Argument | `[Parameter("name")]` or `[Parameter(index)]` | The target parameter's name |
| Target instance | `[Instance]` | `__instance` |
| Instance field, including a non-public field | `[Field("name")]` | `___fieldName` |
| Return value | `[ReturnValue]` | `__result` |
| Per-invocation shared state | `[State]` | `__state` |

Passing a bound value by value lets the patch read it; passing it by `ref` lets the patch replace it where supported.
State bindings share data between patches registered together in the same `Patch` or `PatchAll` call, during each
outer invocation.

In an inner patch, bindings generally refer to the inner operation. Name-based argument and field bindings fall back
to the outer target when there is no inner match. Use `Scope.Inner` or `Scope.Outer` on a binding attribute to make
the source explicit; `__caller` also provides access to the outer instance.

For more specialized patches, `[Method]` binds a delegate to a possibly non-public method on the target instance,
and `[BaseMethod]` binds a delegate for calling the patched method's base method. The
[attribute reference](Attributes.cs) describes these bindings and their constraints.

## Configure patches in code

When reflection or runtime conditions determine which method to patch, the fluent API lets you build the patch
configuration in code. This example selects `GetPrice` through reflection and applies a postfix that caps its result:

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

`Patch.Postfix.With(...).Of(...)` builds a `PatchConfig`, which `Patcher.Patch` applies. Adding `.Inner(innerMethod)`
selects calls inside the outer target; `.InnerGet(...)`, `.InnerSet(...)`, and `.InnerConstant(...)` select the other
kinds of inner operation.

In this form, the configuration supplies the patch definition, so attributes such as `[Postfix]`, `[Target]`, and
`[Inner]` are ignored. Parameter-binding attributes such as `[ReturnValue]` still apply to the patch method.

## Select and manage patches

The examples register one class or configuration at a time. For larger patch sets, `Patcher` also supports discovery
across an assembly and registration by category:

| Registration | Applies |
| --- | --- |
| `Patcher.Patch(config)` | A fluent configuration |
| `Patcher.Patch(methodInfos)` | Selected attributed patch methods |
| `Patcher.PatchAll(type)` | All attributed patch methods declared by one type |
| `Patcher.PatchAll(assembly)` | Patches in containers marked with `[Patch]` or `[HarmonyPatch]` |
| `Patcher.PatchCategory(assembly, category)` | Marked containers in the selected category |

Direct registration by type or method does not require a `[Patch]` container marker. For assembly discovery,
`[Category("name")]` can group containers for selective registration.

Within a patch definition, `[Target]` must resolve to exactly one member. Parameter types distinguish overloads, and
`Ref<T>`, `In<T>`, and `Out<T>` distinguish by-reference parameter forms in attribute signatures. Multiple `[Target]`
attributes select several members; `[Targets]` selects every match, such as every overload of a method.

Each registration call returns a `PatchHandle`. Several configurations can be registered together to share a handle
and per-invocation state. To remove the patches associated with a handle, call:

```csharp
Patcher.Unpatch(pricePatches);
```

Patches registered by other calls remain active. All patches affect the current process, so their effects are visible
to other mods and game code that use the patched methods.

Patches take effect before registration returns, although Disharmony may defer generating the modified method bodies
until their first call. `Patcher.ForceApply()` completes that preparation during initialization or another suitable
period.

### Use alongside Harmony

Existing Harmony patches can continue to use Harmony's registration API while Disharmony patches use `Patcher`.
Disharmony recognizes `[HarmonyPatch]` for container discovery and a default declaring type, and
`[HarmonyPatchCategory]` for categories. Patch definitions still use Disharmony's `[Prefix]` or `[Postfix]` and
`[Target]` or `[Targets]`; the discovery support does not import Harmony patch definitions wholesale.

This allows gradual adoption where inner patches or generated-code selectors are useful. As with any runtime patch,
test the result alongside other mods that modify the same code.

## Explore further

Beyond the examples above, Disharmony supports:

* **Compiler-generated targets.** Select nested types with dotted names, a local function with
  `OuterMethod.LocalFunction`, or lambdas with `OuterMethod.*`. Disharmony also exposes captured variables and
  understands iterator state-machine methods.
* **Patch ordering and execution options.** `[Priority]` or `.Priority(...)` orders interacting patches.
  `[PatchOptions]` or `.Options(...)` controls inlining and `AlwaysRun` behavior, including postfixes that can inspect
  or change an exception through `[Exception]`. An experimental optimization pass is also available.
* **Diagnostics.** The `Debug` option writes modified IL and available Mono JIT assembly to Harmony's debug log.
  `Patcher.RuntimeExceptionHandler` reports errors during IL generation or patch application; exceptions from normal
  execution of the game or patch methods are outside its scope.

The source includes API documentation in the [attribute reference](Attributes.cs), [fluent API](Patch.cs), and
[patch registration API](Patcher.cs). The optional [Disharmony analyzers](../Source_Disharmony.Analyzers/README.md)
check patch definitions and some bindings at build time, while target resolution remains a runtime check. For working
examples of more specialized behavior and instructions for running the suite, see the
[test project](../Source_Disharmony.Tests/README.md).
