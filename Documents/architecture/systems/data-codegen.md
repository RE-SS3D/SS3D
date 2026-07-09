> Code paths: Assets/Scripts/SS3D/Data/
> Entry points: AssetDatabase, AssetDatabasesCodeGenerator
> Status: stub

# Data / codegen

## Overview

ScriptableObject asset catalogs and codegen writers producing typed references (`Generated/Scenes.cs`, `Items.cs`, etc.).

## Start here

- `Assets/Scripts/SS3D/Data/AssetDatabases/AssetDatabase.cs` — asset database base
- `Assets/Scripts/SS3D/Data/AssetDatabases/AssetDatabasesCodeGenerator.cs` — codegen entry
- `Assets/Scripts/SS3D/Data/Generated/AssetDatabases.cs` — generated database refs

## Extension points

- New asset categories: extend asset database settings and rerun codegen.

## Depends on / Used by

- **Used by:** Most content-loading systems

## Related docs

- [INDEX.md](../INDEX.md)
