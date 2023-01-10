namespace SS3D.Systems.Health
{
    public class StaminaHelper
    {
        public static IStamina Create(float max = 10f, float rechargeRate = 0.05f)
        {
            return new Stamina(max, rechargeRate);
        }
    }
}