using System;

namespace SS3D.Attributes
{
    /// <summary>
    /// When decorated with this attribute, a field requires that it be set
    /// to a non-null value prior to runtime. It should not be used on fields
    /// that you intend to initialize at runtime.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class NotNullAttribute : Attribute
    {

    }
}
