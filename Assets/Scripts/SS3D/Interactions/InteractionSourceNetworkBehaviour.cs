﻿using System.Collections.Generic;
using System.Linq;
using FishNet.Object;
using SS3D.Core.Behaviours;
using SS3D.Engine.Interactions;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SS3D.Interactions
{
    /// <summary>
    /// A base class for interaction sources which use the mirror NetworkBehaviour
    /// </summary>
    public abstract class InteractionSourceNetworkBehaviour : NetworkedSpessBehaviour, IGameObjectProvider, IInteractionSource
    {
        protected bool SupportsMultipleInteractions { get; set; } = false;
        public IInteractionSource Source { get; set; }
        
        // Server only
        private readonly List<InteractionInstance> _interactions = new();
        // Client only
        private readonly List<ClientInteractionInstance> _clientInteractions = new();

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

        private void UpdateServerInteractions()
        {
            for (int index = 0; index < _interactions.Count; index++)
            {
                InteractionInstance instance = _interactions[index];
                if (instance.FirstTick)
                {
                    instance.FirstTick = false;

                    if (instance.Interaction.Start(instance.Event, instance.Reference))
                    {
                        continue;
                    }
                }
                else
                {
                    if (instance.Interaction.Update(instance.Event, instance.Reference))
                    {
                        continue;
                    }
                }

                _interactions.Remove(instance);
                index--;
            }
        }

        private void UpdateClientInteractions()
        {
            for (int index = 0; index < _clientInteractions.Count; index++)
            {
                ClientInteractionInstance instance = _clientInteractions[index];
                if (instance.FirstTick)
                {
                    instance.FirstTick = false;

                    if (instance.Interaction.ClientStart(instance.Event))
                    {
                        continue;
                    }
                }
                else
                {
                    if (instance.Interaction.ClientUpdate(instance.Event))
                    {
                        continue;
                    }
                }

                _clientInteractions.RemoveAt(index);
                index--;
            }
        }


        public virtual void GenerateInteractionsFromSource(IInteractionTarget[] targets, List<InteractionEntry> entries)
        {
            foreach (IInteractionSourceExtension extension in GetComponents<IInteractionSourceExtension>())
            {
                extension.GenerateInteractionsFromSource(targets, entries);
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

        public void ClientInteract(InteractionEvent interactionEvent, IInteraction interaction,
            InteractionReference reference)
        {
            IClientInteraction clientInteraction = interaction.CreateClient(interactionEvent);
            if (clientInteraction != null)
            {
                _clientInteractions.Add(new ClientInteractionInstance(clientInteraction,
                    interactionEvent, reference));
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

        public GameObject GameObject => gameObject;
    }
}