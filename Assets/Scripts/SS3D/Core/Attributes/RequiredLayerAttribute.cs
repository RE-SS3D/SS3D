using System;

namespace SS3D.Core.Attributes
{
    /// <summary>
    /// When decorated with this attribute, a Component requires that its GameObject
    /// is on the specified layer.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class RequiredLayerAttribute : Attribute
    {
        public readonly string Layer;

        public RequiredLayerAttribute(string layer)
        {
            this.Layer = layer;
        }
    }
}
