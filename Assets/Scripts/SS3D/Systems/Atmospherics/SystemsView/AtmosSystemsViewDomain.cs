using System.Linq;
using SS3D.Systems.Tile.MapEditor.SystemsView;
using SS3D.Systems.Tile.TileMapCreator;
using UnityEngine;

namespace SS3D.Systems.Atmospherics.SystemsView
{
    /// <summary>
    /// Registers the Atmos view's client half with the shared map editor code (<see cref="SystemsViewDomains"/>): its
    /// style, its legend lines in plain words (creative-mode §4 "Plain words") and Gas Pipes as its own layer. Runs on
    /// load in players and in the editor, so EditMode tests see it too.
    /// </summary>
    public static class AtmosSystemsViewDomain
    {
        public static readonly SystemsViewLegendEntry[] Legend = new SystemsViewLegendEntry[]
        {
            new("Each colour is one pipe network: a connected run on one line. Point at a pipe to see its line and name.",
                SystemsViewLegend.NetworkSwatches),
            new("Devices are squares edged in their network's colour. Pumps, valves, filters and mixers show the network on each side; grey means no pipe there."),
            new("Vent: marks point out, gas goes into the room. Scrubber: marks point in; a dot in the middle means it is siphoning."),
            new("Pump, filter, mixer: the arrow is the way gas moves; a filter's or mixer's side notch has its side network's colour. Valve: a bar along the pipe is open, across it is closed."),
            new("Marks: white running, grey switched off, amber switched on but no power.",
                AtmosSystemsViewStyle.MarkOn, AtmosSystemsViewStyle.MarkOff, AtmosSystemsViewStyle.MarkNoPower),
            new("Floors, objects and the pipes' own paint are dimmed so the networks show through."),
        }.Concat(SystemsViewProblems.Legend(AtmosProblemAnalyzer.Kinds)).ToArray();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        public static void Register() => SystemsViewDomains.Register(new SystemsViewDomain
        {
            View = SystemsViewKind.Atmos,
            Style = AtmosSystemsViewStyle.Instance,
            Legend = Legend,
            OwnLayer = TileLayerCategory.GasPipes,
            Problems = AtmosProblemAnalyzer.Kinds,
        });
    }
}
