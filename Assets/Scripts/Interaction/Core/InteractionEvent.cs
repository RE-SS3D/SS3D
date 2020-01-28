using System;
using UnityEngine;

namespace Interaction.Core
{
    /// <summary>
    /// This struct contains all the information about an interaction event.
    /// </summary>
    [Serializable]
    public struct InteractionEvent
    {
        /// <summary>
        /// The kind of event. For example, "pickup", "shoot", "explode", "admin_kill".
        /// </summary>
        public InteractionKind kind;
        /// <summary>
        /// The `GameObject` that sent the event.
        /// </summary>
        public GameObject sender;
        /// <summary>
        /// The player 'GameObject' that sent the event
        /// </summary>
        public GameObject player;
        /// <summary>
        /// (Optional) The position that the event happened at.
        /// </summary>
        public Vector3 worldPosition;
        /// <summary>
        /// (Optional) The normal of the position that the event happened at.
        /// </summary>
        public Vector3 worldNormal;
        /// <summary>
        /// The receiver that any further events should be forwarded to.<br/>
        /// Used for example when using an item in your hand, that then needs to send an event to the object it is being used on
        /// </summary>
        public InteractionReceiver forwardTo;
        /// <summary>
        /// Any receiver that need to finish before this event can be handled.<br/>
        /// Events are handled concurrently and this is a mechanism to ensure that everything works.
        /// </summary>
        public InteractionReceiver waitFor;
        /// <summary>
        /// A predicate that should be true while a continuous interaction should run.<br/>
        /// The interaction will be stopped when the predicate return false.
        /// </summary>
        public Predicate<InteractionEvent> runWhile;

        internal Action onFail;
        internal Action onSuccess;
            

        /// <summary>
        /// Default constructor that should cover most use cases.
        /// </summary>
        /// <param name="kind">The kind of event being triggered.</param>
        /// <param name="sender">The `GameObject` that sends the event.</param>
        /// <param name="player">The player `GameObject` that triggered the event.</param>
        public InteractionEvent(InteractionKind kind, GameObject sender, GameObject player)
        {
            this.kind = kind;
            this.sender = sender;
            this.player = player;
            this.worldPosition = Vector3.zero;
            this.worldNormal = Vector3.zero;
            this.forwardTo = null;
            this.waitFor = null;
            this.runWhile = null;
            this.onFail = null;
            this.onSuccess = null;
        }

        /// <summary>
        /// Builder method for adding `worldPosition`
        /// </summary>
        /// <param name="value">The position that the event happened at</param>
        /// <returns>This `InteractionEvent`</returns>
        public InteractionEvent WorldPosition(Vector3 value)
        {
            worldPosition = value;
            return this;
        }
        
        /// <summary>
        /// Builder method for adding `worldNormal`
        /// </summary>
        /// <param name="value">The normal at the position that the event happened at</param>
        /// <returns>This `InteractionEvent`</returns>
        public InteractionEvent WorldNormal(Vector3 value)
        {
            worldNormal = value;
            return this;
        }
        
        /// <summary>>
        /// Builder method for adding `forwardTo`
        /// </summary>
        /// <param name="value">Any receiver that need to finish before this event can be handled</param>
        /// <returns>This `InteractionEvent`</returns>
        public InteractionEvent ForwardTo(InteractionReceiver value)
        {
            forwardTo = value;
            return this;
        }

        /// <summary>>
        /// Builder method for adding `waitFor`
        /// </summary>
        /// <param name="value">The receiver that any further events should be forwarded to</param>
        /// <returns>This `InteractionEvent`</returns>
        public InteractionEvent WaitFor(InteractionReceiver value)
        {
            waitFor = value;
            return this;
        }

        /// <summary>>
        /// Builder method for adding `runWhile`
        /// </summary>
        /// <param name="value">A predicate that should be true while a continuous interaction should run</param>
        /// <returns>This `InteractionEvent`</returns>
        public InteractionEvent RunWhile(Predicate<InteractionEvent> value)
        {
            runWhile = value;
            return this;
        }

        public override string ToString()
        {
            return $"{kind.name} from {sender.name} from player {player}";
        }
    }
}