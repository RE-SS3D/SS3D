namespace SS3D.Systems.Combat
{
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
