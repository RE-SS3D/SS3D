using FishNet.Object;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Entities;
using SS3D.Systems.Health;
using SS3D.Systems.Entities.Humanoid;
using UnityEngine;

namespace SS3D.Systems.Comms
{
    /// <summary>
    /// Attached to a player Entity. Client -> server request to speak, and the per-speaker RPC
    /// surface that broadcasts the resulting line to nearby observers. This is a component on the
    /// speaking Entity itself (rather than logic living solely on CommsSubSystem) because a FishNet
    /// RPC must be declared on a NetworkBehaviour that's part of the observed object graph - the
    /// singleton CommsSubSystem can't fire an RPC scoped to a specific Entity's observers.
    /// </summary>
    [RequireComponent(typeof(Entity))]
    public class LocalSpeechEmitter : NetworkActor
    {
        private Entity _entity;

        /// <summary>Health gates in-character speech on the server, including radio and announcements.</summary>
        public bool CanSpeak => (!TryGetComponent(out Ragdoll ragdoll) || ragdoll.Presentation != BodyPresentationState.Dead)
            && (!TryGetComponent(out HumanHealthController health)
                || (health.Snapshot.State == HealthState.Healthy
                    && health.Snapshot.IsConscious && !health.Snapshot.IsCardiacArrest));

        protected override void OnAwake()
        {
            base.OnAwake();

            _entity = GetComponent<Entity>();
        }

        /// <summary>
        /// Client -> server: ask to speak locally as this entity.
        /// </summary>
        [ServerRpc]
        public void CmdSpeak(string text, SpeechMode mode)
        {
            SubSystems.Get<CommsSubSystem>().HandleSpeakRequest(this, text, mode);
        }

        /// <summary>
        /// Client -> server: send on a non-positional radio channel.
        /// </summary>
        [ServerRpc]
        public void CmdSendRadio(string channelId, string text)
        {
            SubSystems.Get<CommsSubSystem>().HandleRadioRequest(this, channelId, text);
        }

        /// <summary>
        /// Client -> server: station announcement (ALL-STATION banner).
        /// </summary>
        [ServerRpc]
        public void CmdSendAnnouncement(string text)
        {
            SubSystems.Get<CommsSubSystem>().HandleAnnouncementRequest(this, text);
        }

        /// <summary>
        /// Server-only: broadcasts a validated speech event to this entity's current observers.
        /// </summary>
        public void ServerBroadcastSpeech(SpeechEvent speechEvent)
        {
            if (!IsServer)
            {
                return;
            }

            RpcReceiveSpeech(speechEvent);
        }

        [ObserversRpc]
        private void RpcReceiveSpeech(SpeechEvent speechEvent)
        {
            SubSystems.Get<CommsSubSystem>().NotifyLocalSpeechReceived(_entity, speechEvent);
        }
    }
}
