namespace SS3D.UI.MachineInterface
{
    public sealed class VendingProductViewData
    {
        public int Index { get; set; }

        public string Name { get; set; } = string.Empty;

        public int Stock { get; set; }

        public bool RequiresId { get; set; }

        public bool Locked { get; set; }

        public bool CanSelect { get; set; }
    }
}
