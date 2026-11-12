using System.Collections.Generic;

namespace SS3D.Systems.ActionMessages
{
    /// <summary>
    /// Action line keys in the Examine string table with their English templates. Arguments: {0} the
    /// actor's name, {1} the target's, {2} the item's, each formatted by the viewer
    /// (examine feedback Decision 10). The text never travels on the wire, only the key.
    /// </summary>
    public static class ActionMessageKeys
    {
        public const string ExamineCue = "action.examine_cue";
        public const string TakeBlind = "action.take_blind";
        public const string TakeOpen = "action.take_open";
        public const string LightSwitch = "action.light_switch";
        public const string PlantCharge = "action.plant_charge";
        public const string NukeLoadDisk = "action.nuke_load_disk";
        public const string NukeArm = "action.nuke_arm";
        public const string NukeOpenPanel = "action.nuke_open_panel";
        public const string NukeCutWires = "action.nuke_cut_wires";

        private static readonly Dictionary<string, string> English = new()
        {
            { ExamineCue, "{0} looks you over." },
            { TakeBlind, "{0} is reaching into your pockets!" },
            { TakeOpen, "{0} is trying to take your {2}!" },
            { LightSwitch, "{0} flicks the light switch." },
            { PlantCharge, "{0} plants something on the {1}." },
            { NukeLoadDisk, "{0} slides a disk into the {1}." },
            { NukeArm, "{0} starts arming the {1}!" },
            { NukeOpenPanel, "{0} starts unscrewing the {1}'s wiring panel." },
            { NukeCutWires, "{0} starts cutting the {1}'s wires." },
        };

        /// <summary>Lines aimed at the viewer that should outrank routine ones (action-messages.md §5).</summary>
        public static bool IsHostile(string key) => key == TakeBlind || key == TakeOpen;

        public static string EnglishFor(string key) => key != null && English.TryGetValue(key, out string text) ? text : null;

        public static IEnumerable<(string Key, string English)> All
        {
            get
            {
                foreach (KeyValuePair<string, string> pair in English)
                {
                    yield return (pair.Key, pair.Value);
                }
            }
        }
    }
}
