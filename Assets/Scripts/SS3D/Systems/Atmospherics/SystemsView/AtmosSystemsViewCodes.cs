using SS3D.Systems.Tile;
using SS3D.Systems.Tile.MapEditor.SystemsView;

namespace SS3D.Systems.Atmospherics.SystemsView
{
    /// <summary>
    /// What the atmos view's domain codes mean (Documents/architecture/systems/map-editor-systems-views.md). These
    /// values go on the wire inside <c>SystemsViewSegment</c> and <c>SystemsViewBadge</c>: never renumber, only add.
    /// <list type="bullet">
    /// <item><b>Segment slot</b>: the line, Supply 0, Mixed 1, Waste 2, Surface 3. A manifold spans several lines, so it
    /// gives one segment per spanned line at <see cref="ManifoldSlotBase"/> + line (4 to 7); a pipe on the manifold's own
    /// tile keeps its plain slot, so the two never collide.</item>
    /// <item><b>Badge slot</b>: the device's <see cref="TileLayer"/> as a byte (pipe devices sit on PipeSurface, vents and
    /// scrubbers on theirs); layers hold one object per tile.</item>
    /// <item><b>Badge state</b>: bits 0-1 the switch (<see cref="Off"/>, <see cref="On"/>, <see cref="NoPower"/>; a valve
    /// that is <see cref="On"/> is open), bits 2-3 the mode, bit 4 <see cref="SideOnLeft"/> for filters and mixers.</item>
    /// <item><b>Badge networks</b>: a vent or scrubber has its line's network in ViewId and 0 in OtherViewId. A pump,
    /// valve, filter or mixer has the network at its back (where gas enters) in ViewId and the one at its front (where it
    /// leaves) in OtherViewId, with Facing pointing front; 0 means that port reaches no pipe. A filter's or mixer's side
    /// port network is in SideViewId.</item>
    /// </list>
    /// </summary>
    public static class AtmosSystemsViewCodes
    {
        public const byte Supply = 0;
        public const byte Mixed = 1;
        public const byte Waste = 2;
        public const byte Surface = 3;
        public const byte ManifoldSlotBase = 4;

        public const byte KindVent = 1;
        public const byte KindScrubber = 2;
        public const byte KindPump = 3;
        public const byte KindValve = 4;
        public const byte KindFilter = 5;
        public const byte KindMixer = 6;
        /// <summary>A scrubber set up as a gas chamber's intake (<c>ScrubberIntake</c>): it takes all air.</summary>
        public const byte KindIntake = 7;

        public const byte Off = 0;
        public const byte On = 1;
        public const byte NoPower = 2;
        public const byte SwitchMask = 0x03;

        public const int ModeShift = 2;
        public const byte ModeMask = 0x0C;
        /// <summary>Scrubber mode 0: scrubbing its filter gases at its normal rate.</summary>
        public const byte ModeScrub = 0;
        /// <summary>Scrubber mode 1: flat out on every gas (the air alarm's panic siphon).</summary>
        public const byte ModeSiphon = 1;
        /// <summary>Pump mode 0: pumps until its outlet reaches a target pressure.</summary>
        public const byte ModePressure = 0;
        /// <summary>Pump mode 1: moves a set volume per second.</summary>
        public const byte ModeVolume = 1;

        public const byte SideOnLeft = 0x10;

        public static byte State(byte switchState, byte mode = 0, bool sideOnLeft = false) =>
            (byte)((switchState & SwitchMask) | ((mode << ModeShift) & ModeMask) | (sideOnLeft ? SideOnLeft : 0));

        public static byte SwitchOf(byte state) => (byte)(state & SwitchMask);

        public static byte ModeOf(byte state) => (byte)((state & ModeMask) >> ModeShift);

        public static bool IsSideOnLeft(byte state) => (state & SideOnLeft) != 0;

        public static bool IsTwoNetwork(byte kind) => kind is KindPump or KindValve or KindFilter or KindMixer;

        /// <summary>The run slot of a pipe on <paramref name="line"/>; false for a layer that is not a gas line.</summary>
        public static bool TryGetLineSlot(TileLayer line, out byte slot)
        {
            switch (line)
            {
                case TileLayer.PipeLeft: slot = Supply; return true;
                case TileLayer.PipeMiddle: slot = Mixed; return true;
                case TileLayer.PipeRight: slot = Waste; return true;
                case TileLayer.PipeSurface: slot = Surface; return true;
                default: slot = 0; return false;
            }
        }

        /// <summary>The line of a run slot, and whether it is a manifold's.</summary>
        public static byte LineOf(byte slot, out bool manifold)
        {
            manifold = slot >= ManifoldSlotBase;
            return (byte)(manifold ? slot - ManifoldSlotBase : slot);
        }

        /// <summary>The hue a line's networks are shades of, matching the pipes' own paint: Supply blue, Waste red.</summary>
        public static byte LineHue(byte line) => line switch
        {
            Supply => SystemsViewPalette.Blue,
            Mixed => SystemsViewPalette.Green,
            Waste => SystemsViewPalette.Red,
            Surface => SystemsViewPalette.Amber,
            _ => SystemsViewPalette.AnyHue,
        };

        /// <summary>The line's name as the editor's palette and the design use it.</summary>
        public static string LineName(byte line) => line switch
        {
            Supply => "Supply",
            Mixed => "Mixed",
            Waste => "Waste",
            Surface => "Surface",
            _ => "Pipe",
        };

        public static string KindName(byte kind) => kind switch
        {
            KindVent => "Vent",
            KindScrubber => "Scrubber",
            KindPump => "Pump",
            KindValve => "Valve",
            KindFilter => "Filter",
            KindMixer => "Mixer",
            KindIntake => "Chamber intake",
            _ => "Device",
        };

        /// <summary>A device's ports, numbered as <c>SystemsViewBadge.ViewIdAt</c> numbers them; worded by <see cref="PortName"/>.</summary>
        public const byte PortBack = 0;
        public const byte PortFront = 1;
        public const byte PortSide = 2;

        /// <summary>
        /// Which side of a device a network is on, in plain words: the trace panel's side headings and its device rows.
        /// Gas enters a pump, filter or mixer at its back and leaves at its front; a filter's side lets the chosen
        /// gases out, a mixer's takes its second gas in. A valve has no direction, so its sides are back and front.
        /// </summary>
        public static string PortName(byte kind, byte port) => (kind, port) switch
        {
            (KindValve, PortBack) => "Back",
            (KindValve, PortFront) => "Front",
            (KindFilter, PortSide) => "Filtered out",
            (KindMixer, PortBack) => "Inlet 1",
            (KindMixer, PortSide) => "Inlet 2",
            (_, PortBack) => "Inlet",
            (_, PortFront) => "Outlet",
            _ => "Side",
        };

        /// <summary>A badge's state in plain words, the way the hover readout and legend say it.</summary>
        public static string StateWords(byte kind, byte state)
        {
            byte switchState = SwitchOf(state);
            if (kind == KindValve)
                return switchState == Off ? "closed" : switchState == NoPower ? "open, but no power" : "open";
            if (switchState == Off)
                return "off";
            if (switchState == NoPower)
                return "on, but no power";

            return kind switch
            {
                KindScrubber => ModeOf(state) == ModeSiphon ? "on, siphoning" : "on, scrubbing",
                KindIntake => ModeOf(state) == ModeSiphon ? "on, siphoning" : "on, drawing air",
                KindPump => ModeOf(state) == ModeVolume ? "on, set volume" : "on, to a pressure",
                _ => "on",
            };
        }
    }
}
