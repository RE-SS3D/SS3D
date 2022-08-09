using System;

/// <summary>
/// When decorated with this attribute, a Component requires that its GameObject
/// is on the specified layer.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class RequiredLayerAttribute : Attribute
{
    public readonly string layer;

    public RequiredLayerAttribute(string layer)
    {
        this.layer = layer;
    }
}
