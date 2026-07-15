using System;

namespace SS3D.Systems.Electricity
{
    [Flags]
    public enum ApcControlFlags : byte
    {
        None = 0,
        Lighting = 1,
        Equipment = 2,
        Environment = 4,
        All = Lighting | Equipment | Environment,
    }
}
