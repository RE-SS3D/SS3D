namespace SS3D.Systems.Combat
{
    /// <summary>
    /// <c>AssetDatabases.Sounds</c> clip ids for combat SFX. M4 gunshot/foley + surface impacts
    /// are SS14 CC-BY-SA-3.0 imports under <c>Assets/Art/Sound/Items/Weapons/Firearms/SS14/</c>
    /// Energy reports and impacts live under <c>Items/Weapons/Energy/SS14/</c>.
    /// See <c>Assets/Art/Sound/ATTRIBUTIONS.md</c> for per-clip sources and licenses.
    /// </summary>
    public static class CombatAudioTrackIds
    {
        public static string[] FireFor(RangedVisualStyle style) => style switch
        {
            RangedVisualStyle.Laser => LaserFire,
            RangedVisualStyle.Ion => IonFire,
            RangedVisualStyle.Taser => TaserFire,
            _ => GunFire,
        };

        public static readonly string[] LaserFire = { "7d8f7f4005a7408dac34329b02c5c252" };
        public static readonly string[] IonFire = { "f1ba387ba8d9495fbb8df0b2bfd99a0c" };
        public static readonly string[] TaserFire = { "46d1ebcf19d64594a03b8053351b1a3a" };
        public static readonly string[] EnergyHit = { "a6351208c22943d8a9e52e4a37bf9ced", "e48acffcbdc544129677ae54abbfd947" };
        public static readonly string[] EnergyFleshHit = { "32bf9af821e047a49907cd5d1b76d077", "bfcb361d68ba498eade01a32f2ff5037" };
        public const string EnergyReady = "e3295dd9bef344db9b30b15847417c41";

        /// <summary>SS14 rifle/rifle2 gunshot variety for M4 fire reports.</summary>
        public static readonly string[] GunFire =
        {
            "6394d11747aa4751a0f1521e5e2b1f5a", // Rifle.ogg
            "3cd42449f5454a3da5a29079d176b26f", // Rifle2.ogg
        };

        /// <summary>Dry-fire click when the mag is empty.</summary>
        public const string GunEmpty = "c09143590f9c417ab03c13e03f8fb274"; // Empty.ogg

        /// <summary>Magazine pulled free — reload start.</summary>
        public const string ReloadMagazineOut = "447209b4cce44b028574559d414842d5"; // LtRifleMagOut.ogg

        /// <summary>Magazine seated — reload complete.</summary>
        public const string ReloadMagazineIn = "e6450c81b14845dfa95f71fb51165b29"; // LtRifleMagIn.ogg

        /// <summary>Optional bolt/charge cue after mag-in.</summary>
        public const string ReloadCock = "4921f2ffd8dd4d15839e867289aca44b"; // LtRifleCock.ogg

        /// <summary>
        /// Non-living surface impact variety (bullet_hit + ric1–ric5). Living hits stay on
        /// <see cref="FleshHit"/>.
        /// </summary>
        public static readonly string[] SurfaceHit =
        {
            "8892d96a3d6d4eceb040c775cecb7a4c", // BulletHit.ogg
            "2525c1750669441fbac077db0ea02813", // Ric1.ogg
            "61148db9e5334c05a9ab81e68758edbb", // Ric2.ogg
            "c8679d06b97b4e4495c0fef32f2992e3", // Ric3.ogg
            "e9ab1f2103ba4014aebada3d61cc53a2", // Ric4.ogg
            "14dce3262c114eaf8dba37349a9734f7", // Ric5.ogg
        };

        /// <summary>Flesh impact variety (SS14 punch / genhit / weak_hit) — health damage path.</summary>
        public static readonly string[] FleshHit =
        {
            "6fab86354b054a54b8b4828f689970e4",
            "82588e82537544f59a9a0d330ff3d7ee",
            "36c53e237ddb426ea20825ce79dd1737",
            "d592855338f1467d897447bbfee4dc11",
        };
    }
}
