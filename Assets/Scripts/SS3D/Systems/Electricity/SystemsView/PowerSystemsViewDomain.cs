using SS3D.Systems.Tile.MapEditor.SystemsView;
using SS3D.Systems.Tile.TileMapCreator;
using UnityEngine;

namespace SS3D.Systems.Electricity.SystemsView
{
    /// <summary>
    /// Registers the Power view's client half with the shared map editor code (<see cref="SystemsViewDomains"/>): its
    /// style, its legend lines in plain words (creative-mode §4 "Plain words") and Cables as its own layer. Runs on load in
    /// players and in the editor, so EditMode tests see it too.
    /// </summary>
    public static class PowerSystemsViewDomain
    {
        public static readonly SystemsViewLegendEntry[] Legend =
        {
            new("Each colour is one cable circuit, joining generators, SMES units and APCs. Point at a cable to see its name.",
                SystemsViewLegend.NetworkSwatches),
            new("Rooms are tinted with the colour of the circuit their APC is on. Grey stripes: the room has no APC (or a broken one), so nothing in it gets power.",
                PowerSystemsViewStyle.Hatch),
            new("Grid devices are squares edged in their circuit's colour: a bolt is a generator, four cells a solar panel, a battery an SMES. White is running, grey is off."),
            new("APC: three lights for its lighting, equipment and environment breakers, lit when on. A red cross means it is broken."),
            new("Every machine has a dot: green powered, amber its channel switched off at the APC, red unpowered.",
                PowerSystemsViewStyle.DotPowered, PowerSystemsViewStyle.DotChannelOff, PowerSystemsViewStyle.DotUnpowered),
            new("Select a cable or device to trace its circuit, or a floor, machine or APC to trace its room."),
            new("Floors, objects and the cables' own paint are dimmed so the circuits show through."),
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        public static void Register() => SystemsViewDomains.Register(new SystemsViewDomain
        {
            View = SystemsViewKind.Power,
            Style = PowerSystemsViewStyle.Instance,
            Legend = Legend,
            OwnLayer = TileLayerCategory.Cables,
            RoomHatch = PowerSystemsViewStyle.Hatch,
        });
    }
}
