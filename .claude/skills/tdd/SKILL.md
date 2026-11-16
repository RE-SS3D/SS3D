---
name: tdd
description: Test-driven development. Use when the user wants to build features or fix bugs test-first, mentions "red-green-refactor", or wants integration tests.
---

# Test-Driven Development

TDD is the red → green loop. This skill is the reference that makes that loop produce tests worth keeping: what a good test is, where tests go, the anti-patterns, and the rules of the loop. Every section applies on every cycle: consult them before and during the loop, not after.

Before choosing seams, read the domain's system map (via `Documents/architecture/INDEX.md`) and its **Pitfalls**, and the design doc sections the work implements: test names use the design doc's vocabulary, and expected values come from its rules and worked examples.

## What a good test is

Tests verify behavior through public interfaces, not implementation details. Code can change entirely; tests shouldn't. A good test reads like a specification: "user can checkout with valid cart" tells you exactly what capability exists, and it survives refactors because it doesn't care about internal structure.

See [tests.md](tests.md) for examples and [mocking.md](mocking.md) for mocking guidelines.

## Seams: where tests go

A **seam** is the public boundary you test at: the interface where you observe behavior without reaching inside. Tests live at seams, never against internals.

**Test only at pre-agreed seams.** Before writing any test, write down the seams under test and confirm them with the user. No test is written at an unconfirmed seam. You can't test everything, so agreeing the seams up front is how testing effort lands on the critical paths and complex logic instead of every edge case.

Ask: "What's the public interface, and which seams should we test?"

### SS3D seams

- **Pure logic, EditMode** (`Assets/Scripts/Tests/EditMode`, NUnit): the default. Rules live in plain/static classes the MonoBehaviour or `NetworkSubSystem` calls into (e.g. `ArmorSimulation.ResolveAbsorption`). When the logic is buried in a MonoBehaviour, extracting that class *is* creating the seam; agree it with the user first.
- **SubSystem, EditMode**: register a real or test subsystem with `SubSystems.Register` and unregister in teardown (see `EditMode/IdAccessTests/IdAccessTestFixtures.cs`).
- **Scene / network, PlayMode** (`Assets/Scripts/Tests/PlayMode`): only for behaviour that needs frames, scenes, or FishNet. Slower; use sparingly.
- **Cross-process**: the multiplayer smoke harness (`Testing/multiplayer/`) is a verification loop, not a TDD seam.

Run tests via Unity MCP `run_tests` with a filter on the one test, or headless per `.cursor/skills/fix-editmode-tests/SKILL.md`.

## Anti-patterns

- **Implementation-coupled**: mocks internal collaborators, tests private methods, or verifies through a side channel (querying the database instead of using the interface). The tell: the test breaks when you refactor but behavior hasn't changed.
- **Tautological**: the assertion recomputes the expected value the way the code does (`expect(add(a, b)).toBe(a + b)`, a snapshot derived by hand the same way, a constant asserted equal to itself), so it passes by construction and can never disagree with the code. Expected values must come from an independent source of truth: a known-good literal, a worked example, the spec.
- **Horizontal slicing**: writing all tests first, then all implementation. Bulk tests verify _imagined_ behavior: you test the _shape_ of things rather than user-facing behavior, the tests go insensitive to real changes, and you commit to test structure before understanding the implementation. Work in **vertical slices** instead: one test → one implementation → repeat, each test a **tracer bullet** that responds to what the last cycle taught you.

## Rules of the loop

- **Red before green.** Write the failing test first, then only enough code to pass it. Don't anticipate future tests or add speculative features.
- **One slice at a time.** One seam, one test, one minimal implementation per cycle.
- **Refactoring is not part of the loop.** It belongs to the review stage (see the `spec-review` skill), not the red → green implementation cycle.
