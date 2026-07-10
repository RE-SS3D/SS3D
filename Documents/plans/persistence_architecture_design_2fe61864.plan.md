---
name: Persistence Architecture Design
overview: "Introduce a layered, contributor-based persistence framework that generalizes the existing tilemap JSON pipeline, separates station templates from round runtime state, and phases delivery: server meta + map templates first, full round snapshots later."
todos:
  - id: phase-1a-framework
    content: Create PersistenceSubSystem, IPersistenceContributor, PersistenceEnvelope, and EnvelopePersistenceStore under Assets/Scripts/SS3D/Data/Persistence/
    status: pending
  - id: phase-1a-tilemap-refactor
    content: Extract TileMapPersistenceContributor and AreaPersistenceContributor from existing TileMap save logic; add LegacyTileMapMigrator
    status: pending
  - id: phase-1a-rewire
    content: Rewire TileSubSystem + TileMap Creator UI to use PersistenceSubSystem; replace OnMapLoaded with framework lifecycle events
    status: pending
  - id: phase-1a-tests
    content: Add EditMode tests for envelope round-trip, legacy migration, and contributor load ordering
    status: pending
  - id: phase-1b-server-meta
    content: "Implement server meta contributors: round config, permissions migration, round history JSONL; hook round-start map selection"
    status: pending
  - id: phase-2-snapshots
    content: "Phase 2: round snapshot contributors (items, electricity, substances, entities) with delta-from-template format and admin commands"
    status: pending
  - id: docs
    content: Add persistence system map and register in architecture INDEX; update rounds-lobby.md with persistence hooks (tile.md and area.md already synced in area-foundation commits)
    status: pending
isProject: false
---

# Persistence Architecture — Recommended Design

## Revision note (post area-foundation commits)

Recent commits (`5ebe0426`–`6fa79807`) shipped **APC-seeded area foundation (phases 0–2)** with save/load already wired into the tilemap pipeline. This validates the plan’s contributor split but does **not** change the recommended architecture — it gives us concrete extraction points and existing tests to build on.

**What changed since the original draft:**

- Area save/load is live: per-chunk `areaIds`, `SavedAreaRecord[]`, `AreaSubSystem.BuildSavedAreaRecords()`, restore via `TileMap.LoadedAreaRecords` + `OnMapLoaded`
- Architecture docs landed: [`area.md`](../architecture/systems/area.md), [`2026-07_area-foundation`](../architecture/2026-07_area-foundation.md), [`tile.md`](../architecture/systems/tile.md) updated (areas no longer stale)
- EditMode coverage exists: `AreaFloodFillTests.SaveLoad_PreservesAreaIdsAndMetadata`
- Area rebuild on load has a **dual path** (see load-order note below): if APCs are already registered, `AreaSubSystem` re-floods from live APCs instead of restoring saved metadata

**What did not change:** no central `PersistenceSubSystem`, no envelope format, no round-config integration, no round snapshots. Core recommendations stand.

---

## Current state (what exists)

The only real disk persistence today is a **tilemap authoring pipeline** (now including embedded area data):

```mermaid
flowchart LR
    TileMapCreatorUI --> TileSubSystem
    ServerBoot --> TileSubSystem
    TileSubSystem --> TileMap
    TileMap --> SavedTileMap
    TileSubSystem --> LocalStorage
    LocalStorage --> JSON["Builds/Game/Data/Tilemaps/*.json"]
    TileMap -->|OnMapLoaded| AreaSubSystem
```




| Piece             | Location                                                                             | Role                                                                    |
| ----------------- | ------------------------------------------------------------------------------------ | ----------------------------------------------------------------------- |
| Generic I/O       | `[LocalStorage.cs](Assets/Scripts/SS3D/Data/Management/LocalStorage.cs)`             | `JsonUtility` read/write to `Builds/Game/Data/`                         |
| Orchestration     | `[TileSubSystem.cs](Assets/Scripts/SS3D/Systems/Tile/TileSubSystem.cs)`              | Server-only `Save()` / `Load()`; auto-loads most recent map on boot     |
| DTO + walk        | `[TileMap.Save/Load](Assets/Scripts/SS3D/Systems/Tile/TileMap.cs)`, `SavedObjects/*` | Chunk/tile/item/area serialization                                      |
| Cross-system hook | `[AreaSubSystem](Assets/Scripts/SS3D/Systems/Area/AreaSubSystem.cs)`                 | `BuildSavedAreaRecords()` on save; restore via `TileMap.LoadedAreaRecords` + `OnMapLoaded` |
| Area storage      | `[TileChunk](Assets/Scripts/SS3D/Systems/Tile/TileChunk.cs)` area-id grid            | Per-chunk `ushort[] areaIds` serialized in `SavedTileChunk`            |
| Tests             | `[AreaFloodFillTests.SaveLoad_PreservesAreaIdsAndMetadata](Assets/Scripts/Tests/EditMode/AreaFloodFillTests.cs)` | Baseline for contributor extraction tests |


**What it is:** a **station layout template** editor, not gameplay persistence. Items in containers/inventory are explicitly excluded. Adjacency, electricity, substances, machine runtime state, round state, and player meta are not saved.

**Pain points to address:**

- No central orchestrator — `TileSubSystem` owns save/load directly
- `JsonUtility` + `[SerializeReference]` on `[SavedTileChunk](Assets/Scripts/SS3D/Systems/Tile/SavedObjects/SavedTileChunk.cs)` — bloated files, weak polymorphism, no schema versioning
- Asset identity is **prefab name strings** at save time; network uses **ushort catalog IDs** — two parallel identity systems
- Areas are coupled into tilemap DTOs (shipped in area foundation; still should be split into separate contributor chunks)
- Area restore depends on `OnMapLoaded` firing after tiles **and** APC NetworkObjects exist — contributor load order must guarantee this (see below)
- No integration with `[RoundSubSystem](Assets/Scripts/SS3D/Systems/Rounds/RoundSubSystem.cs)` — server boot always loads "most recent" map, not round-selected map
- Design docs (`[lobby.md](Documents/design/lobby.md)` §11, `[round-config.md](Documents/design/round-config.md)` §5) assume a **Persistence & accounts** layer that does not exist

---

## SS13 persistence domains (the mental model)

Space Station 13 has **four distinct persistence layers**. The framework should make these explicit rather than conflating them:

```mermaid
flowchart TB
    subgraph layer1 [Layer 1: StationTemplates]
        Maps["Map layouts"]
        Areas["Area metadata"]
    end

    subgraph layer2 [Layer 2: ServerMeta]
        RoundConfig["Round config pools"]
        Permissions["Admin permissions"]
        RoundHistory["Round history log"]
    end

    subgraph layer3 [Layer 3: PlayerMeta]
        Playtime["Playtime / job unlocks"]
        Prefs["Saved preferences"]
        Bans["Bans / notes"]
    end

    subgraph layer4 [Layer 4: RoundSnapshots]
        WorldState["Full mid-round world state"]
        Checkpoints["Admin checkpoints / crash recovery"]
    end

    subgraph diegetic [Diegetic — NOT disk persistence]
        GeneticsDB["Genetics / crew records console"]
        IDCards["ID cards, physical objects"]
    end

    layer1 -->|"loaded at round start"| Sim[Live FishNet simulation]
    layer2 -->|"read at lobby / boot"| Sim
    layer3 -->|"read at connect / lobby"| Sim
    layer4 -.->|"Phase 2"| Sim
    diegetic -->|"SyncVar state within round"| Sim
```



**Critical design rule (from `[death-cloning-respawn.md](Documents/design/death-cloning-respawn.md)` §4):** genetics/crew records are **in-fiction physical databases** — destructible station objects synced via FishNet, not external save files. The persistence framework must not shortcut this.

---

## Recommended architecture

### Core: `PersistenceSubSystem` + contributor pattern

Add a new infrastructure subsystem at `[Assets/Scripts/SS3D/Data/Persistence/](Assets/Scripts/SS3D/Data/Persistence/)` that owns **when** and **what** gets saved, while each domain owns **how** its data is serialized.

```csharp
// Contracts (sketch)
public enum PersistenceLayer { StationTemplate, ServerMeta, PlayerMeta, RoundSnapshot }

public interface IPersistenceContributor
{
    string ContributorId { get; }           // e.g. "tilemap", "areas", "electricity"
    PersistenceLayer Layer { get; }
    int LoadOrder { get; }                   // lower = loaded first
    object Capture();                        // domain DTO
    void Restore(object data, PersistenceContext ctx);
}

public interface IPersistenceStore
{
    bool TrySave(string path, object envelope, bool overwrite);
    bool TryLoad<T>(string path, out T envelope);
    IReadOnlyList<string> List(string directory);
}
```

`**PersistenceSubSystem**` responsibilities:

- Register contributors at startup (each subsystem calls `RegisterContributor` in `OnStartServer`)
- Orchestrate **layered save/load** with ordered contributors and explicit lifecycle hooks
- Expose admin/API entry points: `SaveStationTemplate(name)`, `LoadStationTemplate(name)`, `SaveRoundSnapshot(name)` (Phase 2)
- Fire events: `OnBeforeRestore`, `OnAfterRestore`, `OnBeforeCapture` — replaces ad-hoc `OnMapLoaded` pattern
- Enforce **server-only** disk I/O (same rule as tilemap today)

`**PersistenceEnvelope**` — wrapper for all saves:

```csharp
[Serializable]
public class PersistenceEnvelope
{
    public int schemaVersion;          // migration support
    public string envelopeType;        // "station-template" | "round-snapshot" | "server-meta"
    public string createdAt;           // ISO timestamp
    public string gameVersion;         // build identifier
    public List<PersistenceChunk> chunks;
}

[Serializable]
public class PersistenceChunk
{
    public string contributorId;
    public string payloadJson;         // contributor-owned DTO, serialized separately
}
```

Each contributor serializes its own DTO into `payloadJson`. This avoids `[SerializeReference]` across the whole document, keeps domains decoupled, and allows contributors to migrate their own schema independently.

### Storage layout

```
Builds/Game/Data/
├── StationTemplates/
│   └── outpost-station.json          # envelope with tilemap + areas chunks
├── ServerMeta/
│   ├── round-config.json
│   ├── permissions.json              # migrate from Config/
│   └── round-history.jsonl           # append-only log
├── PlayerMeta/
│   └── {ckey}.json                   # playtime, prefs (Phase 1b)
└── RoundSnapshots/                   # Phase 2
    └── 2026-07-10_143022.json
```

Keep `[LocalStorage](Assets/Scripts/SS3D/Data/Management/LocalStorage.cs)` as the low-level file primitive; add `EnvelopePersistenceStore` on top that reads/writes `PersistenceEnvelope`.

### Serializer strategy

**Phase 1:** Keep `JsonUtility` for contributor DTOs (minimal migration cost). The envelope wrapper is simple enough for `JsonUtility`.

**Phase 1.5 / 2:** Evaluate **Newtonsoft.Json** (already common in Unity projects) or **System.Text.Json** for contributor payloads that need polymorphism, dictionaries, or `[JsonProperty]`. The contributor boundary means this can be adopted per-domain without rewriting everything.

Add `**IAssetResolver**` for load-time identity:

```csharp
public interface IAssetResolver
{
    bool TryResolve(string assetKey, out GenericObjectSo asset);  // prefab name (legacy)
    bool TryResolve(ushort assetId, out GenericObjectSo asset);  // catalog ID (preferred)
    string GetStableKey(GenericObjectSo asset);                    // canonical save key
}
```

Migrate tilemap saves from raw prefab names to **catalog-backed stable keys** with fallback to legacy names on load.

---

## Refactoring the tilemap save system

### Split tilemap into two contributors


| Contributor                     | Layer           | Captures                                  | Notes                                                      |
| ------------------------------- | --------------- | ----------------------------------------- | ---------------------------------------------------------- |
| `TileMapPersistenceContributor` | StationTemplate | `SavedTileMap` minus areas                | Existing `TileMap.Save/Load` logic, extracted              |
| `AreaPersistenceContributor`    | StationTemplate | `SavedAreaRecord[]` + per-chunk `areaIds` | Wrap existing `AreaSubSystem.BuildSavedAreaRecords()` + restore path; decouple from `SavedTileMap` DTO |


**Load order:** tilemap first (creates chunks, tiles, and per-chunk `areaIds`), then APC NetworkObjects spawn/register, then areas contributor restores registry.

**Important (from shipped `AreaSubSystem.HandleMapLoaded`):** if APCs are already registered when the map finishes loading, the subsystem calls `RebuildAllAreasFromApcs()` and **ignores** saved area metadata. The persistence orchestrator must either (a) restore area metadata only after tiles are placed but before APC registration, or (b) expose a explicit `RestoreFromSave()` on `AreaSubSystem` that takes precedence over live re-flood. Option (b) is cleaner for the contributor model.

**Backward compatibility:** `PersistenceSubSystem` detects legacy flat `SavedTileMap` JSON (no envelope wrapper) and routes through a `LegacyTileMapMigrator` that wraps it into the new format on first save.

### Slim down `TileSubSystem`

`[TileSubSystem.Save/Load](Assets/Scripts/SS3D/Systems/Tile/TileSubSystem.cs)` become thin delegates:

```csharp
// Before: TileSubSystem owns file I/O directly
// After:
public void Save(string mapName, bool overwrite)
    => SubSystems.Get<PersistenceSubSystem>()
         .SaveStationTemplate(mapName, overwrite);
```

TileMap Creator UI (`[TileMapSaveTab](Assets/Scripts/SS3D/Systems/Tile/TileMapCreator/TileMapSaveTab.cs)`, `[TileMapLoadTab](Assets/Scripts/SS3D/Systems/Tile/TileMapCreator/TileMapLoadTab.cs)`) continues to work; only the backend changes.

### Replace `OnMapLoaded` with framework events

`[TileMap.OnMapLoaded](Assets/Scripts/SS3D/Systems/Tile/TileMap.cs)` → `PersistenceSubSystem.OnAfterRestore(PersistenceLayer.StationTemplate)`. `[AreaSubSystem.HandleMapLoaded](Assets/Scripts/SS3D/Systems/Area/AreaSubSystem.cs)` subscribes to the persistence event instead of tilemap-specific event.

---

## Round lifecycle integration

Today the server auto-loads the most recent tilemap on boot, ignoring round config. The framework should wire persistence into the round state machine:

```mermaid
sequenceDiagram
    participant Boot as ServerBoot
    participant Persist as PersistenceSubSystem
    participant Round as RoundSubSystem
    participant RC as RoundConfig
    participant Sim as GameplaySystems

    Boot->>Persist: LoadServerMeta()
    Note over Persist: round-config, permissions, history

    Round->>Round: Preparing
    Round->>RC: DrawMapFromPool()
    RC-->>Round: selectedMapId
    Round->>Persist: LoadStationTemplate(selectedMapId)
    Persist->>Sim: Restore contributors in order

    Round->>Round: Ongoing
    Note over Sim: Live FishNet state only

    Round->>Round: Ending
  Note over Persist: Phase 2 — optional SaveRoundSnapshot
    Round->>Persist: AppendRoundHistory(drawn mode, map, duration)
    Note over Sim: World resets on next Preparing
```



**Round start:** load station template selected by round config pool (`[round-config.md](Documents/design/round-config.md)`), not "most recent file." Keep "load most recent" as a **map editor dev default** only.

**Round end:** append to `round-history.jsonl`; do **not** auto-save round snapshots unless admin requests it (Phase 2).

---

## Phase 2: Round snapshot contributors

When implementing full mid-round persistence, each gameplay domain registers a contributor at `PersistenceLayer.RoundSnapshot`:


| Contributor (future)                | Priority | Captures                                                          |
| ----------------------------------- | -------- | ----------------------------------------------------------------- |
| `TileMapPersistenceContributor`     | early    | Structural diffs from template (damaged walls, new constructions) |
| `ItemPersistenceContributor`        | mid      | World items + container contents (requires `Item` serialization)  |
| `ElectricityPersistenceContributor` | mid      | Circuit charge, APC cell levels, cable breaks                     |
| `SubstancePersistenceContributor`   | mid      | Tank/pipe contents                                                |
| `EntityPersistenceContributor`      | late     | Humanoid health, inventory, mind links                            |
| `GamemodePersistenceContributor`    | late     | Objectives, antag assignments                                     |


**Snapshot vs template:** a round snapshot references a `baseTemplateId` and stores **deltas** where possible (only changed tiles, moved items). Full snapshots are a fallback for admin "save round" commands.

**FishNet consideration:** persistence runs **server-only on disk**. After restore, normal `NetworkObject` spawn + `SyncVar` replication brings clients up to date. No save/load RPCs to clients. Late-joiners after restore use existing spawn flow.

**Load ordering matters:** structural (tilemap) → infrastructure (power, atmospherics) → entities (mobs, items in hands). The `LoadOrder` field on contributors enforces this.

---

## Server meta contributors (Phase 1)


| Contributor                          | Source today                                                                                                         | Action                                                                   |
| ------------------------------------ | -------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------ |
| `PermissionsPersistenceContributor`  | `[PermissionSubSystem](Assets/Scripts/SS3D/Permissions/PermissionSubSystem.cs)` reads `permissions.txt` from Config/ | Migrate to envelope; keep txt import for backward compat                 |
| `RoundConfigPersistenceContributor`  | Design only (`[round-config.md](Documents/design/round-config.md)`)                                                  | New; pool model with enabled/weight/preconditions                        |
| `RoundHistoryPersistenceContributor` | None                                                                                                                 | Append-only JSONL: timestamp, gamemode, map, player count, fallback flag |


---

## Player meta contributors (Phase 1b)

Deferred until accounts/auth exist, but design for it now:

- `PlayerMetaPersistenceContributor` — per-ckey file: playtime per role, job unlock state, lobby preference defaults
- Ties into `[lobby.md](Documents/design/lobby.md)` playtime-gated jobs
- Requires real authentication (today ckey is client-trusted in `[PlayerSubSystem](Assets/Scripts/SS3D/Systems/PlayerControl/PlayerSubSystem.cs)`)

---

## What NOT to build into disk persistence

- **Genetics/crew records** — in-world objects per design spec
- **ID card contents** — physical items; saved only in round snapshots (Phase 2) as item state
- **Machine UI snapshots** (`[ApcInterfaceSnapshot](Assets/Scripts/SS3D/UI/MachineInterface/ApcInterfaceSnapshot.cs)`) — derived read models, rebuilt from sim state
- **FishNet wire serializers** — keep separate from disk format; optionally share DTO shapes where sensible

---

## Implementation phases

### Phase 1a — Framework + station templates (first ship)

1. Create `PersistenceSubSystem`, `IPersistenceContributor`, `PersistenceEnvelope`, `EnvelopePersistenceStore`
2. Extract `TileMapPersistenceContributor` and `AreaPersistenceContributor` from existing save logic
3. Add `LegacyTileMapMigrator` for old flat JSON files
4. Rewire `TileSubSystem` and TileMap Creator UI to use framework
5. Replace `OnMapLoaded` with persistence lifecycle events
6. Add EditMode tests: envelope round-trip, legacy migration, contributor load ordering — extend/migrate existing `AreaFloodFillTests.SaveLoad_PreservesAreaIdsAndMetadata` rather than rewriting from scratch

### Phase 1b — Server meta

1. `RoundConfigPersistenceContributor` (when round config is implemented)
2. Migrate permissions to contributor
3. `RoundHistoryPersistenceContributor` + hook into `RoundSubSystem` ending state
4. Wire round-start map selection through `PersistenceSubSystem.LoadStationTemplate(mapId)`

### Phase 2 — Round snapshots

1. Define delta format for tilemap changes from template
2. Item/container serialization (biggest new work — inventory is currently stub)
3. Electricity, substances contributors
4. Entity/mind/gamemode contributors
5. Admin console commands: `save_round`, `load_round`, `list_snapshots`
6. Optional: periodic auto-checkpoint for crash recovery

### Ongoing — docs

- Add `Documents/architecture/systems/persistence.md` system map
- Update `[rounds-lobby.md](Documents/architecture/systems/rounds-lobby.md)` with persistence hooks (round-start template load, round-end history append)
- Register persistence in `[INDEX.md](Documents/architecture/INDEX.md)`
- Already done (area-foundation commits): `[tile.md](Documents/architecture/systems/tile.md)`, `[area.md](Documents/architecture/systems/area.md)`, `[2026-07_area-foundation.md](Documents/architecture/2026-07_area-foundation.md)`

---

## Key design decisions (summary)


| Decision             | Recommendation                               | Rationale                                                             |
| -------------------- | -------------------------------------------- | --------------------------------------------------------------------- |
| Central orchestrator | `PersistenceSubSystem`                       | Matches existing `SubSystem` pattern; single lifecycle owner          |
| Domain serialization | Contributor pattern                          | Tilemap refactor is proof-of-concept; scales to 10+ domains           |
| File format          | Envelope + per-chunk JSON                    | Decouples domains, enables versioning, fixes SerializeReference bloat |
| Station vs round     | Separate layers + directories                | SS13 resets world each round; templates != snapshots                  |
| Asset identity       | Catalog stable keys + legacy fallback        | Unifies save and network identity over time                           |
| Serializer           | JsonUtility now, Newtonsoft per-domain later | Minimize Phase 1 churn; upgrade at contributor boundary               |
| Diegetic records     | Excluded from disk                           | Honors design spec; keeps stakes physical                             |


