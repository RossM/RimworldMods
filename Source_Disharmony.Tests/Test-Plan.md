# Disharmony test coverage plan

This file records deferred test-coverage proposals. They are ideas to evaluate, not agreed requirements or descriptions
of currently supported behavior. Before implementing a section, re-read the implementation and existing tests, confirm
the intended public contract, and refine the cases below rather than mechanically treating this as a checklist.

## Cross-runtime execution

The same end-to-end suite should run regularly on both the Microsoft CLR and Mono. The local runner is
`Run-DisharmonyTests.ps1`; it supports both runtimes, but merely having the script does not protect against regressions
unless both modes are part of the normal validation process or CI.

The Mono run has already demonstrated its value. A throw-only method with a non-void signature, patched by an
`AlwaysRun` postfix that suppressed the exception and wrote `__result`, produced an unreachable `stloc` with an empty
stack. The Microsoft CLR accepted the unreachable fragment while Mono rejected the generated method as invalid IL.
That specific defect is fixed, but it is representative of the differences this coverage should catch.

Suggested coverage:

- Run all non-ignored end-to-end tests under both runtimes, using the same Release-built `TestTargets` assembly.
- Preserve identical assertions and ignore lists across runtimes. A runtime-specific failure should be investigated as
  a product bug before considering a runtime-specific expectation.
- Give particular attention to generated IL containing exception regions, unreachable blocks, by-ref values, value-type
  instances, constructors, state machines, inlining, and control flow that carries stack values.
- Run each runtime in a fresh process so cached trampolines, resolved methods, and static state cannot hide first-use
  failures.

Completion criterion: both runtime commands are repeatable and are run automatically or are an explicit required local
check, with result files retained separately.

## Harmony coexistence

Disharmony relies on Harmony/MonoMod, but most tests exercise Disharmony in isolation. Add a small integration fixture
that deliberately combines public Harmony patches with public Disharmony patches. Use dedicated target and patch types,
and select the patch methods explicitly rather than scanning the main test assembly.

The initial suite is implemented in `EndToEnd/Interop/HarmonyCoexistenceTests.cs`. It uses Harmony's explicit
`Harmony.Patch` API with valid patches and a fixture-specific Harmony ID. It covers:

- Apply a Harmony prefix/postfix pair first, then a Disharmony pair to the same method; verify execution order and result.
- Apply the same patches in the opposite registration order.
- Include a Harmony transpiler that makes a simple observable change, then apply a Disharmony outer patch and inner
  patch independently.
- Selectively unpatch the Disharmony `PatchHandle` and verify the Harmony patch remains active; unpatch Harmony and
  verify Disharmony remains active.
- Repeat an apply, first-call trampoline resolution, unpatch, and reapply cycle while the other library's patch remains
  installed.

Possible follow-up coverage:

- Verify an application failure or rollback on one side does not remove or corrupt the other side's registrations.

Avoid deliberately invalid Harmony patches and throwing Harmony transpilers in this fixture. Harmony may emit invalid
IL or retain a broken patch state instead of rolling back, so those cases cannot safely provide isolated coexistence
tests.

The purpose is not to duplicate Harmony's own tests. It is to protect ownership, ordering, wrapper regeneration, and
cache invalidation at the boundary between the two systems.

## Concurrent patch lifecycle and first-use resolution

The registry, trampoline cache, Harmony wrapper update, runtime exception reporting, and `PatchHandle` lifecycle share
global state. Decide which concurrent operations are supported, then test that contract. Unsupported combinations should
fail deterministically and early rather than corrupting state or generating invalid IL.

Candidate scenarios:

- Several threads make the first call to one newly patched method simultaneously.
- Different threads resolve different newly patched methods simultaneously.
- Multiple patches for one target are submitted concurrently, where supported, and the final nesting order remains
  deterministic.
- Patch and selectively unpatch different targets concurrently.
- A failure during deferred trampoline resolution races with another caller; the runtime exception is reported once as
  intended, every caller receives contract-compliant behavior, and the original method is restored.
- A `RuntimeExceptionHandler` invokes unrelated patched code, to exercise safe reentrancy without making the test depend
  on recursive patch application unless that is explicitly supported.

Use barriers rather than timing sleeps, repeat race-sensitive cases enough to be meaningful, and apply timeouts so a
deadlock produces a useful test failure.

## Public lifecycle stress and atomicity

Existing lifecycle tests cover individual rollback and unpatch paths. Add combination tests that exercise those paths
after caches and trampolines have reached different states.

Useful sequences include:

- Patch several targets in one call, resolve none/some/all of them, then selectively unpatch the returned handle.
- Apply overlapping handles to the same and different targets, remove them in both orders, and verify only the selected
  registrations disappear.
- Fail a multi-patch call at the first, middle, and final patch after earlier patches have reached both pending and
  resolved states; verify the whole call is atomic.
- Repeat successful and failed apply-unpatch-apply cycles to catch stale registry, matcher, trampoline, and thunk cache
  entries.
- Exercise `ForceApply` before and after partial target resolution, including a failure affecting one of several pending
  targets.

Assertions should cover externally observable method behavior and exception reporting, not private collection counts.

## Compiler-generated method families

Iterator and local-function coverage is extensive, but other compiler-generated forms can take distinct reflection and
state-machine paths. The most important candidate is async code. Before writing broad matrices, establish whether each
form is intended to be supported; if it is not, add patch-time validation tests for the documented rejection.

Forms worth evaluating:

- Async instance and static methods, both before and after the first invocation.
- Calls inside an async state machine before and after an `await` suspension point.
- Async lambdas and local functions, including captured parameters and instance state.
- Async iterators, which combine iterator and async state-machine transformations.
- Generic local functions and lambdas on generic declaring types.
- Explicit interface implementations and default interface methods, particularly virtual/base-method and `[Method]`
  delegate binding.

For supported state machines, write tests from the API user's perspective: patch the source-level method/call described
by the public attributes or `PatchConfig`, then assert source-level behavior. Do not assert compiler-generated names or
layout except in focused reflection unit tests.

## Boundary targets and validation

Create a compact set of public-API tests around uncommon method shapes that may reach Harmony, reflection, or code
generation differently. The goal is either correct execution or a specific early Disharmony exception—never a late
`RuntimePatchException`, process crash, or silently ineffective patch.

Candidates include abstract methods, interface methods, extern/P/Invoke methods, open generic declaring types, generic
method definitions versus constructed generic methods, varargs methods if representable, methods with function-pointer
or pointer signatures, and type initializers. The existing ignored static-constructor test should remain documented as a
Harmony limitation unless the underlying runtime behavior changes.

## Behavioral preservation corpus

Maintain a corpus of Release-compiled target methods that combine language and IL features rather than testing every
feature only in isolation. Apply a no-op outer patch, no-op inner patch, and—where meaningful—inline/optimized variants,
then compare behavior with an unpatched equivalent over representative inputs.

Combinations should cross exception handling, branching, loops, switches, pattern matching, casts, null-aware access,
value types, references, by-ref parameters, generics, closures, and state machines. Include both normal and exceptional
outcomes. This provides broad regression protection when rule generation, exception fixup, trampoline generation, or
the optimizer changes without tying expectations to a particular generated opcode sequence.

## Test-writing constraints

- Keep tests organized by the feature whose public behavior they primarily exercise.
- Put reflection-sensitive target and inline patch methods in `Source_Disharmony.TestTargets`, which is compiled in
  Release mode.
- End-to-end fixtures must fail immediately when `Patcher.RuntimeExceptionHandler` reports an unexpected exception.
- Enable the optimizer only in optimizer fixtures; keep it disabled everywhere else.
- Preserve valid tests that expose possible product bugs. Report and investigate the failure instead of weakening the
  expectation.
- Keep distinct by-value and by-reference struct paths separate; compiler optimizations can make them fundamentally
  different.
- Prefer self-contained tests. Introduce a helper only when at least ten tests genuinely benefit from it.
- Preserve CRLF line endings.
