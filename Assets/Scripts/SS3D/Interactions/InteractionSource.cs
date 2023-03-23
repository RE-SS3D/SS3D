using System.Collections.Generic;
using System.Linq;
using FishNet.Object;
using SS3D.Core.Behaviours;
using SS3D.Interactions.Interfaces;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SS3D.Interactions
{
    /// <summary>
    /// A base class for interaction sources which use the mirror NetworkBehaviour
    /// </summary>
    public abstract class InteractionSource : NetworkActor, IGameObjectProvider, IInteractionSource
    {
        protected bool SupportsMultipleInteractions { get; set; }
        public IInteractionSource Source { get; set; }

        // Server only
        private readonly List<InteractionInstance> _interactions = new();

        // Client only
        private readonly List<ClientInteractionInstance> _clientInteractions = new();

        public new GameObject GameObject => base.GameObject;

        public virtual void Update()
        {
            if (IsClient)
            {
                UpdateClientInteractions();
            }
            if (IsServer)
            {
                UpdateServerInteractions();
            }
        }

        /// <summary>
        /// Updates interactions server-side
        /// </summary>
        private void UpdateServerInteractions()
        {
            for (int index = 0; index < _interactions.Count; index++)
            {
                InteractionInstance instance = _interactions[index];
                if (instance.FirstTick)
                {
                    instance.FirstTick = false;

                    // Runs the first tick of the interaction
                    if (instance.Interaction != null && instance.Interaction.Start(instance.Event, instance.Reference))
                    {
                        continue;
                    }
                }
                else
                {
                    // Continue running the interaction until it's done
                    if (instance.Interaction != null && instance.Interaction.Update(instance.Event, instance.Reference))
                    {
                        continue;
                    }
                }

                _interactions.Remove(instance);
                index--;
            }
        }

        /// <summary>
        /// Updates interactions client-side
        /// </summary>
        private void UpdateClientInteractions()
        {
            for (int index = 0; index < _clientInteractions.Count; index++)
            {
                ClientInteractionInstance instance = _clientInteractions[index];
                if (instance.FirstTick)
                {
                    instance.FirstTick = false;

                    if (instance.Interaction != null && instance.Interaction.ClientStart(instance.Event))
                    {
                        continue;
                    }
                }
                else
                {
                    if (instance.Interaction != null && instance.Interaction.ClientUpdate(instance.Event))
                    {
                        continue;
                    }
                }

                _clientInteractions.RemoveAt(index);
                index--;
            }
        }

        /// <summary>
        /// Creates the interactions from the source object
        /// </summary>
        public virtual void CreateSourceInteractions(IInteractionTarget[] targets, List<InteractionEntry> entries)
        {
            foreach (IInteractionSourceExtension extension in GetComponents<IInteractionSourceExtension>())
            {
                extension.GetSourceInteractions(targets, entries);
            }
        }

        public virtual bool CanInteractWithTarget(IInteractionTarget target)
        {
            return true;
        }

        public virtual bool CanExecuteInteraction(IInteraction interaction)
        {
            return true;
        }

        /// <summary>
        /// Runs the interaction
        /// </summary>
        [Server]
        public InteractionReference Interact(InteractionEvent interactionEvent, IInteraction interaction)
        {
            InteractionReference reference = new(Random.Range(1, int.MaxValue));
            if (!SupportsMultipleInteractions)
            {
                for (int index = _interactions.Count - 1; index >= 0; index--)
                {
                    InteractionInstance instance = _interactions[index];
                    CancelInteraction(instance.Reference);
                }
            }
            _interactions.Add(new InteractionInstance(interaction, interactionEvent, reference, Owner));

            return reference;
        }

        public InteractionInstance GetInstanceFromReference(InteractionReference reference)
        {
            return _interactions.FirstOrDefault(x => x.Reference.Equals(reference));
        }

        public void ClientInteract(InteractionEvent interactionEvent, IInteraction interaction, InteractionReference reference)
        {
            IClientInteraction clientInteraction = interaction.CreateClient(interactionEvent);
            if (clientInteraction != null)
            {
                _clientInteractions.Add(new ClientInteractionInstance(clientInteraction, interactionEvent, reference));
            }

        }

        [Server]
        public void CancelInteraction(InteractionReference reference)
        {
            InteractionInstance instance = _interactions.FirstOrDefault(i => Equals(reference, i.Reference));
            if (instance == null) return;

            RpcCancelInteraction(reference.Id);
            instance.Interaction.Cancel(instance.Event, reference);
            _interactions.Remove(instance);
        }

        [ObserversRpc]
        private void RpcCancelInteraction(int id)
        {
            ClientInteractionInstance instance = _clientInteractions.FirstOrDefault(i => i.Reference.Id == id);
            if (instance != null)
            {
                instance.Interaction.ClientCancel(instance.Event);
                _clientInteractions.Remove(instance);
            }
        }
    }
}