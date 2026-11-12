namespace SS3D.Systems.Furniture
{
    /// <summary>
    /// The nuclear device's clips, under <c>Assets/Art/Sound/Items/Weapons/Explosive/SS14/</c> and registered by
    /// <c>ExplosivePrefabSetup.RegisterNukeSounds</c>. Sources and licences: Sound ATTRIBUTIONS.
    /// </summary>
    public static class NukeAudioTrackIds
    {
        /// <summary>The disk slides into the slot (at the device).</summary>
        public const string DiskInsert = "a0598fa17bfcb815fad7408b6c60f9e0";
        /// <summary>Code-accepted beep as arming completes (at the device).</summary>
        public const string ArmBeep = "34d3c3a2410b47612ad0e6c6a2210c6c";
        /// <summary>Station-wide sting after the arm announcement's chime.</summary>
        public const string Armed = "499fddd0b2c1a9eb7851582af7212146";
        /// <summary>The device's countdown beep, faster near zero (at the device).</summary>
        public const string Tick = "ceabe1eeefec121739e2ad2ed67bfcb7";
        /// <summary>Station-wide alarm for the last ten seconds.</summary>
        public const string Alarm = "04ede92dcec9390a7a7ed55931e847e0";
        /// <summary>The wiring panel being unscrewed (at the device).</summary>
        public const string Panel = "ad29110d81908efdf8efa944c2965c2b";
        /// <summary>The wirecutters snipping as the defuse completes (at the device).</summary>
        public const string WireCut = "c71b390cba490a1af818076d0edd59f2";
        /// <summary>Wires cut: the device is disarmed (at the device).</summary>
        public const string Disarmed = "03651e6838974f8e7b66152682104269";

        public static readonly string[] All = { DiskInsert, ArmBeep, Armed, Tick, Alarm, Panel, WireCut, Disarmed };
    }
}
