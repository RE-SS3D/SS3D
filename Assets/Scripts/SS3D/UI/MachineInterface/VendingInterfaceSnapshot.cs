namespace SS3D.UI.MachineInterface
{
    public struct VendingInterfaceSnapshot
    {
        public const int MaxProducts = 12;

        public const int MaxTrayItems = 4;

        public const int MaxLogEntries = 4;

        public const byte NoVendingProduct = 255;

        public int MachineObjectId;

        public string InterfaceId;

        public string Title;

        public string Subtitle;

        public string ModelLabel;

        public string ConnectionStatus;

        public string StockedReadout;

        public bool PowerOk;

        public bool IdScanned;

        public bool Scanning;

        public byte VendingProductIndex;

        public byte ProductCount;

        public VendingProductSnapshot Product0;

        public VendingProductSnapshot Product1;

        public VendingProductSnapshot Product2;

        public VendingProductSnapshot Product3;

        public VendingProductSnapshot Product4;

        public VendingProductSnapshot Product5;

        public VendingProductSnapshot Product6;

        public VendingProductSnapshot Product7;

        public VendingProductSnapshot Product8;

        public VendingProductSnapshot Product9;

        public VendingProductSnapshot Product10;

        public VendingProductSnapshot Product11;

        public byte TrayItemCount;

        public VendingTrayItemSnapshot Tray0;

        public VendingTrayItemSnapshot Tray1;

        public VendingTrayItemSnapshot Tray2;

        public VendingTrayItemSnapshot Tray3;

        public byte LogEntryCount;

        public string Log0;

        public string Log1;

        public string Log2;

        public string Log3;
    }
}
