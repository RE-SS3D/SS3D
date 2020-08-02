using Mirror;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace SS3D.Engine.Health
{
    public class Integrity : NetworkBehaviour
    {
        /// <summary>
        /// Server-side event invoked when object integrity reaches 0 by any means and object
        /// destruction logic is about to be invoked. Does not override the default destruction logic,
        /// simply provides a hook for when it is going to be invoked.
        /// </summary>
        [NonSerialized]
        public DestructionEvent OnWillDestroyServer = new DestructionEvent();

        // <summary>
        /// Server-side burn up logic - invoked when integrity reaches 0 due to burn damage.
        /// Setting this will override the default burn up logic.
        /// See OnWillDestroyServer if you only want an event when the object is destroyed
        /// and don't want to override the burn up logic.
        /// </summary>
        /// <returns></returns>
        [NonSerialized]
        public UnityAction<DestructionInfo> OnBurnUpServer;

        /// <summary>
        /// Armor for this object.
        /// </summary>
        [Tooltip("Armor for this object.")]
        public Armor Armor = new Armor();

        /// <summary>
        /// resistances for this object.
        /// </summary>
        [Tooltip("Resistances of this object.")]
        public Resistances Resistances = new Resistances();

        /// <summary>
        /// Below this temperature (in Kelvin) the object will be unaffected by fire exposure.
        /// </summary>
        [Tooltip("Below this temperature (in Kelvin) the object will be unaffected by fire exposure.")]
        public float HeatResistance = 100;

        public float InitialIntegrity = 100f;
        public float integrity { get; private set; } = 100f;
        private bool destroyed = false;

        private DamageType lastDamageType;

        //whether this is a large object (meaning we would use the large ash pile)
        private bool isLarge;


        private void Awake()
        {
            EnsureInit();
        }

        private void EnsureInit()
        {
            // TODO: Add logic 
        }

        public override void OnStartClient()
        {
            EnsureInit();
        }

        [Server]
        private void DefaultBurnUp(DestructionInfo info)
        {
            // TODO: Burn effect

            // TODO: Register burn in the chat
        }

        [Server]
        private void DefaultDestroy(DestructionInfo info)
        {
            if (info.DamageType == DamageType.Brute)
            {
                // TODO: Register destroyed object in the chat
                
                // TODO: Despawn the destroyed object
            }
            // TODO: Other damage types (acid)
            else
            {
                // TODO: Register destroyed object in the chat for other damage types

                // TODO: Despawn the destroyed object
            }
        }


        /// <summary>
        /// Directly deal damage to this object.
        /// </summary>
        /// <param name="damage"></param>
        /// <param name="damageType"></param>
        [Server]
        public void ApplyDamage(float damage, AttackType attackType, DamageType damageType)
        {
            //already destroyed, don't apply damage
            if (destroyed || Resistances.Indestructable) return;

            if (Resistances.FireProof && attackType == AttackType.Fire) return;

            damage = Armor.GetDamage(damage, attackType);
            if (damage > 0)
            {
                integrity -= damage;
                lastDamageType = damageType;
                CheckDestruction();
                Logger.LogTraceFormat("{0} took {1} {2} damage from {3} attack (resistance {4}) (integrity now {5})", Category.Health, name, damage, damageType, attackType, Armor.GetRating(attackType), integrity);
            }
        }

        [Server]
        private void CheckDestruction()
        {
            if (!destroyed && integrity <= 0)
            {
                var destructInfo = new DestructionInfo(lastDamageType, this);
                OnWillDestroyServer.Invoke(destructInfo);

                if (destructInfo.DamageType == DamageType.Burn)
                {
                    if (OnBurnUpServer != null)
                    {
                        OnBurnUpServer(destructInfo);
                    }
                    else
                    {
                        DefaultBurnUp(destructInfo);
                    }
                }
                else
                {
                    DefaultDestroy(destructInfo);
                }

                destroyed = true;
            }
        }
    }

    /// <summary>
    /// Contains info on how an object was destroyed
    /// </summary>
    public class DestructionInfo
    {
        /// <summary>
        /// Damage that destroyed the object
        /// </summary>
        public readonly DamageType DamageType;

        /// <summary>
        /// Integrity of the object that was destroyed.
        /// </summary>
        public readonly Integrity Destroyed;

        public DestructionInfo(DamageType damageType, Integrity destroyed)
        {
            DamageType = damageType;
            Destroyed = destroyed;
        }
    }

    /// <summary>
    /// Event fired when an object is destroyed
    /// </summary>
    public class DestructionEvent : UnityEvent<DestructionInfo> { }
}