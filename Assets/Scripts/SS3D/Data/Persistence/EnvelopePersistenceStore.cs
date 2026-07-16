using SS3D.Data.Management;
using SS3D.Logging;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SS3D.Data.Persistence
{
    public sealed class EnvelopePersistenceStore : IPersistenceStore
    {
        public bool TrySave(string path, PersistenceEnvelope envelope, bool overwrite)
        {
            return LocalStorage.SaveObject(path, envelope, overwrite);
        }

        public bool TryLoad(string path, out PersistenceEnvelope envelope)
        {
            envelope = LocalStorage.LoadObject<PersistenceEnvelope>(path);
            return envelope != null && !string.IsNullOrEmpty(envelope.envelopeType);
        }

        public IReadOnlyList<string> List(string directory)
        {
            List<string> fileNames = LocalStorage.GetAllObjectsNameInFolder(directory);
            return fileNames
                .Select(name => name.EndsWith(".json") ? name[..^5] : name)
                .ToList();
        }

        public static bool IsEnvelopeJson(string json)
        {
            return !string.IsNullOrEmpty(json) && json.Contains("\"envelopeType\"");
        }

        public static string ReadRawJson(string path)
        {
            if (!LocalStorage.TryReadRaw(path, out string json))
            {
                Log.Warning(typeof(EnvelopePersistenceStore), $"Persistence file not found at {path}");
                return null;
            }

            return json;
        }
    }
}
