using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace SS3D.Systems.Vision
{
    public sealed class VisionRenderer : PostProcessEffectRenderer<VisionMaskEffect>
    {
        private static readonly int FovBlurQuality = Shader.PropertyToID("_FovBlurQuality");
        private static readonly int FovBlurDirections = Shader.PropertyToID("_FovBlurDirections");
        private static readonly int FovBlurSize = Shader.PropertyToID("_FovBlurSize");
        private static readonly int PlayerCameraInvViewProj = Shader.PropertyToID("_PlayerCameraInvViewProj");
        private static readonly int FovTex = Shader.PropertyToID("_FovTex");

        public override void Render(PostProcessRenderContext context)
        {
            Camera camera = context.camera;

            if ((int)camera.depthTextureMode < 1)
            {
                camera.depthTextureMode += 1;
            }

            Matrix4x4 worldToCameraMatrix = context.camera.worldToCameraMatrix;
            Matrix4x4 projectionMatrix = GL.GetGPUProjectionMatrix(context.camera.projectionMatrix, false);
            Matrix4x4 viewProjectionMatrix = projectionMatrix * worldToCameraMatrix;
            RenderTexture fovTexture = RenderTexture.GetTemporary(Screen.width, Screen.height);

            PropertySheet sheet = context.propertySheets.Get(Shader.Find("Vision/VisionMask"));

            sheet.properties.SetMatrix(PlayerCameraInvViewProj, viewProjectionMatrix.inverse);

            context.command.BlitFullscreenTriangle(context.source, fovTexture, sheet, 0, false, null, true);
            
            sheet = context.propertySheets.Get(Shader.Find("Vision/VisionMaskBlur"));

            sheet.properties.SetFloat(FovBlurQuality, settings.Quality);
            sheet.properties.SetFloat(FovBlurDirections, settings.Directions);
            sheet.properties.SetVector(FovBlurSize, settings.Size);
            sheet.properties.SetMatrix(PlayerCameraInvViewProj, viewProjectionMatrix.inverse);
            sheet.properties.SetTexture(FovTex, fovTexture);

            context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0, false, null, true);

            RenderTexture.ReleaseTemporary(fovTexture);
        }
    }
}
