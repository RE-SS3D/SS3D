namespace SS3D.Systems.Health
{

    public class StaminaHelper
    {
        public static IStamina Create(StaminaType type, float max = 10f, float rechargeRate = 0.05f)
        {
            IStamina stamina = null;
            switch (type)
            {
                case StaminaType.Standard:
                    stamina = new Stamina(max, rechargeRate);
                    break;
                case StaminaType.Indefatigable:
                    stamina = new Indefatigable();
                    break;
                default:
                    break;
            }
            return stamina;
        }
    }

    public enum StaminaType
    {
        Standard = 0,
        Indefatigable = 1
    }
}