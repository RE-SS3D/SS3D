using FishNet.Object;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Substances;
using SS3D.Systems.Health;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Health
{
    public class CirculatoryController : NetworkActor, IMetabolicController
    {
        [SerializeField]
        private Heart _heart;

        [SerializeField]
        private SubstanceContainer _container;

        [SerializeField]
        private HealthController _healthController;

        private Substance _blood;
        private Substance _oxygen;

        // Perfused parts are the source of truth; the parallel caches (layers/needs) are rebuilt only when the set
        // changes (dirty flag), so the per-tick loop is allocation-free and safe against mid-tick removal (reentrancy).
        private readonly List<BodyPart> _perfused = new();

        // Parts announced before their body layers existed (async-spawned organs). Retried from the tick until they
        // can be perfused; normally drains within a frame or two of the organ attaching.
        private readonly List<BodyPart> _pendingPerfusion = new();
        private CirculatoryLayer[] _perfusedLayers = Array.Empty<CirculatoryLayer>();
        private float[] _needs = Array.Empty<float>();
        private float _sumNeed;
        private bool _cachesDirty;

        public SubstanceContainer Container => _container;

        /// <summary>
        /// The volume of blood in mLs the substance container can contain as a maximum. the 1 plus MaxOxygenToBloodVolumeRatio factor
        /// is there to "leave place" to oxygen in the container, which takes up a significant amount 
        /// (up to MaxOxygenToBloodVolumeRatio times 100 % of the blood volume).
        /// </summary>
        public float MaxBloodVolume => _healthController.BodyPartsVolume * HealthConstants.BloodVolumeToHumanVolumeRatio / (1 + HealthConstants.MaxOxygenToBloodVolumeRatio);

        /// <summary>
        /// The maximum amount of blood in mmols _container can handle.
        /// </summary>
        public float MaxBloodQuantity => MaxBloodVolume / _blood.MillilitersPerMilliMoles;

        /// <summary>
        /// The maximum volume in mLs of oxygen _container can handle.
        /// </summary>
        private float MaxOxygenFullBloodVolume => HealthConstants.MaxOxygenToBloodVolumeRatio * MaxBloodVolume;


        /// <summary>
        /// Maximum volume of oxygen in mLs that can fit in _container, given the present amount of blood in it.
        /// </summary>
        public float MaxOxygenVolume => HealthConstants.MaxOxygenToBloodVolumeRatio * _container.GetSubstanceVolume(_blood);


        /// <summary>
        /// Maximum amount of oxygen in mmols that can fit in _container, given the present amount of blood in it.
        /// </summary>
        public float MaxOxygenQuantity => MaxOxygenVolume / _oxygen.MillilitersPerMilliMoles;

        /// <summary>
        /// The maximum amount of oxygen in mmols _container can handle.
        /// </summary>
        private float MaxOxygenFullBloodQuantity => MaxOxygenFullBloodVolume / _oxygen.MillilitersPerMilliMoles;

        /// <summary>
        /// Max total volume _container can have. The tolerance factor allows for a margin, but trouble can occurs if 
        /// the volume of the container is above MaxOxygenVolume + MaxBloodVolume.
        /// </summary>
        public float MaxTotalVolume => (MaxOxygenFullBloodVolume + MaxBloodVolume) * HealthConstants.HighBloodVolumeToleranceFactor;

        public override void OnStartServer()
        {
            base.OnStartServer();
            SubstancesSubSystem registry = SubSystems.Get<SubstancesSubSystem>();
            _blood = registry.FromType(SubstanceType.Blood);
            _oxygen = registry.FromType(SubstanceType.Oxygen);
            StartCoroutine(Init());
        }

        /// <summary>
        /// Has to wait a bit to set up all body parts, otherwise fail to get bodypart attached to heart.
        /// </summary>
        /// <returns></returns>
        [Server]
        private IEnumerator Init()
        {
            yield return null;
            yield return null;

            UpdateVolume();
            AddInitialSubstance();

            RebuildPerfusedList();

            _healthController.OnBodyPartAdded += HandleBodyPartAdded;
            _healthController.OnBodyPartRemoved += HandleBodyPartRemoved;

            // Registering with the scheduler is the readiness gate: nothing ticks this entity until Init has finished.
            SubSystems.Get<MetabolicSubSystem>().RegisterController(this);
        }

        /// <summary>
        /// Should be called when a body part is disconnected from heart, as the total volume of the circulatory container should
        /// become smaller (or bigger if a body part is added).
        /// TODO : should be called whenever a bodypart is destroyed or detached.
        /// </summary>
        [Server]
        private void UpdateVolume()
        {
            _container.ChangeVolume(MaxTotalVolume);
        }

        /// <summary>
        /// Return the amount of oxygen the circulatory system can send to organs.
        /// If the blood quantity is above a given treshold, all oxygen in the circulatory container is available.
        /// If blood gets below, it starts diminishing the availability of oxygen despite the circulatory system containing enough.
        /// This is to mimick the lack of blood making oxygen transport difficult and potentially leading to organ suffocation.
        /// </summary>
        [Server]
        public double AvailableOxygen()
        {
            float bloodVolume = _container.GetSubstanceVolume(_blood);

            float healthyBloodVolume = HealthConstants.HealthyBloodVolumeRatio * MaxBloodVolume;

            float oxygenQuantity = _container.GetSubstanceQuantity(_oxygen);

            if(oxygenQuantity > MaxOxygenQuantity)
            {
                _container.RemoveSubstance(_oxygen, oxygenQuantity - MaxOxygenQuantity);
            }

            return bloodVolume > healthyBloodVolume ? oxygenQuantity : (bloodVolume / healthyBloodVolume) * oxygenQuantity;
        }

        /// <summary>
        /// Compute the need in oxygen of every body part in the provided list.
        /// </summary>
        [Server]
        public float[] ComputeIndividualNeeds(ReadOnlyCollection<BodyPart> parts)
        {
            float[] oxygenNeededForEachpart = new float[parts.Count];
            int i = 0;
            foreach (BodyPart bodyPart in parts)
            {
                bodyPart.TryGetBodyLayer(out CirculatoryLayer circulatory);
                oxygenNeededForEachpart[i] = (float)circulatory.OxygenNeeded;
                i++;
            }
            return oxygenNeededForEachpart;
        }

        /// <summary>
        /// Simply add oxygen and blood in the maximum allowed amount.
        /// </summary>
        /// <returns></returns>
        public void AddInitialSubstance()
        {
            _container.AddSubstance(_blood, MaxBloodQuantity);
            _container.AddSubstance(_oxygen, MaxOxygenFullBloodQuantity);
        }

        public override void OnStopServer()
        {
            base.OnStopServer();

            // TryGet, not Get: on scene teardown the scheduler is destroyed before the entities registered with it,
            // and Get logs an error and returns null in that case - which then throws here. Registration in Init still
            // uses Get, because a scheduler missing at startup is a real fault and should be loud.
            if (SubSystems.TryGet(out MetabolicSubSystem scheduler))
            {
                scheduler.UnregisterController(this);
            }

            if (_healthController != null)
            {
                _healthController.OnBodyPartAdded -= HandleBodyPartAdded;
                _healthController.OnBodyPartRemoved -= HandleBodyPartRemoved;
            }

            if (_heart != null)
            {
                _heart.OnPulse -= HandleHeartBeat;
            }
        }

        /// <summary>
        /// One continuous, dt-scaled metabolic step for the whole entity: advance the heartbeat, then move oxygen from
        /// the blood pool into each perfused part's reserve (capped by cardiac output and blood volume) and burn this
        /// tick's demand. Debits the pool exactly once, by the amount actually taken.
        /// </summary>
        [Server]
        public void MetabolicTick(float dt)
        {
            if (_pendingPerfusion.Count > 0)
            {
                DrainPendingPerfusion();
            }

            if (_cachesDirty)
            {
                RebuildCaches();
            }

            // The heartbeat drives bleeding (via OnPulse) and may remove blood, so beat before reading bloodFactor.
            if (_heart != null)
            {
                _heart.BeatTick(dt);
            }

            if (_sumNeed <= 0f)
            {
                return;
            }

            double available = AvailableOxygen();

            float cardiacFactor = _heart != null ? _heart.CardiacOutputFactor : 0f;
            float healthyBloodVolume = HealthConstants.HealthyBloodVolumeRatio * MaxBloodVolume;
            float bloodFactor = healthyBloodVolume > 0f
                ? Mathf.Clamp01(_container.GetSubstanceVolume(_blood) / healthyBloodVolume)
                : 0f;

            double flowCap = cardiacFactor * bloodFactor * HealthConstants.FlowExtractionHeadroom * _sumNeed * dt;
            double deliverable = Math.Min(available, flowCap);

            double totalRefilled = ApportionOxygen(deliverable, dt);

            if (totalRefilled > 0d)
            {
                float refill = (float)totalRefilled;
                float inPool = _container.GetSubstanceQuantity(_oxygen);
                float toRemove = refill < inPool ? refill : inPool;
                if (toRemove > 0f)
                {
                    _container.RemoveSubstance(_oxygen, toRemove);
                }
            }
        }

        /// <summary>
        /// Split the deliverable oxygen across the perfused parts and run each part's metabolic step. This is the only
        /// place the apportionment lives: proportional to demand now, priority-weighted (triage) in a later stage.
        /// Returns the total oxygen actually taken into reserves, for the single pool debit.
        /// </summary>
        [Server]
        private double ApportionOxygen(double deliverable, float dt)
        {
            double totalRefilled = 0d;
            for (int i = 0; i < _perfusedLayers.Length; i++)
            {
                double supply = deliverable * (_needs[i] / _sumNeed);
                totalRefilled += _perfusedLayers[i].MetabolicStep(supply, dt);
            }

            return totalRefilled;
        }

        /// <summary>
        /// Make every perfused part bleed once per heartbeat. Bleeding cadence and amount are unchanged; only the
        /// ownership moved here, since the heart no longer holds the parts list.
        /// </summary>
        [Server]
        private void HandleHeartBeat(object sender, EventArgs args)
        {
            foreach (CirculatoryLayer layer in _perfusedLayers)
            {
                layer.Bleed();
            }
        }

        /// <summary>
        /// Track a body part announced by the health controller. We add the exact reference we were handed rather than
        /// re-reading the containers, which lag the attach by several frames.
        /// An internal organ is announced before its own OnStartServer has run (the torso's WaitUntil only gates on
        /// Instantiate having assigned the field, not on the network spawn completing), so it has no body layers yet
        /// and cannot be perfused at this instant. Park those in the pending list and admit them from the tick once
        /// their circulatory layer exists - otherwise the heart is announced once, rejected, and never seen again,
        /// leaving the body with no circulation at all (#1362).
        /// </summary>
        [Server]
        private void HandleBodyPartAdded(object sender, BodyPart part)
        {
            if (AddIfPerfused(part))
            {
                _cachesDirty = true;
            }
            else if (part && !_pendingPerfusion.Contains(part))
            {
                _pendingPerfusion.Add(part);
            }
        }

        /// <summary>
        /// Admit any pending part whose circulatory layer has since been created. Runs from the metabolic tick; the
        /// list is empty in the steady state, so this costs nothing once the body has finished initialising.
        /// </summary>
        [Server]
        private void DrainPendingPerfusion()
        {
            for (int i = _pendingPerfusion.Count - 1; i >= 0; i--)
            {
                BodyPart part = _pendingPerfusion[i];

                if (!part)
                {
                    _pendingPerfusion.RemoveAt(i);
                    continue;
                }

                if (AddIfPerfused(part))
                {
                    _pendingPerfusion.RemoveAt(i);
                    _cachesDirty = true;
                }
            }
        }

        [Server]
        private void HandleBodyPartRemoved(object sender, BodyPart part)
        {
            _pendingPerfusion.Remove(part);

            if (_perfused.Remove(part))
            {
                _cachesDirty = true;
            }
        }

        /// <summary>
        /// Build the perfused-parts list from the live body. Used once at Init to capture the parts present by then;
        /// afterwards the list is maintained incrementally through HealthController's add/remove events.
        /// GetComponentsInChildren only walks the transform hierarchy, which holds the external body parts but NOT the
        /// internal organs (heart, lungs, brain) - those live in each part's AttachedContainer. So we also pull every
        /// discovered part's InternalBodyParts; otherwise the heart is never found, no oxygen is ever delivered, and
        /// the whole body suffocates (#1362).
        /// </summary>
        [Server]
        private void RebuildPerfusedList()
        {
            _perfused.Clear();
            foreach (BodyPart part in _healthController.GetComponentsInChildren<BodyPart>())
            {
                AddIfPerfused(part);

                if (!part.HasInternalBodyPart)
                {
                    continue;
                }

                foreach (BodyPart organ in part.InternalBodyParts)
                {
                    AddIfPerfused(organ);
                }
            }

            _cachesDirty = true;
        }

        /// <summary>
        /// Add a body part to the perfused set if it carries a circulatory layer and is not already tracked.
        /// Returns true if it was newly added, so callers can mark the caches dirty.
        /// </summary>
        [Server]
        private bool AddIfPerfused(BodyPart part)
        {
            if (!part || !part.ContainsLayer(BodyLayerType.Circulatory) || _perfused.Contains(part))
            {
                return false;
            }

            _perfused.Add(part);
            return true;
        }

        /// <summary>
        /// Rebuild the parallel per-part caches (layers, per-part need, sum of needs) and re-derive the tracked heart
        /// from the perfused set. Runs only when the set has changed, never mid-iteration, so the tick stays safe.
        /// </summary>
        [Server]
        private void RebuildCaches()
        {
            int count = _perfused.Count;
            _perfusedLayers = new CirculatoryLayer[count];
            _needs = new float[count];
            _sumNeed = 0f;

            Heart foundHeart = null;
            for (int i = 0; i < count; i++)
            {
                BodyPart part = _perfused[i];
                part.TryGetBodyLayer(out CirculatoryLayer layer);
                _perfusedLayers[i] = layer;
                _needs[i] = (float)layer.OxygenNeeded;
                _sumNeed += _needs[i];

                if (part is Heart heart)
                {
                    foundHeart = heart;
                }
            }

            if (foundHeart != _heart)
            {
                if (_heart)
                {
                    _heart.OnPulse -= HandleHeartBeat;
                }

                _heart = foundHeart;

                if (_heart)
                {
                    _heart.OnPulse += HandleHeartBeat;
                }
            }

            _cachesDirty = false;
        }
    }
}
