using System;
using System.Collections.Generic;

namespace SS3D.Data.Persistence
{
    [Serializable]
    public class PersistenceEnvelope
    {
        /// <summary>
        /// 2: placed tile objects and items carry their prefab <c>id</c> beside the name. Version 1 saves load by name.
        /// </summary>
        public const int CurrentSchemaVersion = 2;

        public const string StationTemplateType = "station-template";

        public const string ServerMetaType = "server-meta";

        public int schemaVersion;

        public string envelopeType;

        public string createdAt;

        public string gameVersion;

        public List<PersistenceChunk> chunks = new();
    }
}
