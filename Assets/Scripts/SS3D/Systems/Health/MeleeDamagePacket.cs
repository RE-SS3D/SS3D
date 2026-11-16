namespace SS3D.Systems.Health
{
    /// <summary>
    /// Damage payload applied to a body zone (melee and future combat sources).
    /// </summary>
    public struct MeleeDamagePacket
    {
        public float Brute;
        public float Burn;
        public bool CanSever;

        public MeleeDamagePacket(float brute, float burn = 0f, bool canSever = false)
        {
            Brute = brute;
            Burn = burn;
            CanSever = canSever;
        }
    }
}
