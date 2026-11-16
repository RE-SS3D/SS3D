using SS3D.Core;
using SS3D.Rendering.URP;
using SS3D.Systems.Vision;
using UnityEngine;

namespace SS3D.Systems.Tile.MapEditor
{
    /// <summary>
    /// Temporary authoring visibility while the map editor is open.
    /// <para>
    /// Scene lights / ambient cannot fullbright Simple Toon (ST only samples lights that
    /// actually reach the shader; dark rooms stay black). Instead we set
    /// <c>_SS3DAuthoringFullbright</c> on the toon path and suppress client vision FOV.
    /// </para>
    /// </summary>
    internal sealed class MapEditorLighting
    {
        private const string FullbrightGlobal = "_SS3DAuthoringFullbright";

        private bool _applied;

        public void Apply()
        {
            if (_applied)
                return;

            Shader.SetGlobalFloat(FullbrightGlobal, 1f);
            VisionRenderContext.Suppressed = true;
            VisionRenderContext.Enabled = false;

            if (SubSystems.TryGet(out VisionSubSystem vision))
                vision.SetSuppressed(true);

            _applied = true;
        }

        public void Restore()
        {
            if (!_applied)
                return;

            Shader.SetGlobalFloat(FullbrightGlobal, 0f);
            VisionRenderContext.Suppressed = false;

            if (SubSystems.TryGet(out VisionSubSystem vision))
                vision.SetSuppressed(false);

            _applied = false;
        }
    }
}
