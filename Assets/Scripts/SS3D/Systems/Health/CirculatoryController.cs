using FishNet.Object;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Logging;
using SS3D.Substances;
using SS3D.Systems.Health;
using System;
using System.Collections;
using System.Collections.Generic;
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

        private CirculatoryLayer[] _perfusedLayers = Array.Empty<CirculatoryLayer>();
        private float[] _needs = Array.Empty<float>();
        private float _sumNeed;

        // Set when the body's shape changes. The perfused set is re-derived from it at the top of the next tick, not in
        // the event handler: the handlers fire mid-teardown, before BodyPart.Dispose has cut the departing part out of
        // its parent's child list, so a walk from there still reaches it and keeps it. Deferring also coalesces the
        // several events one dismemberment raises into a single walk.
        private bool _perfusedSetDirty;
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

            if (oxygenQuantity > MaxOxygenQuantity)
            {
                _container.RemoveSubstance(_oxygen, oxygenQuantity - MaxOxygenQuantity);
            }

            return bloodVolume > healthyBloodVolume ? oxygenQuantity : (bloodVolume / healthyBloodVolume) * oxygenQuantity;
        }

        /// <summary>
        /// Compute the need in oxygen of every body part in the provided list.
        /// </summary>
        [Server]
        public float[] ComputeIndividualNeeds(IReadOnlyCollection<BodyPart> parts)
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
            // Topology has settled by now, unlike inside the add/remove handlers themselves.
            if (_perfusedSetDirty)
            {
                _perfusedSetDirty = false;
                RebuildPerfusedList();
            }

            if (_cachesDirty)
            {
                RebuildCaches();
            }

            // The heartbeat drives bleeding (via OnPulse) and may remove blood, so beat before reading bloodFactor.
            if (_heart)
            {
                _heart.BeatTick(dt);
            }

            if (_sumNeed <= 0f)
            {
                return;
            }

            double available = AvailableOxygen();

            float cardiacFactor = _heart ? _heart.CardiacOutputFactor : 0f;
            float healthyBloodVolume = HealthConstants.HealthyBloodVolumeRatio * MaxBloodVolume;
            float bloodFactor = healthyBloodVolume > 0f ? Mathf.Clamp01(_container.GetSubstanceVolume(_blood) / healthyBloodVolume) : 0f;

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
                // Contain a failing part rather than letting it take the rest down with it. Without this the throw
                // aborts the remaining iterations, so every later part goes untended, and then escapes into
                // MetabolicSubSystem.Update and stops the metabolic tick for every other entity as well.
                try
                {
                    double supply = deliverable * (_needs[i] / _sumNeed);
                    totalRefilled += _perfusedLayers[i].MetabolicStep(supply, dt);
                }
                catch (Exception exception)
                {
                    Log.Error(
                        this,
                        exception,
                        "Metabolic step threw for perfused layer {Index} of {Count} on {Entity}, skipping it this tick. Usually means a destroyed part is still in the set, because the set was not re-derived when the part died.",
                        Logs.ServerOnly,
                        i,
                        _perfusedLayers.Length,
                        name);
                }
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
        /// The body's shape has changed, so mark the perfused set for re-derivation on the next tick. Deliberately not
        /// re-walked here: a removal is announced from inside BodyPart.DetachBodyPart, before Dispose has unlinked the
        /// departing part from its parent, so a walk at this moment still reaches it and keeps it perfused.
        /// Re-deriving rather than editing the set incrementally also costs nothing when a part is announced before its
        /// body layers exist: it is skipped that time round and picked up by the next re-derive. That does not arise
        /// here - SpawnOrgans is synchronous on this branch, so every organ is attached before Init runs and the set is
        /// complete on the first walk - but it is exactly what breaks once organ spawning becomes asynchronous, and the
        /// incremental version needed a pending-retry list to survive it: announce the heart once, reject it for having
        /// no layers yet, never look again, and the whole body has no circulation at all (#1362).
        /// </summary>
        [Server]
        private void HandleBodyPartAdded(object sender, BodyPart part)
        {
            _perfusedSetDirty = true;
        }

        [Server]
        private void HandleBodyPartRemoved(object sender, BodyPart part)
        {
            _perfusedSetDirty = true;
        }

        /// <summary>
        /// Re-derive which parts are joined into this body's circulatory tree, by walking outward from the body itself.
        /// Deliberately not rooted at the heart. This answers a question about topology - what is plumbed into what -
        /// and that stays true whether or not anything is pumping. Whether blood actually moves through the tree is
        /// cardiacFactor's job, and with no heart that is 0, so every part is offered nothing and starves. Rooting here
        /// at the heart instead made a destroyed heart empty the set, which drove _sumNeed to 0 and made MetabolicTick
        /// return early - the body stopped being simulated rather than finishing dying. develop suffocated it, because
        /// consumption there ran off a global list entirely independent of the heart.
        /// </summary>
        [Server]
        private void RebuildPerfusedList()
        {
            _perfused.Clear();

            // The body's root parts: attached to nothing above them, and not an organ sitting inside something else.
            // On a human that is the torso alone, but iterating costs nothing and degrades gracefully.
            foreach (BodyPart part in _healthController.BodyPartsOnEntity)
            {
                if (part && !part.IsInsideBodyPart && !part.ParentBodyPart)
                {
                    AddPerfusedRecursion(part);
                }
            }

            _cachesDirty = true;
        }

        /// <summary>
        /// A part joins the tree if it is a root of the body, an internal organ of a part already in it, or a child
        /// reachable through an unbroken chain of circulatory layers.
        /// Descending only through parts that carry circulation is the whole point: blood cannot cross a part that has
        /// no vessels, so fixing a living foot onto a wooden leg does not keep the foot alive. A flat scan of everything
        /// under the entity - which is what this replaces - silently included it anyway.
        /// Membership means "plumbed in", not "currently being fed": a body whose heart has stopped or been destroyed
        /// still has a complete tree, and every part in it is offered nothing and starves.
        /// </summary>
        [Server]
        private void AddPerfusedRecursion(BodyPart current)
        {
            // Stopping on a false return does two jobs. It enforces the rule above - no circulation here means nothing
            // beyond here is reached either - and it doubles as a visited check, since a part already in the set
            // returns false too. That makes the walk safe on a cyclic topology: body parts are authored as a tree, but
            // nothing in the code enforces that, and an unguarded recursion would not survive a cycle.
            if (!AddIfPerfused(current))
            {
                return;
            }

            if (current.HasInternalBodyPart)
            {
                foreach (BodyPart organ in current.InternalBodyParts)
                {
                    AddIfPerfused(organ);
                }
            }

            foreach (BodyPart child in current.ChildBodyParts)
            {
                AddPerfusedRecursion(child);
            }
        }

        /// <summary>
        /// Add a body part to the perfused set if it carries a circulatory layer and is not already tracked. Organs are
        /// checked for the layer like anything else - the old graph walk added them unconditionally and then dereferenced
        /// the layer it had not checked for.
        /// Destroyed parts are rejected on IsDestroyed rather than on Unity truthiness, because truthiness is a frame too
        /// late: Object.Destroy leaves the reference valid for the rest of the frame it is called in (measured - see the
        /// rework doc), while the re-derive runs in that same frame. IsDestroyed is already true when the removal event
        /// fires, since it is the precondition InflictDamage tests before calling DestroyBodyPart at all.
        /// This relies on CleanLayers() not resetting damages - every Cleanlayer() override is currently empty. If one
        /// ever clears its damage container, IsDestroyed flips back to false during Dispose and destroyed parts start
        /// re-entering the perfused set.
        /// </summary>
        /// <returns>True if the part was newly added. The walk uses this to decide whether to descend any further.</returns>
        [Server]
        private bool AddIfPerfused(BodyPart part)
        {
            if (!part || part.IsDestroyed || !part.ContainsLayer(BodyLayerType.Circulatory) || _perfused.Contains(part))
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
