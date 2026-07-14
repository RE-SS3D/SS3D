namespace SS3D.UI.MachineInterface
{
    public struct IdConsoleInterfaceSnapshot
    {
        public const int MaxLogEntries = 4;

        public int MachineObjectId;

        public string InterfaceId;

        public string Title;

        public string Subtitle;

        public string ModelLabel;

        public string PromptText;

        public bool EditorUnlocked;

        public bool HasTargetCard;

        public string TargetName;

        public string TargetJob;

        public ulong TargetAccessMask;

        public byte LogEntryCount;

        public string Log0;

        public string Log1;

        public string Log2;

        public string Log3;
    }
}
