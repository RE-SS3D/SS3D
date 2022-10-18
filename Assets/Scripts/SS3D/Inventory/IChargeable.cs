namespace SS3D.Inventory
{
    public interface IChargeable
    {
        void AddCharge(int amount);
        int GetChargeRate();
        float GetPowerPercentage();
    }
}