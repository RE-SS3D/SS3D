> Code paths: Assets/Scripts/SS3D/Data/
> Entry points: AssetDatabase, AssetDatabasesCodeGenerator
> Status: stub

# Data / codegen

## Overview

ScriptableObject asset catalogs, codegen writers producing typed references (`Generated/Scenes.cs`, `Items.cs`, etc.), and shared disk I/O helpers under `Data/Management/`.

## Start here

- `Assets/Scripts/SS3D/Data/AssetDatabases/AssetDatabase.cs` — asset database base
- `Assets/Scripts/SS3D/Data/AssetDatabases/AssetDatabasesCodeGenerator.cs` — codegen entry
- `Assets/Scripts/SS3D/Data/Generated/AssetDatabases.cs` — generated database refs
- `Assets/Scripts/SS3D/Data/Management/LocalStorage.cs` — `JsonUtility` file I/O, append JSONL, legacy path helpers

## Extension points

- New asset categories: extend asset database settings and rerun codegen.

## Depends on / Used by

- **Used by:** Most content-loading systems; [persistence](persistence.md) (`EnvelopePersistenceStore`, `RoundHistoryStore`)

## Related docs

- [INDEX.md](../INDEX.md)
