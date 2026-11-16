using UnityEngine;

namespace SS3D.Systems.Health
{
    public static class HealthLayers
    {
        public const string BodyPartsLayerName = "BodyParts";

        public static int BodyPartsLayer => LayerMask.NameToLayer(BodyPartsLayerName);

        public static int BodyPartsMask => LayerMask.GetMask(BodyPartsLayerName);
    }
}
