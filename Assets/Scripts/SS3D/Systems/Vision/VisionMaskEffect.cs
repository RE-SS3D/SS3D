@@ -1,58 +1,30 @@
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.Rendering;

[Serializable]
[PostProcess(typeof(VisionRenderer), PostProcessEvent.AfterStack, "Vision/VisionMask")]
public sealed class VisionMaskEffect : PostProcessEffectSettings
namespace SS3D.Systems.Vision
{
    public FloatParameter Quality = new FloatParameter {value = 5};
    public FloatParameter Directions = new FloatParameter {value = 25};
    public Vector2Parameter Size = new Vector2Parameter {value = new Vector2(5,5)};

    public override bool IsEnabledAndSupported(PostProcessRenderContext context)
    {
        return enabled.value
            && Quality.value > 0f
            && Directions.value > 0f
            && Size.value.x >0f
            && Size.value.y >0f;
    }
}
public sealed class VisionRenderer : PostProcessEffectRenderer<VisionMaskEffect>
{
    public override void Render(PostProcessRenderContext context)
    [Serializable]
    [PostProcess(typeof(VisionRenderer), PostProcessEvent.AfterStack, "Vision/VisionMask")]
    public sealed class VisionMaskEffect : PostProcessEffectSettings
    {
        var camera = context.camera;
        if((int)camera.depthTextureMode < 1)
        public FloatParameter Quality = new()
        {
            camera.depthTextureMode += 1;
        }
        var worldToCameraMatrix = context.camera.worldToCameraMatrix;
		var projectionMatrix = GL.GetGPUProjectionMatrix(context.camera.projectionMatrix, false);
		var viewProjectionMatrix = projectionMatrix * (worldToCameraMatrix);
        var fovTexture = RenderTexture.GetTemporary(Screen.width,Screen.height);

        var sheet = context.propertySheets.Get(Shader.Find("Vision/VisionMask"));

        sheet.properties.SetMatrix("_PlayerCameraInvViewProj", viewProjectionMatrix.inverse);

        context.command.BlitFullscreenTriangle(context.source, fovTexture, sheet, 0,false,null,true);


        sheet = context.propertySheets.Get(Shader.Find("Vision/VisionMaskBlur"));

        sheet.properties.SetFloat("_FovBlurQuality", settings.Quality);
        sheet.properties.SetFloat("_FovBlurDirections", settings.Directions);
        sheet.properties.SetVector("_FovBlurSize", settings.Size);
        sheet.properties.SetMatrix("_PlayerCameraInvViewProj", viewProjectionMatrix.inverse);
        sheet.properties.SetTexture("_FovTex", fovTexture);

        context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0,false,null,true);
            value = 5,
        };
        
        public FloatParameter Directions = new()
        {
            value = 25,
        };
        
        public Vector2Parameter Size = new()
        {
            value = new(5, 5),
        };

        RenderTexture.ReleaseTemporary(fovTexture);
        public override bool IsEnabledAndSupported(PostProcessRenderContext context)
        {
            return enabled.value && Quality.value > 0f && Directions.value > 0f && Size.value.x > 0f && Size.value.y > 0f;
        }
    }
}
