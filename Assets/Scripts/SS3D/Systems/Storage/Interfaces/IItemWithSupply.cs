namespace SS3D.Systems.Storage.Interfaces
{
    public interface IItemWithSupply
    {
        void ChangeSupply(int amount);
        int GetSupplyDrainRate();
        float GetRemainingSupplyPercentage();
    }
}