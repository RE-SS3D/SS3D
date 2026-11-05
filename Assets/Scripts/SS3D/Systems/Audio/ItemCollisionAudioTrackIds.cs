using System;

namespace SS3D.Systems.Audio
{
    /// <summary>What a loose object sounds like when it hits something.</summary>
    public enum ItemCollisionMaterial
    {
        Metal = 0,
        HeavyMetal = 1,
        Can = 2,
        Wood = 3,
        Plastic = 4,
        Glass = 5,
    }

    /// <summary>
    /// <c>AssetDatabases.Sounds</c> clip ids for items hitting things, under
    /// <c>Assets/Art/Sound/World/Collisions/</c>.
    /// </summary>
    public static class ItemCollisionAudioTrackIds
    {
        private static readonly string[] Metal =
        {
            "3a55012643054208a8b99befe77738cb", // DroppingMetalThin1
            "d2743382a6914c5b96d534e578f8622d", // DroppingMetalThin2
            "5f8866c0c6fc4314afa8f0dc57bbaa94", // DroppingMetalThin3
            "a9dda0057e414f8db5556f11cecc631a", // DroppingMetalThin4
            "b5d60d684f7244dbac9e964f83e93878", // DroppingMetalThin5
            "7939f5bbd4744aba9cd4e8f7955707b0", // DroppingMetalThin8
            "928d6957cd954fbd908217559bbad6e8", // DroppingMetalThin9
            "32bb0828a11f41f091683c97627fb0bd", // DroppingMetalThin10
        };

        private static readonly string[] HeavyMetal =
        {
            "644bda4b9b4f47a9bbff6abae80548cc", // DroppingMetalThick1
            "f8b4b0d4f03040c6bf8c2ddfd5398ce6", // DroppingMetalThick2
        };

        private static readonly string[] Can =
        {
            "1a34c805f84be3b4caac14d7de790607", // Can1
            "8d93fdf9dcada204a952e29213f0293f", // Can2
            "b315c4abdef36244fb211c5f643d411a", // Can3
            "f00c6b5a098e84b4b87d80d23b4f295a", // Can4
        };

        private static readonly string[] Wood =
        {
            "f0346c3978844e8790df79a8964fc698", // DroppingWood1
            "108bdc6ede9940bc816c0441a778da89", // DroppingWood2
            "8cd0f32382744a7a9162d79b0bf6d091", // DroppingWood3
        };

        private static readonly string[] Plastic =
        {
            "7aa66e48ea854ff284adc259b0a2a83c", // DroppingPlastic1
            "64532da349de40e48fb4be0dd6b9aff1", // DroppingPlastic2
            "355ca16899a9423bbee365d5738fb75d", // DroppingPlastic3
            "d6bd2fe29fdd47a685d89f0e66c78bc0", // DroppingPlastic4
            "eddafe311df64d78a5862f933fc55c3e", // DroppingPlastic5
        };

        private static readonly string[] Glass =
        {
            "b7599f4794edadf46bc16b7fe36b8920", // Tap
            "c2142bd97a284ee88e0f105e3a5335aa", // GlassSmack
        };

        public static string[] ClipsFor(ItemCollisionMaterial material)
        {
            return material switch
            {
                ItemCollisionMaterial.Metal => Metal,
                ItemCollisionMaterial.HeavyMetal => HeavyMetal,
                ItemCollisionMaterial.Can => Can,
                ItemCollisionMaterial.Wood => Wood,
                ItemCollisionMaterial.Plastic => Plastic,
                ItemCollisionMaterial.Glass => Glass,
                _ => Array.Empty<string>(),
            };
        }
    }
}
