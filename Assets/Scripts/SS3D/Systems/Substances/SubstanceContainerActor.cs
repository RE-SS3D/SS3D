using System;
using System.Collections.Generic;
using System.Linq;
using SS3D.Interactions;
using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Systems.Inventory.Containers;
using SS3D.Interactions.Interfaces;
using SS3D.Core;

namespace SS3D.Substances
{
    /// <summary>
    /// Stores substances
    /// </summary>
    public class SubstanceContainerActor : InteractionTargetNetworkBehaviour
    {


        [SyncVar]
        public float currentVolume;

        [SyncVar]
        public float volume;

        public SubstanceContainer substanceContainer;

        /// <summary>
        /// A list of all substances in this container
        /// </summary>
        [SerializeField]
        public List<SubstanceEntry> InitialSubstances;

        /// <summary>
        /// The remaining volume in milliliters that fit in this container
        /// </summary>
        public float RemainingVolume => substanceContainer.RemainingVolume;

        /// <summary>
        /// The filled volume in ml
        /// </summary>
        public float CurrentVolume => substanceContainer.CurrentVolume;

        /// <summary>
        /// The temperature of the container
        /// </summary>
        public float Temperature => substanceContainer.Temperature;

        public float TotalMoles => substanceContainer.TotalMoles;

        public List<SubstanceEntry> Substances => substanceContainer.Substances;


        /// <summary>
        /// The capacity of this container in milliliters
        /// </summary>
        public float Volume
        {
            get => substanceContainer.Volume;
        }




        /// <summary>
        /// Is the container locked?
        /// </summary>
        [SyncVar]
        public bool Locked;
        
        public delegate void OnContentChanged(SubstanceContainerActor container);

        public event OnContentChanged ContentsChanged;

        private void Start()
        {
            substanceContainer = new SubstanceContainer(volume, InitialSubstances, this);
        }

        public bool IsEmpty()
        {
            return currentVolume == 0f;
        }

        public bool CanTransfer()
        {
            return !Locked;
        }

        /// <summary>
        /// Adds an amount of an substance to the container.
        /// </summary>
        /// <param name="substance">The substance to add</param>
        /// <param name="moles">How many moles should be added</param>
        [Server]
        public void AddSubstance(Substance substance, float moles)
        {
            substanceContainer.AddSubstance(substance, moles);
        }

        /// <summary>
        /// Checks if this container contains the desired substance
        /// </summary>
        /// <param name="substance">The desired substance</param>
        /// <param name="moles">The desired amount</param>
        [Server]
        public bool ContainsSubstance(Substance substance, float moles = 0.0001f)
        {
            return substanceContainer.ContainsSubstance(substance, moles);
        }

        /// <summary>
        /// Removes the specified amount of substance
        /// </summary>
        /// <param name="substance">The substance to remove</param>
        /// <param name="moles">The amount of substance</param>
        [Server]
        public void RemoveSubstance(Substance substance, float moles = float.MaxValue)
        {
            substanceContainer.RemoveSubstance(substance, moles);
        }

        /// <summary>
        /// Empties the container
        /// </summary>
        public void Empty()
        {
            substanceContainer.Empty();
        }

        /// <summary>
        /// Removes moles from the container
        /// </summary>
        /// <param name="moles">The amount of moles</param>
        [Server]
        public void RemoveMoles(float moles)
        {
            substanceContainer.RemoveMoles(moles);
        }

        /// <summary>
        /// Transfers moles to a different container
        /// </summary>
        /// <param name="other">The other container</param>
        /// <param name="moles">The moles to transfer</param>
        [Server]
        public void TransferMoles(SubstanceContainerActor other, float moles)
        {
            substanceContainer.TransferMoles(other.substanceContainer, moles);
        }

        /// <summary>
        /// Transfers volume (ml) to a different container
        /// </summary>
        /// <param name="other">The other container</param>
        /// <param name="milliliters">How many milliliters to transfer</param>
        [Server]
        public void TransferVolume(SubstanceContainerActor other, float milliliters)
        {
            substanceContainer.TransferVolume(other.substanceContainer, milliliters);
        }

        /// <summary>
        /// Returns the index of an substance
        /// </summary>
        /// <param name="substance">The substance to look for</param>
        /// <returns>The index or -1 if it is not found</returns>
        [Server]
        public int IndexOfSubstance(Substance substance)
        {
            return substanceContainer.IndexOfSubstance(substance);
        }

        /// <summary>
        /// Informs the system that the contents of this container have changed
        /// </summary>
        [Server]
        public void MarkDirty()
        {
            OnContentsChanged();
        }

        [Server]
        protected virtual void OnContentsChanged()
        {
            //SystemLocator.Get<SubstancesSystem>().ProcessContainer(this);
            ContentsChanged?.Invoke(this);
        }

        public override IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            return new IInteraction[]
            {
                new TransferSubstanceInteraction()
            };
        }

        /// <summary>
        /// Processes substances in a container
        /// </summary>
        public void ProcessContainer(SubstanceContainerActor container)
        {
            substanceContainer.ProcessContainer(container.substanceContainer);
            container.MarkDirty(); 
        }
    }
}