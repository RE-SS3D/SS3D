using UnityEngine;

namespace SS3D.Engine.Health
{
    public class BodyPartBehaviour : MonoBehaviour
    {
        private float bruteDamage;
        private float burnDamage;
        public float BruteDamage { get { return bruteDamage; } set { bruteDamage = Mathf.Clamp(value, 0, 200); } }
        public float BurnDamage { get { return burnDamage; } set { burnDamage = Mathf.Clamp(value, 0, 200); } }

        public int MaxDamage = 200;
        public BodyPartType Type;
        public bool isBleeding = false;
        public CreatureHealth creatureHealth;

        public DamageSeverity Severity;
        public float OverallDamage => BruteDamage + BurnDamage;

        void Start()
        {

        }

        //Apply damages from here.
        public virtual void ReceiveDamage(DamageType damageType, float damage)
        {
            UpdateDamage(damage, damageType);
        }

        private void UpdateDamage(float damage, DamageType type)
        {
            switch (type)
            {
                case DamageType.Brute:
                    BruteDamage += damage;
                    break;

                case DamageType.Burn:
                    BurnDamage += damage;
                    break;
            }
        }

        //Restore damage from here
        public virtual void HealDamage(int damage, DamageType type)
        {
            switch (type)
            {
                case DamageType.Brute:
                    BruteDamage -= damage;
                    if (BruteDamage < 20)
                    {
                        creatureHealth.bloodSystem.StopBleeding(this);
                    }
                    break;

                case DamageType.Burn:
                    BurnDamage -= damage;
                    break;
            }
            UpdateSeverity();
        }

        public float GetDamageValue(DamageType damageType)
        {
            if (damageType == DamageType.Brute)
            {
                return BruteDamage;
            }
            if (damageType == DamageType.Burn)
            {
                return BurnDamage;
            }
            return 0;
        }

        private void UpdateSeverity()
        {
            // update UI limbs depending on their severity of damage
            float severity = (float)OverallDamage / MaxDamage;
            // If the limb is uninjured
            if (severity <= 0)
            {
                Severity = DamageSeverity.None;
            }
            // If the limb is under 20% damage
            else if (severity < 0.2)
            {
                Severity = DamageSeverity.Light;
            }
            // If the limb is under 40% damage
            else if (severity < 0.4)
            {
                Severity = DamageSeverity.LightModerate;
            }
            // If the limb is under 60% damage
            else if (severity < 0.6)
            {
                Severity = DamageSeverity.Moderate;
            }
            // If the limb is under 80% damage
            else if (severity < 0.8)
            {
                Severity = DamageSeverity.Bad;
            }
            // If the limb is under 100% damage
            else if (severity < 1f)
            {
                Severity = DamageSeverity.Critical;
            }
            // If the limb is 100% damage or over
            else if (severity >= 1f)
            {
                Severity = DamageSeverity.Max;
            }

            UpdateIcons();
        }

        // TODO: updates the health UI 
        private void UpdateIcons()
        {

        }

        public virtual void RestoreDamage()
        {
            BruteDamage = 0;
            BurnDamage = 0;
            UpdateSeverity();
        }

        // --------------------
        // UPDATES FROM SERVER
        // --------------------
        public void UpdateClientBodyPartStat(float _bruteDamage, float _burnDamage)
        {
            BruteDamage = _bruteDamage;
            BurnDamage = _burnDamage;
            UpdateSeverity();
        }
    }
}