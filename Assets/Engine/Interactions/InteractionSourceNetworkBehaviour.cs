using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SS3D.Engine.Interactions
{
    /// <summary>
    /// A base class for interaction sources which use the mirror NetworkBehaviour
    /// </summary>
    public abstract class InteractionSourceNetworkBehaviour : NetworkBehaviour, IGameObjectProvider, IInteractionSource
    {
        protected bool SupportsMultipleInteractions { get; set; } = false;
        public IInteractionSource Parent { get; set; }

        private class ClientInteractionInstance
        {
            public ClientInteractionInstance(IClientInteraction interaction, InteractionEvent interactionEvent, InteractionReference reference)
            {
                Interaction = interaction;
                Event = interactionEvent;
                Reference = reference;
            }

            public IClientInteraction Interaction { get; }
            public InteractionEvent Event { get; }
            public InteractionReference Reference { get; }
            public bool FirstTick { get; set; } = true;
        }
        
        // Server only
        private readonly List<InteractionInstance> interactions = new List<InteractionInstance>();
        // Client only
        private readonly List<ClientInteractionInstance> clientInteractions = new List<ClientInteractionInstance>();

        public virtual void Update()
        {
            if (isClient)
            {
                // Update client interactions
                for (int index = 0; index < clientInteractions.Count; index++)
                {
                    ClientInteractionInstance instance = clientInteractions[index];
                    if (instance.FirstTick)
                    {
                        instance.FirstTick = false;
                        try
                        {
                            if (!instance.Interaction.ClientStart(instance.Event))
                            {
                                clientInteractions.RemoveAt(index);
                                index--;
                            }
                        }
                        catch (Exception)
                        {
                            clientInteractions.RemoveAt(index);
                            throw;
                        }
                        
                    }
                    else
                    {
                        if (!instance.Interaction.ClientUpdate(instance.Event))
                        {
                            clientInteractions.RemoveAt(index);
                            index--;
                        }
                    }
                }
            }
            if (isServer)
            {
                // Update server interactions
                for (int index = 0; index < interactions.Count; index++)
                {
                    InteractionInstance instance = interactions[index];
                    if (instance.FirstTick)
                    {
                        instance.FirstTick = false;
                        try
                        {
                            if (!instance.Interaction.Start(instance.Event, instance.Reference))
                            {
                                interactions.Remove(instance);
                                index--;
                            }
                        }
                        catch (Exception)
                        {
                            interactions.Remove(instance);
                            throw;
                        }
                        
                    }
                    else
                    {
                        if (!instance.Interaction.Update(instance.Event, instance.Reference))
                        {
                            interactions.Remove(instance);
                            index--;
                        }
                    }
                }
            }
        }
        

        public virtual void CreateInteractions(IInteractionTarget[] targets, List<InteractionEntry> interactions)
        {
            foreach (var extension in GetComponents<IInteractionSourceExtension>())
            {
                extension.CreateInteractions(targets, interactions);
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
            
            InteractionReference reference = new InteractionReference(Random.Range(1, Int32.MaxValue));
            if (!SupportsMultipleInteractions)
            {
                for (int index = interactions.Count - 1; index >= 0; index--)
                {
                    InteractionInstance instance = interactions[index];
                    CancelInteraction(instance.Reference);
                }
            }
            interactions.Add(new InteractionInstance(interaction, interactionEvent, reference, currentSender));

            return reference;
        }

        public InteractionInstance GetInstanceFromReference(InteractionReference reference)
        {
            return interactions.FirstOrDefault(x => x.Reference.Equals(reference));
        }

        public void ClientInteract(InteractionEvent interactionEvent, IInteraction interaction,
            InteractionReference reference)
        {
            IClientInteraction clientInteraction = interaction.CreateClient(interactionEvent);
            if (clientInteraction != null)
            {
                clientInteractions.Add(new ClientInteractionInstance(clientInteraction,
                    interactionEvent, reference));
            }
            
        }

        [Server]
        public void CancelInteraction(InteractionReference reference)
        {
            InteractionInstance instance = interactions.FirstOrDefault(i => Equals(reference, i.Reference));
            if (instance == null) return;
            
            RpcCancelInteraction(reference.Id);
            instance.Interaction.Cancel(instance.Event, reference);
            interactions.Remove(instance);
        }

        [ClientRpc]
        private void RpcCancelInteraction(int id)
        {
            ClientInteractionInstance instance = clientInteractions.FirstOrDefault(i => i.Reference.Id == id);
            if (instance != null)
            {
                instance.Interaction.ClientCancel(instance.Event);
                clientInteractions.Remove(instance);
            }
        }

        public GameObject GameObject => gameObject;
    }
}