using System;
using System.Collections.Generic;
using FishNet;
using FishNet.Broadcast;
using FishNet.Connection;
using SS3D.Core;
using SS3D.Systems.Entities;

namespace SS3D.Systems.Comms
{
    /// <summary>A station-wide sound with no banner, e.g. the nuclear device's final alarm.</summary>
    [Serializable]
    public struct StationCueMessage : IBroadcast
    {
        /// <summary><c>AssetDatabases.Sounds</c> id.</summary>
        public string SoundId;
    }

    /// <summary>
    /// Station cues: a sound every player with a body hears wherever they are, like an announcement chime
    /// but without text. A world object's own RPCs only reach its observers (the nuke uses a grid condition),
    /// so anything the whole station must hear goes through here.
    /// </summary>
    public partial class CommsSubSystem
    {
        /// <summary>Client: a station cue arrived; <c>CommsFeedController</c> plays it.</summary>
        public event Action<string> StationCueReceived;

        private void RegisterStationCueBroadcast() =>
            InstanceFinder.ClientManager.RegisterBroadcast<StationCueMessage>(OnClientReceiveStationCue);

        private void UnregisterStationCueBroadcast()
        {
            if (InstanceFinder.ClientManager != null)
                InstanceFinder.ClientManager.UnregisterBroadcast<StationCueMessage>(OnClientReceiveStationCue);
        }

        /// <summary>Server: plays <paramref name="soundId"/> for every player with a body, as announcements reach.</summary>
        public void SendStationCue(string soundId)
        {
            if (!IsServer || string.IsNullOrEmpty(soundId) || !SubSystems.TryGet(out EntitySubSystem entities))
                return;

            HashSet<NetworkConnection> crew = new();
            foreach (NetworkConnection client in InstanceFinder.ServerManager.Clients.Values)
            {
                if (entities.TryGetSpawnedEntity(client, out _))
                    crew.Add(client);
            }

            if (crew.Count > 0)
                InstanceFinder.ServerManager.Broadcast(crew, new StationCueMessage { SoundId = soundId });
        }

        private void OnClientReceiveStationCue(StationCueMessage message) => StationCueReceived?.Invoke(message.SoundId);
    }
}
