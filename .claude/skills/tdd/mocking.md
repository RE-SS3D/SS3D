# When to Mock

Mock at **system boundaries** only:

- The network: FishNet `NetworkManager`, RPC delivery, connection state
- Time and frames: `Time.deltaTime`, tick counts
- Randomness: pass a seed or an `System.Random` in
- File system and config (`Builds/Game/Config/*`, `permissions.txt`)
- Engine services a pure-logic test can't start (physics queries, rendering)

Don't mock:

- Your own classes or SubSystems: register a real one with `SubSystems.Register` instead
- Internal collaborators
- Anything you control

## Designing for Mockability

**1. Pass the boundary in**

```csharp
// Easy to test: time and randomness are inputs
public static float NextSpreadAngle(float baseSpread, float heat, System.Random rng)
    => baseSpread * (1f + heat) * (float)(rng.NextDouble() * 2 - 1);

// Hard to test: reaches for globals
public float NextSpreadAngle()
    => _baseSpread * (1f + _heat) * UnityEngine.Random.Range(-1f, 1f);
```

**2. Keep the rule out of the NetworkBehaviour**

The `NetworkSubSystem` / `NetworkBehaviour` owns sync and authority; it calls a plain class that owns the rule. Test the plain class in EditMode with no network at all; leave the thin networked shell to PlayMode or the smoke harness.

**3. Prefer specific interfaces over generic ones**

One method per operation at a boundary (`IAccessReader.CanOpen(door, id)`) rather than a generic `Query(string kind, object args)`: each fake returns one specific shape, with no conditional logic in test setup.
