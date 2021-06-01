using System;

namespace SS3D.Engine.Health
{
    // Defines the status of a body part, should be renamed to BodyPartStatus
    [Flags]
    public enum BodyPartStatuses
    {
        Healthy = 0b_0000_0000,
        Numb = 0b_0000_0001,
        Bruised = 0b_0000_0010,
        Bleeding = 0b_0000_0100,
        Burned = 0b_0000_1000,
        Blistered = 0b_0001_0000,
        Crippled = 0b_0010_0000,
        Severed = 0b_0100_0000
    }
}