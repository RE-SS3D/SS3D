# Good and Bad Tests

## Good Tests

**Behaviour through the public seam**: call the real logic, assert on what a caller or player would observe.

```csharp
// GOOD: Tests observable behaviour, name reads like a rule from the design doc
[Test]
public void OverflowDamagePassesThroughAfterAbsorption()
{
    (float remainingBrute, _, _) =
        ArmorSimulation.ResolveAbsorption(profile, currentIntegrity: 10f, incomingBrute: 12f, incomingBurn: 0f);

    Assert.AreEqual(7f, remainingBrute);
}
```

Characteristics:

- Tests behaviour callers / players care about
- Uses the public API only
- Survives internal refactors
- Describes WHAT, not HOW
- One logical assertion per test

## Bad Tests

**Implementation-detail tests**: coupled to internal structure.

```csharp
// BAD: Reaches into private state via reflection
[Test]
public void ApplyDamageSetsPrivateField()
{
    health.ApplyDamage(5f);
    var field = typeof(HealthComponent).GetField("_brute", BindingFlags.NonPublic | BindingFlags.Instance);
    Assert.AreEqual(5f, field.GetValue(health));
}
```

Red flags:

- Mocking internal collaborators or your own SubSystems
- Testing private methods / reflection into fields
- Asserting on call counts or order
- Test breaks when refactoring without behaviour change
- Test name describes HOW not WHAT
- Verifying through a side channel (scene hierarchy, serialized asset) instead of the interface

```csharp
// BAD: Verifies through the scene instead of the interface
[Test]
public void SpawnItemCreatesGameObject()
{
    items.Spawn("Crowbar", position);
    Assert.IsNotNull(GameObject.Find("Crowbar(Clone)"));
}

// GOOD: Verifies through the interface
[Test]
public void SpawnedItemIsRetrievableAtPosition()
{
    Item item = items.Spawn("Crowbar", position);
    Assert.AreSame(item, items.GetAt(position));
}
```

**Tautological tests**: the expected value restates the implementation, so the test passes by construction.

```csharp
// BAD: Expected value is recomputed the way the code computes it
[Test]
public void AbsorbsUpToProfileValue()
{
    float expected = Mathf.Min(incoming, profile.BruteAbsorption);
    Assert.AreEqual(expected, ArmorSimulation.Absorbed(profile, incoming));
}

// GOOD: Expected value is an independent literal, ideally a design-doc worked example
[Test]
public void AbsorbsUpToProfileValue()
{
    Assert.AreEqual(5f, ArmorSimulation.Absorbed(profileWithFiveBrute, incoming: 12f));
}
```
