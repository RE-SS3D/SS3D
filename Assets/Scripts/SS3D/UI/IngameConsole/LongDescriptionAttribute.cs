using System;

namespace SS3D.UI.IngameConsole
{
    [AttributeUsage(AttributeTargets.Method)]
    public class LongDescriptionAttribute : Attribute
    {
        public string Description;
        public LongDescriptionAttribute(string description) => Description = description;
    }
}