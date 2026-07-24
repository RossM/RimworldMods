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

## Migrating from Harmony

For an existing Harmony mod, switching does not mean that every patch must be rewritten. Straightforward prefixes and
postfixes remain conceptually similar. The greatest benefit comes from replacing fragile transpilers and awkward
patches of compiler-generated code with patches that directly describe the call or behavior you want to change.
