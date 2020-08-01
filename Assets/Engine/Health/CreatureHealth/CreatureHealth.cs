using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SS3D.Engine.Health
{
    public static class HealthThreshold
    {
        public const int SoftCrit = 0;
        public const int Crit = -30;
        public const int Dead = -100;
        public const int OxygenPassOut = 50;
    }

    /// <summary>
    /// Event which fires when conscious state changes, provides the old state and the new state
    /// </summary>
    public class ConsciousStateEvent : UnityEvent<ConsciousState, ConsciousState>
    {
    }

    [RequireComponent(typeof(HealthStateManager))]
    public abstract class CreatureHealth : NetworkBehaviour, IHealth
    {
        private static readonly float GIB_THRESHOLD = 200f;
        
        //damage incurred per tick per fire stack
        private static readonly float DAMAGE_PER_FIRE_STACK = 0.08f;
        
        //volume and temp of hotspot exposed by this player when they are on fire
        private static readonly float BURNING_HOTSPOT_VOLUME = .005f;
        private static readonly float BURNING_HOTSPOT_TEMPERATURE = 700f;

        /// <summary>
        /// Invoked when conscious state changes. Provides old state and new state as 1st and 2nd args.
        /// </summary>
        [NonSerialized]
        public ConsciousStateEvent OnConsciousStateChangeServer = new ConsciousStateEvent();

        /// <summary>
        /// Server side, each mob has a different one and never it never changes
        /// </summary>
        public int mobID { get; private set; }

        public float maxHealth = 100;
        public float Resistance { get; } = 50;

        [Tooltip("For mobs that can breath in any atmos environment")]
        public bool canBreathAnywhere = false;

        public float OverallHealth { get; private set; } = 100;
        public float cloningDamage;

        /// <summary>
        /// Serverside, used for gibbing bodies after certain amount of damage is received afer death
        /// </summary>
        private float afterDeathDamage = 0f;

        // Systems can also be added via inspector
        public BloodSystem bloodSystem;
        public BrainSystem brainSystem;
        public RespiratorySystem respiratorySystem;

        public BloodSplatType bloodColor;

        /// <summary>
        /// If there are any body parts for this living thing, then add them to this list
        /// via the inspector. There needs to be at least 1 chest bodypart for a living animal
        /// </summary>
        [Header("Fill BodyPart fields in via Inspector:")]
        public List<BodyPartBehaviour> BodyParts = new List<BodyPartBehaviour>();

        //For meat harvest (pete etc)
        public bool allowKnifeHarvest;

        [Header("For harvestable animals")]
        public GameObject[] butcherResults;

        protected DamageType LastDamageType;
        protected GameObject LastDamagedBy;

        public ConsciousState ConsciousState
        {
            get => consciousState;
            protected set
            {
                ConsciousState oldState = consciousState;
                if (value != oldState)
                {
                    consciousState = value;
                    if (isServer)
                    {
                        OnConsciousStateChangeServer.Invoke(oldState, value);
                    }
                }
            }
        }

        // JSON string for blood types and DNA.
        [SyncVar(hook = nameof(DNASync))] //May remove this in the future and only provide DNA info on request
        private string DNABloodTypeJSON;

        // BloodType and DNA Data.
        private DNABloodType DNABloodType;
        private float tickRate = 1f;
        private float tick = 0;
        private ConsciousState consciousState;

        public bool IsCrit => ConsciousState == ConsciousState.Unconscious;
        public bool IsSoftCrit => ConsciousState == ConsciousState.BarelyConscious;
        public bool IsDead => ConsciousState == ConsciousState.Dead;

        /// <summary>
        /// Has the heart stopped.
        /// </summary>
        public bool IsCardiacArrest => bloodSystem.HeartStopped;

        private void EnsureInit()
        {
            //Always include blood for living entities:
            bloodSystem = GetComponent<BloodSystem>();
            if (bloodSystem == null)
            {
                bloodSystem = gameObject.AddComponent<BloodSystem>();
            }

            //Always include respiratory for living entities:
            respiratorySystem = GetComponent<RespiratorySystem>();
            if (respiratorySystem == null)
            {
                respiratorySystem = gameObject.AddComponent<RespiratorySystem>();
            }
            respiratorySystem.canBreathAnywhere = canBreathAnywhere;

            var tryGetHead = FindBodyPart(BodyPartType.Head);
            if (tryGetHead != null && brainSystem == null)
            {
                if (tryGetHead.Type != BodyPartType.Torso)
                {
                    //Head exists, install a brain system
                    brainSystem = gameObject.AddComponent<BrainSystem>();
                }
            }
        }

        public override void OnStartServer()
        {
            EnsureInit();
            mobID = PlayerManager.Instance.GetMobID();
            ResetBodyParts();
            if (maxHealth <= 0)
            {
                Debug.LogWarning($"Max health ({maxHealth}) set to zero/below zero!");
                maxHealth = 1;
            }

            //Generate BloodType and DNA
            DNABloodType = new DNABloodType();
            DNABloodType.BloodColor = bloodColor;
            DNABloodTypeJSON = JsonUtility.ToJson(DNABloodType);
            bloodSystem.SetBloodType(DNABloodType);
        }

        public override void OnStartClient()
        {
            EnsureInit();
            StartCoroutine(WaitForClientLoad());
        }

        IEnumerator WaitForClientLoad()
        {
            //wait for DNA:
            while (string.IsNullOrEmpty(DNABloodTypeJSON))
            {
                yield return WaitFor.EndOfFrame;
            }
            yield return WaitFor.EndOfFrame;
            DNASync(DNABloodTypeJSON, DNABloodTypeJSON);
        }

        // This is the DNA SyncVar hook
        private void DNASync(string oldDNA, string updatedDNA)
        {
            EnsureInit();
            DNABloodTypeJSON = updatedDNA;
            DNABloodType = JsonUtility.FromJson<DNABloodType>(updatedDNA);
        }

        private BodyPartBehaviour GetBodyPart(float amount, DamageType damageType, BodyPartType bodyPartAim = BodyPartType.Torso)
        {
            if (amount <= 0 || IsDead)
            {
                return null;
            }

            if (bodyPartAim == BodyPartType.EyeLeft || bodyPartAim == BodyPartType.EyeRight
                || bodyPartAim == BodyPartType.EarLeft || bodyPartAim == BodyPartType.EarRight)
            {
                bodyPartAim = BodyPartType.Head;
            }

            if (BodyParts.Count == 0)
            {
                Debug.LogError($"There are no body parts to apply a health change to for {gameObject.name}");
                return null;
            }

            // See if damage affects the state of the blood:
            // See if any of the healing applied affects blood state
            bloodSystem.AffectBloodState(bodyPartAim, damageType, amount);

            if (damageType == DamageType.Brute || damageType == DamageType.Burn)
            {
                BodyPartBehaviour bodyPartBehaviour = null;

                for (int i = 0; i < BodyParts.Count; i++)
                {
                    if (BodyParts[i].Type == bodyPartAim)
                    {
                        bodyPartBehaviour = BodyParts[i];
                        break;
                    }
                }

                //If the body part does not exist then try to find the torso instead
                if (bodyPartBehaviour == null)
                {
                    var getTorsoIndex = BodyParts.FindIndex(x => x.Type == BodyPartType.Torso);
                    if (getTorsoIndex != -1)
                    {
                        bodyPartBehaviour = BodyParts[getTorsoIndex];
                    }
                    else
                    {
                        //If there is no default chest body part then do nothing
                        Debug.LogError($"No torso body part found for {gameObject.name}");
                        return null;
                    }
                }
                return bodyPartBehaviour;
            }
            return null;
        }

        public BodyPartBehaviour FindBodyPart(BodyPartType bodyPartAim)
        {
            int searchIndex = BodyParts.FindIndex(x => x.Type == bodyPartAim);
            if (searchIndex != -1)
            {
                return BodyParts[searchIndex];
            }
            //If nothing is found then try to find a chest component:
            searchIndex = BodyParts.FindIndex(x => x.Type == BodyPartType.Torso);
            if (searchIndex != -1)
            {
                return BodyParts[searchIndex];
            }
            // else nothing:
            return null;
        }

        /// <summary>
        /// Reset all body part damage.
        /// </summary>
        [Server]
        private void ResetBodyParts()
        {
            foreach (BodyPartBehaviour bodyPart in BodyParts)
            {
                bodyPart.RestoreDamage();
                bodyPart.creatureHealth = this;
            }
        }



        /// <summary>
        ///  Apply Damage to the whole body of this Living thing. Server only
        /// </summary>
        /// <param name="damagedBy">The player or object that caused the damage. Null if there is none</param>
        /// <param name="damage">Damage Amount. will be distributed evenly across all bodyparts</param>
        /// <param name="attackType">type of attack that is causing the damage</param>
        /// <param name="damageType">The Type of Damage</param>
        [Server]
        public void ApplyDamage(GameObject damagedBy, float damage,
            AttackType attackType, DamageType damageType)
        {
            foreach (var bodyPart in BodyParts)
            {
                ApplyDamageToBodypart(damagedBy, damage / BodyParts.Count, attackType, damageType, bodyPart.Type);
            }
        }

        /// <summary>
        ///  Apply Damage to random bodypart of the Living thing. Server only
        /// </summary>
        /// <param name="damagedBy">The player or object that caused the damage. Null if there is none</param>
        /// <param name="damage">Damage Amount</param>
        /// <param name="attackType">type of attack that is causing the damage</param>
        /// <param name="damageType">The Type of Damage</param>
        [Server]
        public void ApplyDamageToBodypart(GameObject damagedBy, float damage,
            AttackType attackType, DamageType damageType)
        {
            ApplyDamageToBodypart(damagedBy, damage, attackType, damageType, BodyPartType.Chest.Randomize(0));
        }

        /// <summary>
        ///  Apply Damage to the Living thing. Server only
        /// </summary>
        /// <param name="damagedBy">The player or object that caused the damage. Null if there is none</param>
        /// <param name="damage">Damage Amount</param>
        /// <param name="attackType">type of attack that is causing the damage</param>
        /// <param name="damageType">The Type of Damage</param>
        /// <param name="bodyPartAim">Body Part that is affected</param>
        [Server]
        public virtual void ApplyDamageToBodypart(GameObject damagedBy, float damage,
            AttackType attackType, DamageType damageType, BodyPartType bodyPartAim)
        {
            if (IsDead)
            {
                afterDeathDamage += damage;
                if (afterDeathDamage >= GIB_THRESHOLD)
                {
                    Harvest(); //Gib() instead when fancy gibs are in
                }
            }

            BodyPartBehaviour bodyPartBehaviour = GetBodyPart(damage, damageType, bodyPartAim);
            if (bodyPartBehaviour == null)
            {
                return;
            }

            //TODO: determine and apply armor protection

            var prevHealth = OverallHealth;

            applyDamageEvent?.Invoke(damagedBy);

            LastDamageType = damageType;
            LastDamagedBy = damagedBy;
            bodyPartBehaviour.ReceiveDamage(damageType, damage);
            HealthBodyPartMessage.Send(gameObject, gameObject, bodyPartAim, bodyPartBehaviour.BruteDamage, bodyPartBehaviour.BurnDamage);

            if (attackType == AttackType.Fire)
            {
                // SyncFireStacks(fireStacks, fireStacks + 1);
            }

            //For special effects spawning like blood:
            DetermineDamageEffects(damageType);

            // Debug.LogWarning("{3} received {0} {4} damage from {6} aimed for {5}. Health: {1}->{2}", 
            //     damage, prevHealth, OverallHealth, gameObject.name, damageType, bodyPartAim, damagedBy);
        }

        /// <summary>
        ///  Apply healing to a living thing. Server Only
        /// </summary>
        /// <param name="healingItem">the item used for healing (bruise pack etc). Null if there is none</param>
        /// <param name="healAmt">Amount of healing to add</param>
        /// <param name="damageType">The Type of Damage To Heal</param>
        /// <param name="bodyPartAim">Body Part to heal</param>
        [Server]
        public virtual void HealDamage(GameObject healingItem, int healAmt,
            DamageType damageTypeToHeal, BodyPartType bodyPartAim)
        {
            BodyPartBehaviour bodyPartBehaviour = GetBodyPart(healAmt, damageTypeToHeal, bodyPartAim);
            if (bodyPartBehaviour == null)
            {
                return;
            }
            bodyPartBehaviour.HealDamage(healAmt, damageTypeToHeal);
            HealthBodyPartMessage.Send(gameObject, gameObject, bodyPartAim, bodyPartBehaviour.BruteDamage, bodyPartBehaviour.BurnDamage);

            var prevHealth = OverallHealth;
            Logger.LogTraceFormat("{3} received {0} {4} healing from {6} aimed for {5}. Health: {1}->{2}", Category.Health,
                healAmt, prevHealth, OverallHealth, gameObject.name, damageTypeToHeal, bodyPartAim, healingItem);
        }

        public void OnExposed(FireExposure exposure)
        {
            ApplyDamage(null, 1, AttackType.Fire, DamageType.Burn);
        }

        /// ---------------------------
        /// UPDATE LOOP
        /// ---------------------------
        public void Update()
        {
            //Server Only:
            if (isServer && !IsDead)
            {
                tick += Time.deltaTime;
                if (tick > tickRate)
                {
                    tick = 0f;
                    //if (fireStacks > 0)
                    //{
                    //    //TODO: Burn clothes (see species.dm handle_fire)
                    //    ApplyDamageToBodypart(null, fireStacks * DAMAGE_PER_FIRE_STACK, AttackType.Internal, DamageType.Burn);
                    //    //gradually deplete fire stacks
                    //    SyncFireStacks(fireStacks, fireStacks - 0.1f);
                    //    //instantly stop burning if there's no oxygen at this location
                    //    MetaDataNode node = registerTile.Matrix.MetaDataLayer.Get(registerTile.LocalPositionClient);
                    //    if (node.GasMix.GetMoles(Gas.Oxygen) < 1)
                    //    {
                    //        SyncFireStacks(fireStacks, 0);
                    //    }
                    //    registerTile.Matrix.ReactionManager.ExposeHotspotWorldPosition(gameObject.TileWorldPosition(), BURNING_HOTSPOT_TEMPERATURE, BURNING_HOTSPOT_VOLUME);
                    //}

                    CalculateOverallHealth();
                    CheckHealthAndUpdateConsciousState();
                }
            }
        }

        /// ---------------------------
        /// VISUAL EFFECTS
        /// ---------------------------

        /// <Summary>
        /// Used to determine any special effects spawning cased by a damage type
        /// Server only
        /// </Summary>
        [Server]
        protected virtual void DetermineDamageEffects(DamageType damageType)
        {
            //Brute attacks
            if (damageType == DamageType.Brute)
            {
                //spawn blood
                //EffectsFactory.BloodSplat(registerTile.WorldPositionServer, BloodSplatSize.medium, bloodColor);
            }
        }

        /// ---------------------------
        /// HEALTH CALCULATIONS
        /// ---------------------------

        /// <summary>
        /// Recalculates the overall player health and updates OverallHealth property. Server only
        /// </summary>
        [Server]
        public void CalculateOverallHealth()
        {
            float newHealth = 100;
            newHealth -= CalculateOverallBodyPartDamage();
            newHealth -= CalculateOverallBloodLossDamage();
            newHealth -= bloodSystem.OxygenDamage;
            newHealth -= cloningDamage;
            OverallHealth = newHealth;
        }

        public float CalculateOverallBodyPartDamage()
        {
            float bodyPartDmg = 0;
            for (int i = 0; i < BodyParts.Count; i++)
            {
                bodyPartDmg += BodyParts[i].BruteDamage;
                bodyPartDmg += BodyParts[i].BurnDamage;
            }
            return bodyPartDmg;
        }

        public float GetTotalBruteDamage()
        {
            float bruteDmg = 0;
            for (int i = 0; i < BodyParts.Count; i++)
            {
                bruteDmg += BodyParts[i].BruteDamage;
            }
            return bruteDmg;
        }

        public float GetTotalBurnDamage()
        {
            float burnDmg = 0;
            for (int i = 0; i < BodyParts.Count; i++)
            {
                burnDmg += BodyParts[i].BurnDamage;
            }
            return burnDmg;
        }

        /// Blood Loss and Toxin damage:
        public int CalculateOverallBloodLossDamage()
        {
            float maxBloodDmg = Mathf.Abs(HealthThreshold.Dead) + maxHealth;
            float bloodDmg = 0f;
            if (bloodSystem.BloodLevel < (int)BloodVolume.SAFE)
            {
                bloodDmg = Mathf.Lerp(0f, maxBloodDmg, 1f - (bloodSystem.BloodLevel / (float)BloodVolume.NORMAL));
            }

            if (bloodSystem.ToxinLevel > 1f)
            {
                //TODO determine a way to handle toxin damage when toxins are implemented
                //There will need to be some kind of blood / toxin ratio and severity limits determined
            }

            return Mathf.RoundToInt(Mathf.Clamp(bloodDmg, 0f, maxBloodDmg));
        }

        /// ---------------------------
        /// CRIT + DEATH METHODS
        /// ---------------------------

        ///Death from other causes
        public virtual void Death()
        {
            if (IsDead)
            {
                return;
            }
            afterDeathDamage = 0;
            ConsciousState = ConsciousState.Dead;
            OnDeathActions();
            bloodSystem.StopBleedingAll();
            //stop burning
            //TODO: When clothes/limb burning is implemented, probably should keep burning until clothes are burned up
            //SyncFireStacks(fireStacks, 0);
        }

        private void Crit(bool allowCrawl = false)
        {
            var proposedState = allowCrawl ? ConsciousState.BarelyConscious : ConsciousState.Unconscious;

            if (ConsciousState == proposedState || IsDead)
            {
                return;
            }

            ConsciousState = proposedState;
        }

        private void Uncrit()
        {
            var proposedState = ConsciousState.Conscious;
            if (ConsciousState == proposedState || IsDead)
            {
                return;
            }
            ConsciousState = proposedState;
        }

        /// <summary>
        /// Checks if the player's health has changed such that consciousstate needs to be changed,
        /// and changes consciousstate and invokes whatever needs to be invoked when the state changes
        /// </summary>
        private void CheckHealthAndUpdateConsciousState()
        {
            if (ConsciousState != ConsciousState.Conscious && bloodSystem.OxygenDamage < HealthThreshold.OxygenPassOut && OverallHealth > HealthThreshold.SoftCrit)
            {
                //Logger.LogFormat("{0}, back on your feet!", Category.Health, gameObject.name);
                Uncrit();
                return;
            }

            if (OverallHealth <= HealthThreshold.SoftCrit || bloodSystem.OxygenDamage > HealthThreshold.OxygenPassOut)
            {
                if (OverallHealth <= HealthThreshold.Crit)
                {
                    Crit(false);
                }
                else
                {
                    Crit(true); //health isn't low enough for crit, but might be low enough for soft crit or passed out from lack of oxygen
                }
            }
            if (NotSuitableForDeath())
            {
                return;
            }
            Death();
        }

        private bool NotSuitableForDeath()
        {
            return OverallHealth > HealthThreshold.Dead || IsDead;
        }

        protected abstract void OnDeathActions();

        /// <summary>
        /// Updates the main health stats from the server via NetMsg
        /// </summary>
        public void UpdateClientHealthStats(float overallHealth)
        {
            OverallHealth = overallHealth;
            // Logger.Log($"Update stats for {gameObject.name} OverallHealth: {overallHealth} ConsciousState: {consciousState.ToString()}", Category.Health);
        }

        /// <summary>
        /// Updates the respiratory health stats from the server via NetMsg
        /// </summary>
        public void UpdateClientRespiratoryStats(bool value)
        {
            respiratorySystem.IsSuffocating = value;
        }

        public void UpdateClientTemperatureStats(float value)
        {
            respiratorySystem.temperature = value;
        }

        public void UpdateClientPressureStats(float value)
        {
            respiratorySystem.pressure = value;
        }

        // <summary>
        /// Updates the brain health stats from the server via NetMsg
        /// </summary>
        public void UpdateClientBrainStats(bool isHusk, int brainDamage)
        {
            if (brainSystem != null)
            {
                brainSystem.UpdateClientBrainStats(isHusk, brainDamage);
            }
        }

        /// <summary>
        /// Updates the bodypart health stats from the server via NetMsg
        /// </summary>
        public void UpdateClientBodyPartStats(BodyPartType bodyPartType, float bruteDamage, float burnDamage)
        {
            var bodyPart = FindBodyPart(bodyPartType);
            if (bodyPart != null)
            {
                //	Logger.Log($"Update stats for {gameObject.name} body part {bodyPartType.ToString()} BruteDmg: {bruteDamage} BurnDamage: {burnDamage}", Category.Health);

                bodyPart.UpdateClientBodyPartStat(bruteDamage, burnDamage);
            }
        }

        ///<summary>
        /// If Harvesting is allowed (for pete the goat for example)
        /// then spawn the butchered results
        /// </summary>
        [Server]
        public void Harvest()
        {
            foreach (GameObject harvestPrefab in butcherResults)
            {
                Spawn.ServerPrefab(harvestPrefab, transform.position, parent: transform.parent);
            }

            Gib();
        }

        [Server]
        protected virtual void Gib()
        {
            EffectsFactory.BloodSplat(transform.position, BloodSplatSize.large, bloodColor);
            //todo: actual gibs

            //never destroy players!
            Despawn.ServerSingle(gameObject);
        }


    }
}