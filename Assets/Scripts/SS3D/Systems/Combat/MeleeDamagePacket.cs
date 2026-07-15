namespace SS3D.Systems.Combat
{
    public struct MeleeDamagePacket
    {
        public float Brute;
        public float Burn;

        public MeleeDamagePacket(float brute, float burn = 0f)
        {
            Brute = brute;
            Burn = burn;
        }
    }
}
