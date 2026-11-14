using System;
using System.Collections.Generic;

namespace SS3D.Data.Persistence
{
    [Serializable]
    public class PersistenceEnvelope
    {
        /// <summary>
        /// 2: placed tile objects and items carry their prefab <c>id</c> beside the name. Version 1 saves load by name.
        /// 3: placed objects and items carry their authored <c>setup</c> (device settings, locks, container contents).
        /// Older saves need no migration: no setup entry means the prefab default.
        /// </summary>
        public const int CurrentSchemaVersion = 3;

        /// <summary>
        /// A template saved by a newer build: it still loads, but settings that build added may be missing here.
        /// </summary>
        public static bool IsNewerThanThisBuild(int schemaVersion) => schemaVersion > CurrentSchemaVersion;

        public const string StationTemplateType = "station-template";

        public const string ServerMetaType = "server-meta";

        public int schemaVersion;

        public string envelopeType;

        public string createdAt;

        public string gameVersion;

        public List<PersistenceChunk> chunks = new();
    }
}
