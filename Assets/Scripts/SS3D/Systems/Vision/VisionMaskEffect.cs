using System;
using UnityEngine.Rendering.PostProcessing;

namespace SS3D.Systems.Vision
{
    [Serializable]
    [PostProcess(typeof(VisionRenderer), PostProcessEvent.AfterStack, "Vision/VisionMask")]
    public sealed class VisionMaskEffect : PostProcessEffectSettings
    {
        public FloatParameter Quality = new()
        {
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

        public override bool IsEnabledAndSupported(PostProcessRenderContext context)
        {
            return enabled.value && Quality.value > 0f && Directions.value > 0f && Size.value.x > 0f && Size.value.y > 0f;
        }
    }
}
