namespace SS3D.Systems.StructuralDamage
{
    /// <summary>
    /// <c>AssetDatabases.Sounds</c> clip ids for structural impacts, breaks and blasts. SS14 imports
    /// live under <c>Assets/Art/Sound/World/Destruction/</c>; see <c>Assets/Art/Sound/ATTRIBUTIONS.md</c>.
    /// </summary>
    public static class StructuralAudioTrackIds
    {
        /// <summary>Melee or console hit on metal. Bullets use <see cref="SS3D.Systems.Combat.CombatAudioTrackIds.SurfaceHit"/>.</summary>
        public static readonly string[] MetalHit =
        {
            "3cd350866d2be174584b9acd88c39bc6", // MetalHit1
            "8614cc03edfb35140ba2913d0d94ac4f", // MetalHit2
            "02bde19c53af4ddea8d833976eca87a5", // MediumHitMetalThin1
            "99b6fe945b3d41a687620ea39eb32905", // MediumHitMetalThin2
            "02e4d9a5316845c3be50ed367526ac13", // MediumHitMetalThin3
            "d2fff2d63bc34d3bb7407aebe5cc1b2c", // MediumHitMetalThin4
        };

        /// <summary>Any hit on wood.</summary>
        public static readonly string[] WoodHit =
        {
            "d1c05c1962e46d24bb24a817c61de9e9", // WoodHit1
            "d0b0c2f8856a493d831028f0ad3d7494", // MediumHitWood1
            "56126974866f48b99e877c2dce55c732", // LightHitWood1
            "1da3ce1baa4a46e880201d11cbf8bdb8", // LightHitWood2
        };

        /// <summary>Any hit on glass.</summary>
        public static readonly string[] GlassHit =
        {
            "c2142bd97a284ee88e0f105e3a5335aa", // GlassSmack
            "e419f0cf22a6495ba65f56984409fc81", // GlassCrack1
            "fadcc24c2f0c4c19ae7f947e88e2a43c", // GlassCrack2
        };

        /// <summary>Metal reaching Cracked.</summary>
        public static readonly string[] MetalStrain =
        {
            "c66e7198be8142ee85ed6c3036e46f31", // MetalScrape1
            "49853a177a1a49f797716bd4c961b130", // MetalScrape2
            "68ac856568434c6a9f97656677a5f3cb", // MetalScrape3
        };

        /// <summary>Wood reaching Cracked. Played slowed down, so a short destroy clip reads as a split.</summary>
        public static readonly string[] WoodStrain =
        {
            "d9ab3676b04e41bab6fb14f27f944b70", // WoodDestroy2
        };

        /// <summary>Glass reaching Cracked.</summary>
        public static readonly string[] GlassStrain =
        {
            "8e623d69668e4e8cbc64709b0a02e404", // GlassCrack3
            "2cf5001237114264acdf0aa8816e2b88", // GlassCrack4
        };

        /// <summary>Metal destroyed. CC-BY-NC-4.0, so a commercial release has to replace these.</summary>
        public static readonly string[] MetalBreak =
        {
            "7162c785d7954978a7acf3c43e483959", // MetalBreak1
            "76296b876e8040b1ab9b35a3d2ccdfdf", // MetalBreak2
            "c6958ba94d35463d9266f18bf05665ff", // MetalBreak3
            "6a13eabff94f47d7afba5a7519ff236a", // MetalBreak4
            "8c7e9c7c888641b6bd922ff8021c768e", // MetalBreak5
            "ae9f642727c24480a881c1954ca3262e", // MetalCrunch
        };

        /// <summary>Wood destroyed.</summary>
        public static readonly string[] WoodBreak =
        {
            "acd027ef3a7648988dd0131584febc69", // WoodDestroy1
            "d0d3707dffd54598850cff1a1ed816ce", // WoodDestroy3
            "82a17b81a61047e8823ec0f4a2b45c47", // WoodDestroyHeavy1
        };

        /// <summary>Glass destroyed.</summary>
        public static readonly string[] GlassBreak =
        {
            "a7191e2464b64ea883457da2eb42380b", // WindowShatter1
            "d1063b244ff841eca979fad35804f753", // WindowShatter2
            "4ef1d3a941de438eac17c7fc1c6a97c2", // WindowShatter3
            "28cb8f97272843269110c1bc4b819d6e", // GlassBreak1
            "88ff826ca50044918696e4b6327515e0", // GlassBreak2
            "45637e6ca20c48af93c1ae2792d5307b", // GlassBreak3
            "2bb5d8565d0143088aced9013024f9c1", // GlassBreak4
        };

        /// <summary>Steel wreckage landing.</summary>
        public static readonly string[] WreckageThud =
        {
            "b1d98477f9954f8eb513a936a7b9f130", // MetalThud1
            "162e1f57358040ce8f4c850d1ae74bb5", // MetalThud2
            "ad24268af0904c68851b7d487c65fdae", // MetalThud3
        };

        public const string BlastSmall = "a583ecd799d040728b40947e8e4287d1"; // MinibombExplosion

        /// <summary>The boom, heard from close by.</summary>
        public static readonly string[] BlastNear =
        {
            "e5c9231c45bb4b72855d681a0f9460ff", // Explosion1
            "87cae4686f7544f0860cee2540184b44", // Explosion2
            "086672233efd437d804c137a3a8b91e8", // Explosion3
        };

        /// <summary>The rumble heard from far away instead of a quieter boom.</summary>
        public const string BlastFar = "bccf635606624ca2b67e997098b07098"; // ExplosionFar
    }
}
