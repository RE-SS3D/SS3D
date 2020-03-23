using System.Collections.Generic;
using SS3D.Engine.Health;

namespace SS3D.Content.Systems.Player.Body
{
    //TODO: should be moved to some sort of config
    /// <summary>
    /// Static class to store minimum damage values before an effect can take place on a bodypart.
    /// EG: an arm needs to be damaged by at least 10 points, before it will start to bleed.
    /// This is just the minimal value. Once it is reached, there is still a chance the status won't happen right away.
    /// </summary>
    public static class DamageThresholds
    {
        public static readonly Dictionary<BodyPartStatuses, float> OrganicMinimumRequiredDamage = new Dictionary<BodyPartStatuses, float>
        {
            { BodyPartStatuses.Numb, 2f },
            { BodyPartStatuses.Bruised, 5f },
            { BodyPartStatuses.Bleeding, 10f },
            { BodyPartStatuses.Burned, 10f },
            { BodyPartStatuses.Blistered, 25f },
            { BodyPartStatuses.Crippled, 50f },
            { BodyPartStatuses.Severed, 90f }
        };
    }
}