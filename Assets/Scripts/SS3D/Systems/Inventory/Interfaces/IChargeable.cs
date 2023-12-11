namespace SS3D.Systems.Inventory.Interfaces
{
    public interface IChargeable
    {
        void AddCharge(int amount);
        int GetChargeRate();
        float GetPowerPercentage();
    }
}