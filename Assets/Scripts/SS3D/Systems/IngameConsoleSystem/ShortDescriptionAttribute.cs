using System;

namespace SS3D.Systems.IngameConsoleSystem
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ShortDescriptionAttribute : Attribute
    {
        public readonly string Description;
        public ShortDescriptionAttribute(string description) => Description = description;
    }
}