using SS3D.Core.Behaviours;
using SS3D.Systems.Health;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Combat
{
    public class SimpleHittingItem : Actor, IHittingItem
    {
        [SerializeField]
        private HitType _hitType;

        [SerializeField]
        private float _baseDamage;

        [SerializeField]
        private DamageType _damageType;

        public HitType HitType => _hitType;

        public DamageType DamageType => _damageType;

        public float BaseDamage => _baseDamage;
    }
}
