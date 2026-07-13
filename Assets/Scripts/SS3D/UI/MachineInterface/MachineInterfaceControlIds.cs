namespace SS3D.UI.MachineInterface
{
    public static class MachineInterfaceControlIds
    {
        public static class Apc
        {
            public const byte Lighting = 0;

            public const byte Equipment = 1;

            public const byte Environment = 2;
        }

        public static class Smes
        {
            public const byte Input = 0;

            public const byte Output = 1;
        }

        public static class Vending
        {
            public const byte SelectProduct = 0;

            public const byte TakeTrayItem = 1;

            public const byte ReadId = 2;
        }

        public static class Atmos
        {
            public const byte ReadId = 0;

            public const byte Power = 1;

            public const byte FlowRate = 2;

            public const byte TargetPressure = 3;

            public const byte PresetMode = 4;

            public const byte SelectDevice = 5;

            public const byte CloseDevice = 6;

            public const byte DeviceFilter = 7;
        }
    }
}
