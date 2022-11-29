using System;

namespace SS3D.Systems.IngameConsoleSystem
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ShortDescriptionAttribute : Attribute
    {
        public string Descrition;
        public ShortDescriptionAttribute(string descrition) => Descrition = descrition;
    }
}