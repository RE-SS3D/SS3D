using System;
using System.Collections.Generic;
using System.Linq;
using SS3D.Interactions;
using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Systems.Storage.Containers;
using SS3D.Interactions.Interfaces;
using SS3D.Core;

namespace SS3D.Substances
{
    /// <summary>
    /// Stores substances
    /// </summary>
    public class SubstanceContainer : InteractionTargetNetworkBehaviour
    {

        /// <summary>
        /// A list of all substances in this container
        /// </summary>
        public List<SubstanceEntry> Substances
        {
            get => substances;
            private set => substances = value;
        }

        /// <summary>
        /// The total number of moles
        /// </summary>
        public float TotalMoles => Substances.Sum(x => x.Moles);

        /// <summary>
        /// The filled volume in ml
        /// </summary>
        public float CurrentVolume => currentVolume;

        [SyncVar]
        private float currentVolume;

        /// <summary>
        /// The remaining volume in milliliters that fit in this container
        /// </summary>
        public float RemainingVolume => volume - currentVolume;

        [SyncVar]
        private float volume;

        /// <summary>
        /// Multiplier to convert moles in this container to volume
        /// </summary>
        public float MolesToVolume
        {
            get
            {
                float val = 0;
                float total = TotalMoles;
                foreach (var entry in Substances)
                {
                    val += entry.Substance.MillilitersPerMole * (entry.Moles / total);
                }
                return val;
            }
        }

        /// <summary>
        /// The capacity of this container in milliliters
        /// </summary>
        public float Volume 
        {
            get => volume;
        }
        

        /// <summary>
        /// The temperature of the container
        /// </summary>
        public float Temperature;

        /// <summary>
        /// Is the container locked?
        /// </summary>
        [SyncVar]
        public bool Locked;
        
        public delegate void OnContentChanged(SubstanceContainer container);

        public event OnContentChanged ContentsChanged;

        [SerializeField] private List<SubstanceEntry> substances;

        private void Start()
        {
            if (Substances.Count < 1)
            {
                Substances = new List<SubstanceEntry>();
            }
            if (IsServer)
            {
                RecalculateAndSyncVolume();
            }
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
        /// Can this container hold an additional amount of milliliters
        /// </summary>
        /// <param name="milliliters">The amount of additional milliliters</param>
        /// <returns>If it fits the container</returns>
        public bool CanFitUnits(uint milliliters)
        {
            return RemainingVolume >= milliliters;
        }

        /// <summary>
        /// Adds an amount of an substance to the container.
        /// </summary>
        /// <param name="substance">The substance to add</param>
        /// <param name="moles">How many moles should be added</param>
        [Server]
        public void AddSubstance(Substance substance, float moles)
        {
           
            if (!CanTransfer())
                return;
            
            var remainingCapacity = RemainingVolume;
            var additionalVolume = moles * substance.MillilitersPerMole;
            if (additionalVolume > remainingCapacity)
            {
                moles = remainingCapacity / substance.MillilitersPerMole;
            }

            int index = Substances.FindIndex(x => x.Substance == substance);
            if (index == -1)
            {
                Substances.Add(new SubstanceEntry(substance, moles));
            }
            else
            {
                SubstanceEntry entry = Substances[index];
                entry.Moles += moles;
                Substances[index] = entry;
            }
            Debug.Log("substance is added. Remaining volume is " + RemainingVolume);

            RecalculateAndSyncVolume();
        }

        /// <summary>
        /// Checks if this container contains the desired substance
        /// </summary>
        /// <param name="substance">The desired substance</param>
        /// <param name="moles">The desired amount</param>
        [Server]
        public bool ContainsSubstance(Substance substance, float moles = 0.0001f)
        {
            return Substances.FirstOrDefault(x => x.Substance == substance).Moles >= moles;
        }

        /// <summary>
        /// Removes the specified amount of substance
        /// </summary>
        /// <param name="substance">The substance to remove</param>
        /// <param name="moles">The amount of substance</param>
        [Server]
        public void RemoveSubstance(Substance substance, float moles = float.MaxValue)
        {
            if (!CanTransfer()) 
                return;
            
            int index = IndexOfSubstance(substance);
            if (index < 0)
            {
                return;
            }

            SubstanceEntry entry = Substances[index];
            float newAmount = entry.Moles - moles;
            if (newAmount <= 0.000001)
            {
                Substances.RemoveAt(index);
            }
            else
            {
                entry.Moles = newAmount;
                Substances[index] = entry;
            }
            RecalculateAndSyncVolume();
        }

        /// <summary>
        /// Empties the container
        /// </summary>
        public void Empty()
        {
            Substances.Clear();
        }

        /// <summary>
        /// Removes moles from the container
        /// </summary>
        /// <param name="moles">The amount of moles</param>
        [Server]
        public void RemoveMoles(float moles)
        {
            var totalMoles = Substances.Sum(x => x.Moles);
            if (moles > totalMoles)
            {
                moles = totalMoles;
            }

            if (moles <= 0)
            {
                return;
            }

            for (var i = 0; i < Substances.Count; i++)
            {
                SubstanceEntry entry = Substances[i];
                entry.Moles -= entry.Moles / totalMoles * moles;
                if (entry.Moles <= 0.0001)
                {
                    Substances.RemoveAt(i);
                    i--;
                }
                else
                {
                    Substances[i] = entry;
                }
            }
        }

        /// <summary>
        /// Transfers moles to a different container
        /// </summary>
        /// <param name="other">The other container</param>
        /// <param name="moles">The moles to transfer</param>
        [Server]
        public void TransferMoles(SubstanceContainer other, float moles)
        {
            var totalMoles = Substances.Sum(x => x.Moles);
            if (moles > totalMoles)
            {
                moles = totalMoles;
            }

            float relativeMoles = moles / totalMoles;

            for (var i = 0; i < Substances.Count; i++)
            {
                SubstanceEntry entry = Substances[i];
                float entryMoles = entry.Moles * relativeMoles;
                entry.Moles -= entryMoles;
                other.AddSubstance(entry.Substance, entryMoles);
                if (entry.Moles <= 0.0000001)
                {
                    Substances.RemoveAt(i);
                    i--;
                }
                else
                {
                    Substances[i] = entry;
                }
            }

            RecalculateAndSyncVolume();
        }

        /// <summary>
        /// Transfers volume (ml) to a different container
        /// </summary>
        /// <param name="other">The other container</param>
        /// <param name="milliliters">How many milliliters to transfer</param>
        [Server]
        public void TransferVolume(SubstanceContainer other, float milliliters)
        {
            TransferMoles(other, milliliters / MolesToVolume);
        }

        /// <summary>
        /// Returns the index of an substance
        /// </summary>
        /// <param name="substance">The substance to look for</param>
        /// <returns>The index or -1 if it is not found</returns>
        [Server]
        public int IndexOfSubstance(Substance substance)
        {
            for (int i = 0; i < Substances.Count; i++)
            {
                if (Substances[i].Substance == substance)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Informs the system that the contents of this container have changed
        /// </summary>
        [Server]
        public void MarkDirty()
        {
            OnContentsChanged();
        }

        /// <summary>
        /// Recalculates the current and remaining volume of the container.
        /// Because these variables are SyncVar, they will propagate to the client.
        /// </summary>
        [Server]
        private void RecalculateAndSyncVolume()
        {
            currentVolume = Substances.Sum(x => x.Moles * x.Substance.MillilitersPerMole);
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
        public void ProcessContainer(SubstanceContainer container)
        {
            var registry = SystemLocator.Get<SubstancesSystem>();
            float temperature = container.Temperature;

            // Process recipes
            // TODO : Highly inefficient as most recipes won't be achievable given the substances in the substance container.
            // Instead, find a good data structure able to search through a small set of recipes, based on number of 
            // ingredients or even ingredient types.
            foreach (Recipe recipe in registry.Recipes)
            {
                // Check temperature limits
                if (temperature < recipe.MinimalTemperature || temperature > recipe.MaximalTemperature)
                {
                    continue;
                }

                // Gather the mole amount of every substance
                float[] moles = new float[recipe.Ingredients.Length];
                bool ingredientsPresent = true;
                for (var i = 0; i < recipe.Ingredients.Length; i++)
                {
                    ingredientsPresent = false;
                    foreach (var entry in container.Substances)
                    {
                        if (entry.Substance.Type == recipe.Ingredients[i].Type)
                        {
                            moles[i] = entry.Moles;
                            ingredientsPresent = true;
                            break;
                        }
                    }

                    if (!ingredientsPresent)
                    {
                        break;
                    }
                }

                // Substance missing
                if (!ingredientsPresent)
                {
                    continue;
                }

                // Calculate the maximum amount of ingredients
                float totalIngredients = recipe.Ingredients.Sum(x => x.RelativeAmount);
                float maxConversion = float.MaxValue;
                for (var i = 0; i < moles.Length; i++)
                {
                    float relativeAmount = recipe.Ingredients[i].RelativeAmount;
                    float part = relativeAmount / totalIngredients;
                    float maxProduced = moles[i] / part;
                    if (maxProduced < maxConversion)
                    {
                        maxConversion = maxProduced;
                    }
                }

                // Calculate relative volume of ingredients
                Substance[] ingredientSubstances = new Substance[moles.Length];
                float ingredientsToVolume = 0;
                for (var i = 0; i < moles.Length; i++)
                {
                    var component = recipe.Ingredients[i];
                    var substance = ingredientSubstances[i] = registry.FromType(component.Type);
                    ingredientsToVolume += substance.MillilitersPerMole * component.RelativeAmount;
                }
                ingredientsToVolume /= totalIngredients;

                // Calculate relative volume of results
                float totalResults = recipe.Results.Sum(x => x.RelativeAmount);
                Substance[] resultSubstances = new Substance[recipe.Results.Length];
                float resultsToVolume = 0;
                for (var i = 0; i < recipe.Results.Length; i++)
                {
                    var component = recipe.Results[i];
                    var substance = resultSubstances[i] = registry.FromType(component.Type);
                    resultsToVolume += substance.MillilitersPerMole * component.RelativeAmount;
                }
                resultsToVolume /= totalResults;

                // Limit the reaction to prevent spill
                // TODO: Spill container into surroundings
                float remainingVolume = container.RemainingVolume;
                if (maxConversion * totalResults * resultsToVolume - maxConversion * ingredientsToVolume > remainingVolume)
                {
                    maxConversion = remainingVolume / resultsToVolume / totalResults;
                }

                // Remove ingredients
                for (var i = 0; i < moles.Length; i++)
                {
                    container.RemoveSubstance(ingredientSubstances[i], maxConversion * (recipe.Ingredients[i].RelativeAmount / totalIngredients));
                }

                // Add results
                foreach (var component in recipe.Results)
                {
                    container.AddSubstance(registry.FromType(component.Type), maxConversion * (component.RelativeAmount / totalResults));
                }

                container.MarkDirty();
            }
        }
    }
}