namespace SS3D.Systems.Stamina
{
    public abstract class StaminaFactory
    {
        public const float DefaultMax = 10f;
        public const float DefaultRechargeRate = 0.08f;

        public static IStamina Create(float max = DefaultMax, float rechargeRate = DefaultRechargeRate)
        {
            return new Stamina(max, rechargeRate);
        }
    }
}
