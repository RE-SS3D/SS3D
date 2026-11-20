> Implements: infrastructure — no dedicated design doc; would let MI/Main HUD/Storage Panel migrate their catalogs onto [systems/ui-shell.md](systems/ui-shell.md)'s shipped `UiAssetCatalogBase` instead of `Resources.Load`, and closes [TECH_DEBT.md](TECH_DEBT.md) §1.4–1.5, §1.15 #1
> Touches systems: data-codegen, ui-shell, networking-session, machine-interface, inventory
> Status: in-progress

**Progress:** Phases 1–3 done; 4–6 open.

# Addressables expansion & migration

Scopes turning the project's already-configured but functionally inert Addressables setup into the
thing it's supposed to be: on-demand async asset loading with ref-counting and unload, instead of
every configured asset being hard-referenced into RAM at startup.

## Why

The repository tracks this exact problem as
[RE-SS3D/SS3D#1494](https://github.com/RE-SS3D/SS3D/issues/1494) ("Excessive memory usage" — up to
600MB from `AssetDatabase.Assets` directly referencing everything from the start) and had an
unreviewed rewrite attempting to fix it, left open at the 2026 merge because its author plans to port
it onto the merged `develop`: [RE-SS3D/SS3D#1500](https://github.com/RE-SS3D/SS3D/pull/1500) ("Make adressables great again").

The merged codebase has the identical root cause, independently of that PR. Investigated
2026-07-21:

## Current state

- **Addressables package is present and configured.** `com.unity.addressables` **2.9.1** pinned
  directly in `Packages/manifest.json` (Unity 6), active settings at
  `Assets/Content/Addressables/AddressableAssetSettings.asset`, with **22 configured groups**
  (Items, Materials, Sounds, ParticlesEffects, InteractionIcons, UIElements, Settings, Scenes,
  Built In Data, Default Local Group, plus Localization package locale/table groups). CraftingRecipes
  group was purged with TECH_DEBT 1.6.
- **Runtime Addressables loading exists for InteractionIcons only (Phase 3).** Other databases still
  use eager `AssetDatabase.Assets` hard refs + sync `Assets.Get`. Shared infrastructure:
  `AssetHandle<T>` / `AssetProvider` / `Assets.GetAsync` / `AssetDatabaseLoadMode.AddressablesAsync`.
- **Orphaned cruft removed (Phase 1):** `Assets/AddressableAssetsData/` deleted; single settings root
  is `Assets/Content/Addressables/`.
- **Already-acknowledged debt pointing at this gap:**
  [TECH_DEBT.md](TECH_DEBT.md) §1.4–1.5, §1.15,
  [ui-shell.md](systems/ui-shell.md) / [2026-07_ui-shell-consolidation.md](2026-07_ui-shell-consolidation.md)
  (MI/Main HUD/Storage still `Resources.Load`), [TECH_DEBT.md](TECH_DEBT.md) § 1.5 / § 1.15.

## What RE-SS3D/SS3D#1500 offers (and why not to merge it wholesale)

The PR's shape is a reasonable reference design:

- `AssetRequest<T>` / `AssetHandle<T>` — ref-counted, disposable, async load handles replacing
  direct object references
- `AssetProvider`, `AssetLifecycleTracker`, `InstanceLifetimeTracker` — dedupe concurrent loads,
  track live instances for unload
- `NetworkBarrier` / `PreloadCondition` — FishNet-specific: makes clients preload an asset before a
  networked spawn RPC references it, and gives prefabs deterministic GUID-based network IDs instead
  of relying on FishNet's default prefab-collection index
- Deletes the legacy static `Assets` lookup entirely once migrated

It is **not mergeable as-is**: it targets Unity 2021.3 and the pre-merge `Data/` layout, the codebase
has since diverged substantially (Unity 6, different codegen), it was never reviewed, and it migrates ~470 call sites/assets in one shot with only automated test coverage
(no confirmed manual multiplayer smoke). Adopt the **design** — async handle + ref-count model, and
critically the network-preload-barrier idea, since SS3D is multiplayer-first via FishNet — not the
diff.

## Target model

1. **`AssetHandle<T>`-style async load/release** replacing `Assets.Get<T>` — `AssetDatabase.Get<T>`
   becomes `async Task<AssetHandle<T>> GetAsync<T>(id)` (or a Unity-idiomatic
   `Addressables.LoadAssetAsync` wrapper), ref-counted so multiple callers requesting the same asset
   share one load and release triggers `Addressables.Release` only when the count hits zero.
2. **Keep the existing `AssetDatabase`/`Assets` id-lookup surface and codegen** (`DatabaseID` +
   per-asset string id, `Generated/AssetDatabases.cs`) — this is orthogonal to sync-vs-async and
   already works; only the storage/retrieval mechanism underneath changes from a hard-referenced
   dictionary to an Addressables key lookup.
3. **FishNet spawn ordering** — any networked prefab or synced asset reference needs to be resolvable
   client-side before the spawn/RPC that uses it lands, same problem #1500's `NetworkBarrier`
   solves. Needs a concrete design pass against our `NetworkSpawner`/`NetworkObjectsGenerator`
   equivalents (see [networking-session.md](systems/networking-session.md)) rather than assuming
   #1500's mechanism transfers unchanged.
4. **Migrate database-by-database, not all at once** — start with a database that's easy to verify in
   isolation and has no networked-spawn dependency (e.g. `InteractionIcons` — UI-only, no FishNet
   path) before touching anything that gates a networked prefab spawn (`Items`).

## Phases

1. **Cleanup + baseline** — **done (2026-07-23).** Deleted orphaned `Assets/AddressableAssetsData/`;
   pinned `com.unity.addressables` 2.9.1 in manifest; confirmed settings:
   - Active settings: `Assets/Content/Addressables/` (`DefaultObject` GUID points here).
   - `m_ActivePlayerDataBuilderIndex: 3` = PackedMode; `m_BuildRemoteCatalog: 0`; local profiles only.
   - **`m_BuildAddressablesWithPlayerBuild: 0` kept** — do not flip without verifying CI/player build
     scripts. Content builds remain an explicit Editor step (Addressables Groups window → Build →
     New Build → Default Build Script) or a future pipeline hook.
   - **Baseline verification checklist:**
     - [ ] Editor Play Mode still resolves Localization Addressables (indirect; already working).
     - [ ] Manual Addressables content build against `Content/Addressables` succeeds (no missing-schema).
     - [ ] InteractionIcons radial icons visible after Phase 3 preload (Editor Play Mode).
     - Player-build streaming is **not** proven by Editor Play Mode alone (Editor fast-path can mask
       failures) — see Open questions.
2. **Async handle infrastructure** — **done (2026-07-23).** `AssetHandle<T>`, `AssetProvider`,
   `IAssetLoadBackend` / `AddressablesLoadBackend`, `Assets.GetAsync`, EditMode
   `AssetProviderTests` (ref-count share, double-release, missing key, concurrent missing, preload
   cache, concurrent dedupe). Load-failure path wakes waiters with `TrySetResult(false)` — do not
   `TrySetException` on an unawaited Loading TCS (unobserved UniTask → EditMode LogAssert poison).
   Sync `Assets.Get` remains for non-migrated DBs.
3. **First migration slice: `InteractionIcons`** — **done (2026-07-23).**
   `LoadMode = AddressablesAsync`; GUID keys in `AssetKeys`; serialized `Assets` dict emptied;
   warm preload at `ApplicationInitializing` via `Assets.PreloadAddressableDatabases()`;
   sync `Get` / `InteractionIconLookup` read the Addressables cache; sprite builder still authors
   PNGs→Sprites into the Addressables group (no longer relies on eager SO refs for runtime).
4. **Network-spawn-coupled databases (`Items`, `Materials`)** — requires the
   FishNet preload-ordering design from Target model §3 to land first.
5. **Remove sync `Assets.Get<T>` path** once all call sites are migrated; delete the now-redundant
   `SerializableDictionary<string, Object> Assets` field and `LoadAssetsFromAssetGroup` eager-copy
   step from `AssetDatabase.cs`.
6. **Retire the per-surface `Resources.Load` catalogs** ([TECH_DEBT.md](TECH_DEBT.md) § 1.4) — this
   is the concrete mechanism, not a vague fold-in:

   | Surface | Today | After |
   |---|---|---|
   | Machine UI | `MachineUiAssetPaths` consts + committed `MachineUiAssetCatalog` SO, rebuilt via **SS3D → Data → Rebuild All UI Catalogs** (`MachineUiAssetCatalogBuilder.cs`), loaded with `Resources.Load` | `Addressables.LoadAssetAsync<VisualTreeAsset>("mi/<template>")` (or the `AssetHandle<T>` wrapper from §1) against a stable address; no committed catalog SO |
   | Main HUD | `MainHudAssetPaths` + `MainHudAssetCatalog` SO, rebuilt via **SS3D → Data → Rebuild All UI Catalogs** (`MainHudAssetCatalogBuilder.cs`) | same pattern, `"mainhud/<template>"` addresses |
   | Storage Panel | `StoragePanelAssetCatalog`, rebuilt via **SS3D → Data → Rebuild All UI Catalogs** (`StoragePanelAssetCatalogBuilder.cs`) | same pattern, `"storagepanel/<template>"` addresses |

   The failure mode this removes: today, forgetting to re-run a rebuild menu after adding/renaming a
   UXML/USS asset leaves the committed catalog SO silently serving a stale reference — nothing catches
   it until Play Mode or a build. With Addressables addressing there is no second catalog to go stale;
   the Addressables group config *is* the source of truth, and a missing/renamed address fails loudly
   (`LoadAssetAsync` throws/logs "key not found") at the point of use instead of silently no-op'ing.
   Migrate MI first (best-understood surface, see [2026-07_mi-path-catalog.md](../archive/architecture/2026-07_mi-path-catalog.md)),
   then Main HUD, then Storage Panel; delete each `*AssetCatalog` SO + Editor rebuild menu once its
   surface is migrated.

   **What this does *not* remove:** `InteractionIconSpriteBuilder` (**SS3D → Interactions → Rebuild
   Interaction Icon Sprites**) is a content-authoring step — generating `Sprite` assets from source
   PNGs, including the `Sprite.Create`-goes-null workaround documented in
   [data-codegen.md](systems/data-codegen.md) § Pitfalls — not a loading-mechanism problem. That menu
   still needs to exist; only its *output* (the generated sprites) should land in an Addressables
   group loaded async like everything else in this doc, rather than the eager `AssetDatabase`
   dictionary it uses today.

## Open questions / risks

- **Bundle build verification.** Content-bundle build path still needs a real player build-and-run
  pass; Editor Play Mode can mask load failures via the Editor fast-path. Checklist above tracks the
  manual content build; CI gate not yet added.
- **Dedicated server implications.** `UNITY_SERVER` builds ([networking-session](systems/networking-session.md),
  headless dedicated server) skip renderers/audio but still need correct gameplay assets — async
  loading changes startup-order assumptions there too (InteractionIcons preload is client-UI-only,
  low risk; Items migration will not be).
- **Test coverage:** EditMode `AssetProviderTests` cover handle/ref-count with a fake backend
  (including missing-key / concurrent-missing without unobserved UniTask LogAssert poison).
  End-to-end Addressables load in Play Mode / player build still manual.

## Explicit non-goals (this doc)

- Porting RE-SS3D/SS3D#1500's code directly — design reference only, per above.
- Migrating MI/Main HUD/Storage Panel onto `UiShellSubSystem` — that's
  [2026-07_ui-shell-consolidation.md](2026-07_ui-shell-consolidation.md)'s own later phases; this
  effort only makes their eventual catalog migration land on async Addressables loading instead of
  the current `Resources.Load` pattern once that migration happens.
- Scene/bundle-based Addressables (remote content delivery, CDN hosting) — local-bundle async
  loading only, matching the project's build-from-source / numbered-release distribution model
  ([ci-develop-release-pipeline](2026-07_ci-develop-release-pipeline.md)).

## Related docs

- [TECH_DEBT.md](TECH_DEBT.md) §1.4–1.5, §1.15
- [systems/ui-shell.md](systems/ui-shell.md), [2026-07_ui-shell-consolidation.md](2026-07_ui-shell-consolidation.md)
- [systems/networking-session.md](systems/networking-session.md)
- [TECH_DEBT.md](TECH_DEBT.md) § 1.4, § 1.5, § 1.15
- [TECH_DEBT.md](TECH_DEBT.md) § 6 (resolved 1.7: self-initializing subsystems) — unrelated root cause, do not conflate
- Issue and PR: [RE-SS3D/SS3D#1494](https://github.com/RE-SS3D/SS3D/issues/1494), [RE-SS3D/SS3D#1500](https://github.com/RE-SS3D/SS3D/pull/1500)
