using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace SS3D.Engine.Health
{

    [Serializable]
    public class Armor
    {
        public float Melee;
        public float Bullet;
        public float Laser;
        public float Energy;
        public float Bomb;
        public float Rad;
        public float Fire;
        public float Acid;
        public float Magic;
        public float Bio;


        public float GetDamage(float damage, AttackType attackType)
        {
            return damage * (100 - GetRating(attackType)) * .01f;
        }

        public float GetRating(AttackType attackType)
        {
            switch (attackType)
            {
                case AttackType.Melee:
                    return Melee;
                case AttackType.Bullet:
                    return Bullet;
                case AttackType.Laser:
                    return Laser;
                case AttackType.Energy:
                    return Energy;
                case AttackType.Bomb:
                    return Bomb;
                case AttackType.Bio:
                    return Bio;
                case AttackType.Rad:
                    return Rad;
                case AttackType.Fire:
                    return Fire;
                case AttackType.Acid:
                    return Acid;
                case AttackType.Magic:
                    return Magic;

            }

            return 0;
        }
    }

    public enum AttackType
    {
        Melee = 0,
        Bullet = 1,
        Laser = 2,
        Energy = 3,
        Bomb = 4,
        Rad = 5,
        Fire = 6,
        Acid = 7,
        Magic = 8,
        Bio = 9,
        ///type of attack that bypasses armor - such as suffocating. It's not possible
        /// to have armor against this
        Internal = 10
    }
}