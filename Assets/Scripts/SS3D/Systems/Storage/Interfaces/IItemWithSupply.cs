namespace SS3D.Storage.Interfaces
{
    public interface IItemWithSupply
    {
        void ChangeSupply(int amount);
        int GetSupplyDrainRate();
        float GetRemainingSupplyPercentage();
    }
}