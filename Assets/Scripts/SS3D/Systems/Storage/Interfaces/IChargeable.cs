namespace SS3D.Systems.Storage.Interfaces
{
    public interface IChargeable
    {
        void AddCharge(int amount);
        int GetChargeRate();
        float GetPowerPercentage();
    }
}