using System;

namespace SS3D.Systems.IngameConsoleSystem
{
    [AttributeUsage(AttributeTargets.Method)]
    public class LongDescriptionAttribute : Attribute
    {
        public string Description;
        public LongDescriptionAttribute(string description) => Description = description;
    }
}