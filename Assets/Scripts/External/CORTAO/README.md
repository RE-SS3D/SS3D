# CORTAO (vendored)

Screen-space ambient occlusion by Daniel Isheden, MIT licensed (see `LICENSE.txt`).

- Source: https://github.com/theagentd/CORTAO
- Version: reference implementation v1.0.3, commit `420b168d88dd6f9276077ba62f3711cac9d7b46c`
- Files: `src/shaders/CORTAO*.h`, renamed to `.hlsl` so Unity tracks them as shader includes.
- Only change: each pass's `#include "CORTAOBase.h"` now names `CORTAOBase.hlsl`.

Do not edit these files to integrate CORTAO. The Unity side lives in the compute shader wrappers under
`Assets/Content/Resources/CORTAO/` (input/output callbacks, the Shader Model 6.7 workarounds) and in
`Assets/Scripts/SS3D/Rendering/URP/CortaoAmbientOcclusionFeature.cs`. The occluder lookup table is generated
at runtime by `CortaoOccluderTable.cs`, a port of `utils/generators/OccluderLookupTableGenerator.py`.

To update: copy the four headers from the new release, rename to `.hlsl`, reapply the include rename, and run
the `CortaoOccluderTableTests` EditMode test.
