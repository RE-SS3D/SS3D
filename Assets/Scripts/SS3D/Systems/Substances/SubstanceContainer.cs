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
using System.Collections.ObjectModel;
using SS3D.Logging;
using static UnityEngine.EventSystems.EventTrigger;

namespace SS3D.Substances
{
    /// <summary>
    /// Stores substances, allows transfer between different containers. 
    /// </summary>
    public class SubstanceContainer : InteractionTargetNetworkBehaviour
    {
        /// <summary>
        /// A list of initial substances in this container
        /// </summary>
        public List<SubstanceEntry> InitialSubstances;

        [SyncVar]
        private float _currentVolume;

        [SyncVar]
        private float _volume;

        private float _temperature;

        /// <summary>
        /// Is the container locked?
        /// </summary>
        [SyncVar]
        private bool _locked;

        /// <summary>
        /// A list of all substances in this container
        /// </summary>
        private readonly List<SubstanceEntry> _substances = new List<SubstanceEntry>();

        /// <summary>
        /// Live read-only view of <see cref="_substances"/>. Cached once — <see cref="List{T}.AsReadOnly"/> allocates.
        /// </summary>
        private ReadOnlyCollection<SubstanceEntry> _substancesView;

        /// <summary>
        /// The temperature of the container
        /// </summary>
        public float Temperature => _temperature;

        public ReadOnlyCollection<SubstanceEntry> Substances =>
            _substancesView ??= _substances.AsReadOnly();

        /// <summary>
        /// The capacity of this container in milliliters
        /// </summary>
        public float Volume => _volume;

        public bool Locked => _locked;

        public delegate void OnContentChanged(SubstanceContainer container);

        public event OnContentChanged ContentsChanged;

        /// <summary>
        /// The remaining volume in milliliters that fit in this container
        /// </summary>
        public float RemainingVolume => _volume - _currentVolume;

        public bool IsEmpty => CurrentVolume == 0f;

        /// <summary>
        /// The filled volume in ml
        /// </summary>
        public float CurrentVolume => _currentVolume;

        public bool CanTransfer => !_locked;

        /// <summary>
        /// The total number of millimoles
        /// </summary>
        public float TotalMilliMoles
        {
            get
            {
                float total = 0f;
                for (int i = 0; i < _substances.Count; i++)
                    total += _substances[i].MilliMoles;
                return total;
            }
        }

        [SyncVar]
        private bool _initialised = false;

        protected override void OnStart()
        {
            foreach(var substance in InitialSubstances)
            {
                AddSubstance(substance.Substance, substance.MilliMoles);
            }
            if (IsServer) _initialised = true;
        }

        public void Init(float volume, bool locked)
        {
            if (_initialised)
            {
                Log.Warning(this, "already initialised, returning");
                return;
            }
            _volume = volume;
            _locked = locked;
            _initialised = true;
        }

        [Server]
        public void ChangeVolume(float newVolume)
        {
            // TODO : might need to spill the excess of volume already present
            _volume = newVolume;
        }

        /// <summary>
        /// Multiplier to convert moles in this container to volume
        /// </summary>
        public float MilliMolesToVolume()
        {
            float val = 0;
            float total = TotalMilliMoles;
            if (total <= 0f)
                return 0f;

            for (int i = 0; i < _substances.Count; i++)
            {
                SubstanceEntry entry = _substances[i];
                val += entry.Substance.MillilitersPerMilliMoles * (entry.MilliMoles / total);
            }
            return val;
        }

        /// <summary>
        /// Removes the specified amount of substance
        /// </summary>
        /// <param name="substance">The substance to remove</param>
        /// <param name="moles">The amount of substance</param>
        [Server]
        public void RemoveSubstance(Substance substance, float millimoles = float.MaxValue)
        {
            if (!CanTransfer)
                return;

            int index = IndexOfSubstance(substance);
            if (index < 0)
            {
                return;
            }

            SubstanceEntry entry = _substances[index];
            float newAmount = entry.MilliMoles - millimoles;
            if (newAmount <= 0.000001)
            {
                _substances.RemoveAt(index);
            }
            else
            {
                entry.MilliMoles = newAmount;
                _substances[index] = entry;
            }
            RecalculateAndSyncVolume();
        }

        private void RecalculateAndSyncVolume()
        {
            float volume = 0f;
            for (int i = 0; i < _substances.Count; i++)
            {
                SubstanceEntry entry = _substances[i];
                volume += entry.MilliMoles * entry.Substance.MillilitersPerMilliMoles;
            }

            _currentVolume = volume;
        }

        /// <summary>
        /// Transfers volume (ml) to a different container
        /// </summary>
        /// <param name="other">The other container</param>
        /// <param name="milliliters">How many milliliters to transfer</param>
        public void TransferVolume(SubstanceContainer other, float milliliters)
        {
            TransferMoles(other, milliliters / MilliMolesToVolume());
        }

        /// <summary>
        /// Informs the system that the contents of this container have changed
        /// </summary>
        [Server]
        public void SetDirty()
        {
            OnContentsChanged();
        }

        [Server]
        protected virtual void OnContentsChanged()
        {
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
        /// Transfers moles to a different container
        /// </summary>
        /// <param name="other">The other container</param>
        /// <param name="moles">The moles to transfer</param>
        public void TransferMoles(SubstanceContainer other, float milliMoles)
        {
            // Only transfer what's left.
            float totalMilliMoles = TotalMilliMoles;
            if (milliMoles > totalMilliMoles)
            {
                milliMoles = totalMilliMoles;
            }

            // TODO : Only transfer what can be transferred ?

            float relativeMoles = milliMoles / totalMilliMoles;

            for (int i = 0; i < _substances.Count; i++)
            {
                SubstanceEntry entry = _substances[i];
                float entryMoles = entry.MilliMoles * relativeMoles;
                entry.MilliMoles -= entryMoles;
                other.AddSubstance(entry.Substance, entryMoles);
                if (entry.MilliMoles <= 0.0000001)
                {
                    _substances.RemoveAt(i);
                    i--;
                }
                else
                {
                    _substances[i] = entry;
                }
            }
            RecalculateAndSyncVolume();
        }

        /// <summary>
        /// Returns the index of an substance
        /// </summary>
        /// <param name="substance">The substance to look for</param>
        /// <returns>The index or -1 if it is not found</returns>
        public int IndexOfSubstance(Substance substance)
        {
            for (int i = 0; i < _substances.Count; i++)
            {
                if (_substances[i].Substance == substance)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Adds an amount of an substance to the container.
        /// </summary>
        /// <param name="substance">The substance to add</param>
        /// <param name="moles">How many moles should be added</param>
        [Server]
        public void AddSubstance(Substance substance, float millimoles)
        {

            if (!CanTransfer)
                return;

            float remainingCapacity = RemainingVolume;
            float additionalVolume = millimoles * substance.MillilitersPerMilliMoles;
            if (additionalVolume > remainingCapacity)
            {
                millimoles = remainingCapacity / substance.MillilitersPerMilliMoles;
            }

            int index = _substances.FindIndex(x => x.Substance == substance);
            if (index == -1)
            {
                _substances.Add(new SubstanceEntry(substance, millimoles));
            }
            else
            {
                SubstanceEntry entry = _substances[index];
                entry.MilliMoles += millimoles;
                _substances[index] = entry;
            }
            RecalculateAndSyncVolume();
            //Debug.Log("substance is added. Remaining volume is " + RemainingVolume);
        }

        /// <summary>
        /// Removes moles from the container. For each substance present in the container,
        /// remove a proportionnal amount of mole. If it contains 50 moles of alcohol and 100 moles of water,
        /// removing 15 moles will remove 5 moles of alcohol, and 10 of water.
        /// </summary>
        /// <param name="moles">The amount of moles</param>
        public void RemoveMoles(float milliMoles)
        {
            float totalMoles = TotalMilliMoles;
            if (milliMoles > totalMoles)
            {
                milliMoles = totalMoles;
            }

            if (milliMoles <= 0)
            {
                return;
            }

            for (int i = 0; i < _substances.Count; i++)
            {
                SubstanceEntry entry = _substances[i];
                entry.MilliMoles -= entry.MilliMoles / totalMoles * milliMoles;
                if (entry.MilliMoles <= 0.0001)
                {
                    _substances.RemoveAt(i);
                    i--;
                }
                else
                {
                    _substances[i] = entry;
                }
            }
            RecalculateAndSyncVolume();
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
        /// Empties the container
        /// </summary>
        public void Empty()
        {
            _substances.Clear();
        }

        /// <summary>
        /// Checks if this container contains the desired substance.
        /// </summary>
        /// <param name="substance">The desired substance</param>
        /// <param name="moles">The desired amount</param>
        [Server]
        public bool ContainsSubstance(Substance substance, float moles = 0.0001f)
        {
            int index = IndexOfSubstance(substance);
            return index >= 0 && _substances[index].MilliMoles >= moles;
        }


        /// <summary>
        /// Processes substances in a container
        /// </summary>
        public static void ProcessContainer(SubstanceContainer container)
        {
            var registry = SubSystems.Get<SubstancesSubSystem>();
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
                for (int i = 0; i < recipe.Ingredients.Length; i++)
                {
                    ingredientsPresent = false;
                    foreach (SubstanceEntry entry in container.Substances)
                    {
                        if (entry.Substance.Type == recipe.Ingredients[i].Type)
                        {
                            moles[i] = entry.MilliMoles;
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
                for (int i = 0; i < moles.Length; i++)
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
                for (int i = 0; i < moles.Length; i++)
                {
                    var component = recipe.Ingredients[i];
                    var substance = ingredientSubstances[i] = registry.FromType(component.Type);
                    ingredientsToVolume += substance.MillilitersPerMilliMoles * component.RelativeAmount;
                }
                ingredientsToVolume /= totalIngredients;

                // Calculate relative volume of results
                float totalResults = recipe.Results.Sum(x => x.RelativeAmount);
                Substance[] resultSubstances = new Substance[recipe.Results.Length];
                float resultsToVolume = 0;
                for (int i = 0; i < recipe.Results.Length; i++)
                {
                    var component = recipe.Results[i];
                    var substance = resultSubstances[i] = registry.FromType(component.Type);
                    resultsToVolume += substance.MillilitersPerMilliMoles * component.RelativeAmount;
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
                for (int i = 0; i < moles.Length; i++)
                {
                    container.RemoveSubstance(ingredientSubstances[i], maxConversion * (recipe.Ingredients[i].RelativeAmount / totalIngredients));
                }

                // Add results
                foreach (var component in recipe.Results)
                {
                    container.AddSubstance(registry.FromType(component.Type), maxConversion * (component.RelativeAmount / totalResults));
                }
            }

            container.SetDirty();
        }

        public float GetSubstanceQuantity(Substance substance)
        {
            int index = IndexOfSubstance(substance);
            return index < 0 ? 0f : _substances[index].MilliMoles;
        }

        public float GetSubstanceVolume(Substance substance)
        {
            int index = IndexOfSubstance(substance);
            if (index < 0)
                return 0f;

            SubstanceEntry entry = _substances[index];
            return entry.Substance.MillilitersPerMilliMoles * entry.MilliMoles;
        }
    }
}