namespace SS3D.Storage.Interfaces
{
    public interface IChargeable
    {
        void AddCharge(int amount);
        int GetChargeRate();
        float GetPowerPercentage();
    }
}