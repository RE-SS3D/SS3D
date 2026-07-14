using System;
using System.Collections.Generic;

namespace SS3D.Data.Persistence
{
    [Serializable]
    public class PersistenceEnvelope
    {
        public const int CurrentSchemaVersion = 1;

        public const string StationTemplateType = "station-template";

        public int schemaVersion;

        public string envelopeType;

        public string createdAt;

        public string gameVersion;

        public List<PersistenceChunk> chunks = new();
    }
}
