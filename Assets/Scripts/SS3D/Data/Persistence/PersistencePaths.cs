namespace SS3D.Data.Persistence
{
    public static class PersistencePaths
    {
        public const string LegacyTilemaps = "/Tilemaps";
        public const string StationTemplates = "/StationTemplates";
        public const string ServerMeta = "/ServerMeta";
        public const string ServerMetaPermissions = ServerMeta + "/permissions";
        public const string RoundHistory = ServerMeta + "/round-history";
        public const string ServerInfo = ServerMeta + "/server-info";
        public const string ResetHealth = ServerMeta + "/reset-health";
        public const string BugReports = ServerMeta + "/bugs";
        /// <summary>Folder of JPEGs attached to dialog reports in <see cref="BugReports"/>.</summary>
        public const string BugScreenshots = ServerMeta + "/bug-screenshots";
    }
}
