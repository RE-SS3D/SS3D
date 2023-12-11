namespace SS3D.Systems.Health
{
    /// <summary>
    /// Factory used to create stamina. (AKA As Gatorade™ factory)
    /// </summary>
    public abstract class StaminaFactory
    {
        public static IStamina Create(float max = 10f, float rechargeRate = 0.05f)
        {
            return new Stamina(max, rechargeRate);
        }
    }
}