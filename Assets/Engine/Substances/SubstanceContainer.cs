using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Engine.Substances
{
    /// <summary>
    /// Stores substances
    /// </summary>
    public class SubstanceContainer : MonoBehaviour
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
        public float CurrentVolume => Substances.Sum(x => x.Moles * x.Substance.MillilitersPerMole);
        /// <summary>
        /// The remaining volume in milliliters that fit in this container
        /// </summary>
        public float RemainingVolume => Volume - CurrentVolume;
        /// <summary>
        /// The capacity of this container in milliliters
        /// </summary>
        public float Volume;
        
        public delegate void OnContentChanged(SubstanceContainer container);
        public event OnContentChanged ContentsChanged;

        [SerializeField] private List<SubstanceEntry> substances;

        private void Awake()
        {
            Substances = new List<SubstanceEntry>();
        }

        public bool IsEmpty()
        {
            return Substances.Count < 1;
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
        public void AddSubstance(Substance substance, float moles)
        {
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
            OnContentsChanged();
        }
        
        /// <summary>
        /// Checks if this container contains the desired substance
        /// </summary>
        /// <param name="substance">The desired substance</param>
        /// <param name="moles">The desired amount</param>
        public bool ContainsSubstance(Substance substance, float moles = 0.0001f)
        {
            return Substances.FirstOrDefault(x => x.Substance == substance).Moles >= moles;
        }

        /// <summary>
        /// Removes the specified amount of substance
        /// </summary>
        /// <param name="substance">The substance to remove</param>
        /// <param name="moles">The amount of substance</param>
        public void RemoveSubstance(Substance substance, float moles = float.MaxValue)
        {
            int index = IndexOfSubstance(substance);
            if (index < 0)
            {
                return;
            }
            SubstanceEntry entry = Substances[index];
            float newAmount = entry.Moles - moles;
            if (newAmount <= 0)
            {
                Substances.RemoveAt(index);
            }
            else
            {
                entry.Moles = newAmount;
                Substances[index] = entry;
            }
            OnContentsChanged();
        }

        /// <summary>
        /// Empties the container
        /// </summary>
        public void Empty()
        {
            Substances.Clear();
            OnContentsChanged();
        }

        /// <summary>
        /// Removes moles from the container
        /// </summary>
        /// <param name="moles">The amount of moles</param>
        public void RemoveMoles(float moles)
        {
            var totalMoles = Substances.Sum(x => x.Moles);
            if (moles > totalMoles)
            {
                moles = totalMoles;
            }
            
            for (var i = 0; i < Substances.Count; i++)
            {
                SubstanceEntry entry = Substances[i];
                entry.Moles -= entry.Moles / totalMoles * moles;
                Substances[i] = entry;
            }
            OnContentsChanged();
        }

        /// <summary>
        /// Transfers moles to a different container
        /// </summary>
        /// <param name="other">The other container</param>
        /// <param name="moles">The moles to transfer</param>
        public void TransferMoles(SubstanceContainer other, float moles)
        {
            var totalMoles = Substances.Sum(x => x.Moles);
            if (moles > totalMoles)
            {
                moles = totalMoles;
            }
            
            for (var i = 0; i < Substances.Count; i++)
            {
                SubstanceEntry entry = Substances[i];
                float entryMoles = entry.Moles / totalMoles * moles;
                entry.Moles -= entryMoles;
                other.AddSubstance(entry.Substance, entryMoles);
                Substances[i] = entry;
            }
            OnContentsChanged();
        }

        /// <summary>
        /// Returns the index of an substance
        /// </summary>
        /// <param name="substance">The substance to look for</param>
        /// <returns>The index or -1 if it is not found</returns>
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

        protected virtual void OnContentsChanged()
        {
            ContentsChanged?.Invoke(this);
        }
    }
}
