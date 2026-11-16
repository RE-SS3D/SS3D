using SS3D.Data;
using SS3D.Data.Generated;
using UnityEngine;

namespace SS3D.Interactions
{
    /// <summary>
    /// Resolves interaction menu sprites from the InteractionIcons asset database.
    /// </summary>
    public static class InteractionIconLookup
    {
        public static Sprite Discard => Get(InteractionIcons.Discard);
        public static Sprite Dispose => Get(InteractionIcons.Dispose);
        public static Sprite Examine => Get(InteractionIcons.Examine);
        public static Sprite Honk => Get(InteractionIcons.Honk);
        public static Sprite MachineInterface => Get(InteractionIcons.Power);
        public static Sprite Music => Get(InteractionIcons.Honk);
        public static Sprite Open => Get(InteractionIcons.Open);
        public static Sprite Power => Get(InteractionIcons.Power);
        public static Sprite Recycle => Get(InteractionIcons.Recycle);
        public static Sprite Search => Get(InteractionIcons.Search);
        public static Sprite Take => Get(InteractionIcons.Take);
        public static Sprite Transfer => Get(InteractionIcons.Take);

        public static Sprite Get(string iconId)
        {
            return Assets.Get<Sprite>(AssetDatabases.InteractionIcons, iconId);
        }
    }
}
