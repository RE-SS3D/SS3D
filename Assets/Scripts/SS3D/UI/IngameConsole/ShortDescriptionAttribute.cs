using System;

namespace SS3D.UI.IngameConsole
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ShortDescriptionAttribute : Attribute
    {
        public string Descrition;
        public ShortDescriptionAttribute(string descrition) => Descrition = descrition;
    }
}