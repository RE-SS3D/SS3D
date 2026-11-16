using FishNet;
using SS3D.Core;
using SS3D.Data.Generated;
using SS3D.Systems.Audio;
using UnityEngine;
using AudioType = SS3D.Systems.Audio.AudioType;

namespace SS3D.Systems.Furniture
{
    /// <summary>
    /// State machine behaviour to update colors upon change of state in the Airlock animator.
    /// This behaviour should go on state Open and Enter of the airlock state machine.
    /// </summary>
    public class AirlockStateMachine : StateMachineBehaviour
    {
        private const string Opening = "Opening";
        private const string Closing = "Closing";

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            AirLockOpener opener = animator.GetComponent<AirLockOpener>();
            if (opener != null)
            {
                opener.SetDoorLightColor(AirLockOpener.DoorLightIdleColor);
            }
        }

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            // Play at the door's world position with no parent — parenting to the NetworkObject
            // tied the pooled source to the sliding mesh and (with Doppler) warbled the open/close
            // attack. Positional one-shot is enough for a stationary door cycle.
            // Every peer runs this animator, but only the server may send the sound: on a client the
            // [Server] call fails and FishNet logs it with a native stack trace, a frame hitch on
            // every door cycle. The server's animator is unculled while the panels move, so it
            // reaches this state too and its RPC plays the sound for clients.
            Vector3 position = animator.transform.position;
            bool playSound = InstanceFinder.IsServer;
            AirLockOpener opener = animator.GetComponent<AirLockOpener>();

            if (stateInfo.IsName(Opening))
            {
                if (opener != null)
                {
                    opener.SetDoorLightColor(AirLockOpener.DoorLightOpeningColor);
                }

                if (playSound)
                {
                    SubSystems.Get<AudioSubSystem>().PlayAudioSource(AudioType.Sfx, Sounds.AirlockOpen, position, null);
                }
            }

            if (stateInfo.IsName(Closing))
            {
                if (opener != null)
                {
                    opener.SetDoorLightColor(AirLockOpener.DoorLightClosingColor);
                }

                if (playSound)
                {
                    SubSystems.Get<AudioSubSystem>().PlayAudioSource(AudioType.Sfx, Sounds.AirlockClose, position, null);
                }
            }
        }
    }
}
