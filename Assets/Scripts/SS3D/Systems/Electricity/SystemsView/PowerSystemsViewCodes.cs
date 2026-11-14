using SS3D.Systems.Tile;

namespace SS3D.Systems.Electricity.SystemsView
{
    /// <summary>
    /// What the Power view's domain codes mean (Documents/architecture/2026-11_map-editor-systems-views.md, Phase 3). These
    /// values go on the wire inside <c>SystemsViewSegment</c>, <c>SystemsViewBadge</c> and <c>SystemsViewRoom</c>: never
    /// renumber, only add.
    /// <list type="bullet">
    /// <item><b>Segment slot</b>: <see cref="CableSlot"/> for an HV cable tile (the Wire layer). A cable circuit is every
    /// cable run and grid device the electricity graph joins (<c>ElectricitySubSystem</c>'s circuits): one view id each.</item>
    /// <item><b>Badge slot</b>: the device's layer and facing, <see cref="DeviceSlot"/>, because wall mounts on opposite
    /// faces of one wall tile share a layer.</item>
    /// <item><b>Badge kinds</b>: grid devices (generator, solar panel, SMES or other storage, APC) and consumers (a light
    /// fixture, any other machine), each consumer a status dot.</item>
    /// <item><b>Badge state</b>: a consumer's <see cref="StatusMask"/> bits (<see cref="Unpowered"/>, <see cref="Powered"/>,
    /// <see cref="ChannelOff"/>) and its channel at <see cref="ChannelShift"/>; a generator's or solar panel's
    /// <see cref="Running"/> bit (producing now); an SMES's <see cref="Running"/> bit (output on); an APC's channel breakers
    /// as <c>ApcControlFlags</c> bits and <see cref="ApcBroken"/>.</item>
    /// <item><b>Badge networks</b>: ViewId is the device's circuit (for a consumer, its APC's circuit); 0 means on no
    /// circuit. OtherViewId is the room view id of an APC or a consumer (<see cref="RoomViewId"/>); 0 outside any room.</item>
    /// <item><b>Rooms</b>: each room is a network of its own, <see cref="RoomViewIdBase"/> + its area id, labelled with the
    /// room's name, so a room traces like a circuit. Room flags are <see cref="RoomHasApc"/> and
    /// <see cref="RoomApcBroken"/>, plus the shared <c>SystemsViewRoom.Hatched</c> for a room nothing powers.</item>
    /// </list>
    /// </summary>
    public static class PowerSystemsViewCodes
    {
        public const byte CableSlot = 0;

        public const byte KindGenerator = 1;
        public const byte KindSolar = 2;
        public const byte KindSmes = 3;
        public const byte KindApc = 4;
        public const byte KindLight = 5;
        public const byte KindConsumer = 6;

        public const byte Unpowered = 0;
        public const byte Powered = 1;
        public const byte ChannelOff = 2;
        public const byte StatusMask = 0x03;
        public const int ChannelShift = 2;
        public const byte ChannelMask = 0x0C;

        /// <summary>A generator or solar panel producing now; an SMES with its output on.</summary>
        public const byte Running = 0x01;

        /// <summary>An APC's breakers are its low three bits (<see cref="ApcControlFlags"/>); this one marks it smashed.</summary>
        public const byte ApcBroken = 0x08;
        public const byte ApcChannelsMask = 0x07;

        public const byte RoomHasApc = 0x01;
        public const byte RoomApcBroken = 0x02;

        /// <summary>
        /// Room view ids start here, far above any circuit's (circuit ids count up from 1 and never reach it), so the two
        /// never collide in the structure's networks or a trace.
        /// </summary>
        public const int RoomViewIdBase = 1 << 20;

        public static int RoomViewId(ushort areaId) => areaId == 0 ? 0 : RoomViewIdBase + areaId;

        public static bool IsRoomViewId(int viewId) => viewId > RoomViewIdBase && viewId <= RoomViewIdBase + ushort.MaxValue;

        public static byte DeviceSlot(TileLayer layer, Direction facing) => (byte)(((int)layer << 3) | ((int)facing & 7));

        public static TileLayer LayerOfSlot(byte slot) => (TileLayer)(slot >> 3);

        public static bool IsGrid(byte kind) => kind is KindGenerator or KindSolar or KindSmes or KindApc;

        public static bool IsConsumer(byte kind) => kind is KindLight or KindConsumer;

        public static byte ConsumerState(byte status, PowerChannel channel) =>
            (byte)((status & StatusMask) | (((int)channel << ChannelShift) & ChannelMask));

        public static byte StatusOf(byte state) => (byte)(state & StatusMask);

        public static PowerChannel ChannelOf(byte state) => (PowerChannel)((state & ChannelMask) >> ChannelShift);

        public static byte ApcState(ApcControlFlags channels, bool broken) =>
            (byte)(((byte)channels & ApcChannelsMask) | (broken ? ApcBroken : 0));

        public static ApcControlFlags ApcChannelsOf(byte state) => (ApcControlFlags)(state & ApcChannelsMask);

        public static string KindName(byte kind) => kind switch
        {
            KindGenerator => "Generator",
            KindSolar => "Solar panel",
            KindSmes => "SMES",
            KindApc => "APC",
            KindLight => "Light",
            KindConsumer => "Machine",
            _ => "Device",
        };

        public static string ChannelName(PowerChannel channel) => channel switch
        {
            PowerChannel.Lighting => "Lighting",
            PowerChannel.Equipment => "Equipment",
            PowerChannel.Environment => "Environment",
            _ => "Channel",
        };

        /// <summary>A consumer's status in plain words, as the hover, the legend and the trace panel say it.</summary>
        public static string StatusWords(byte state) => StatusOf(state) switch
        {
            Powered => "powered",
            ChannelOff => ChannelName(ChannelOf(state)).ToLowerInvariant() + " channel off",
            _ => "unpowered",
        };

        /// <summary>A badge's state in plain words, by kind.</summary>
        public static string StateWords(byte kind, byte state)
        {
            if (IsConsumer(kind))
                return StatusWords(state);

            return kind switch
            {
                KindApc when (state & ApcBroken) != 0 => "broken",
                KindApc => ChannelWords(ApcChannelsOf(state)),
                KindSmes => (state & Running) != 0 ? "output on" : "output off",
                _ => (state & Running) != 0 ? "producing" : "not producing",
            };
        }

        /// <summary>"lighting on, equipment on, environment off".</summary>
        public static string ChannelWords(ApcControlFlags channels) =>
            $"lighting {OnOff(channels, ApcControlFlags.Lighting)}, equipment {OnOff(channels, ApcControlFlags.Equipment)}, "
            + $"environment {OnOff(channels, ApcControlFlags.Environment)}";

        public static ApcControlFlags FlagOf(PowerChannel channel) => channel switch
        {
            PowerChannel.Lighting => ApcControlFlags.Lighting,
            PowerChannel.Equipment => ApcControlFlags.Equipment,
            PowerChannel.Environment => ApcControlFlags.Environment,
            _ => ApcControlFlags.None,
        };

        private static string OnOff(ApcControlFlags channels, ApcControlFlags channel) => (channels & channel) != 0 ? "on" : "off";
    }
}
