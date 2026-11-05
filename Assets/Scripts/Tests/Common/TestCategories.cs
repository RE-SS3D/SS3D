namespace SS3D.Tests
{
    public static class TestCategories
    {
        public const string Prerequisites = "Prerequisites";
        public const string RequiresCompiledBuild = "RequiresCompiledBuild";
        public const string Lifecycle = "Lifecycle";

        /// <summary>Too slow for the agent loop; runs in the nightly (<c>validate.sh --nightly</c>).</summary>
        public const string Nightly = "Nightly";

        /// <summary>Tier 2 scenarios in the test room (ScenarioFixture). Run all with <c>validate.sh --steps scenarios:Scenarios</c>.</summary>
        public const string Scenarios = "Scenarios";

        public const string Medical = "Medical";

        public const string EvaGear = "EvaGear";

        public const string Atmos = "Atmos";

        public const string Access = "Access";

        /// <summary>Reach through walls, doors and wall mounts on real station geometry.</summary>
        public const string Reach = "Reach";

        /// <summary>Console tools for playtest wedges: the disk teleport and the ghost jump.</summary>
        public const string ConsoleTools = "ConsoleTools";

        /// <summary>Atmos reaching a body: critical collapse, ragdoll physics on deck and in space, reclaim while critical.</summary>
        public const string Environment = "Environment";

        /// <summary>Death, ghost and respawn.</summary>
        public const string Respawn = "Respawn";

        public const string Speech = "Speech";

        /// <summary>A Nuclear Emergency round in the test room: operative spawn, detonation and squad-out outcomes.</summary>
        public const string NukeOps = "NukeOps";

        /// <summary>Map Editor effects on the live networked world.</summary>
        public const string MapEditor = "MapEditor";
    }
}
