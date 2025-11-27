namespace SS3D.Systems.Inventory.Containers
{
    public interface IOpenable
    {
        public bool IsOpen { get; }

        public void SetOpen(bool openState);
    }
}
