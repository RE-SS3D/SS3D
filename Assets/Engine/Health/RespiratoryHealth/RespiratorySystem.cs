using Mirror;
using SS3D.Engine.Atmospherics;
using SS3D.Engine.Tiles;
using UnityEngine;

namespace SS3D.Engine.Health
{
    /// <summary>
    /// Controls the Respiratory System for this living thing
    /// Mostly managed server side and states sent to the clients
    /// </summary>
    public class RespiratorySystem : NetworkBehaviour
    {
        /// <summary>
        /// 2 minutes of suffocation = 100% damage
        /// </summary>
        public int SuffocationDamage => Mathf.RoundToInt((suffocationTime / 120f) * 100f);

        private float suffocationTime = 0f;
        private BloodSystem bloodSystem;
        private CreatureHealth creatureHealth;

        private float tickRate = 1f;
        private float tick = 0f;
        private float breatheCooldown = 0;

        public bool IsSuffocating;
        public float InternalPressure { get; set; } = 101.325f;
        public float InternalTemperature { get; set;  }= 293.15f;

        public bool canBreathAnywhere { get; set; }

        private TileManager tileManager;

        void Start()
        {
            bloodSystem = GetComponent<BloodSystem>();
            creatureHealth = GetComponent<CreatureHealth>();
            tileManager = FindObjectOfType<TileManager>();
        }

        void Update()
        {
            //Server Only:
            if (isServer && !canBreathAnywhere)
            {
                tick += Time.deltaTime;
                if (tick >= tickRate)
                {
                    tick = 0f;
                    MonitorSystem();
                }
            }
        }

        private void MonitorSystem()
        {
            if (!creatureHealth.IsDead)
            {
                TileObject tile = tileManager.GetTile(tileManager.GetPositionClosestTo(this.transform.position));
                AtmosObject atmos = tile.atmos;

                if (!IsEVACompatible())
                {
                    InternalTemperature = atmos.GetAtmosContainer().GetTemperature();
                    InternalPressure = atmos.GetAtmosContainer().GetPressure();
                    CheckPressureDamage();
                }
                else
                {
                    InternalPressure = 101.325f;
                    InternalTemperature = 293.15f;
                }

                if (creatureHealth.OverallHealth >= HealthThreshold.SoftCrit)
                {
                    Breathe(atmos);
                }
                else
                {
                    bloodSystem.OxygenDamage += 1;
                }
            }
        }

        private bool Breathe(AtmosObject atmos)
        {
            breatheCooldown--; //not timebased, but tickbased
            if (breatheCooldown > 0)
            {
                return false;
            }

            AtmosContainer breathGasMix = atmos.GetAtmosContainer();
            float oxygenUsed = HandleBreathing(breathGasMix, atmos.IsBreathable());

            if (oxygenUsed > 0)
            {
                atmos.RemoveGas(AtmosGasses.Oxygen, oxygenUsed);
                atmos.AddGas(AtmosGasses.CarbonDioxide, oxygenUsed);
            }

            return oxygenUsed > 0;
        }

        private AtmosContainer GetInternalGasMix()
        {
            // TODO: Check the player inventory for internals
            return null;
        }

        private float HandleBreathing(AtmosContainer breathGasMix, bool isBreathable)
        {
            // Human air consumption is 0.016 moles of oxygen per minute at rest
            float oxygenPressure = breathGasMix.GetPartialPressure(AtmosGasses.Oxygen);
            float consumption = 0.016f * (bloodSystem.HeartRate / 55f);
            float oxygenUsed = consumption * (tickRate / 60f); // Consumption per tick

            if (!isBreathable)
            {
                if (Random.value < 0.1)
                {
                    // TODO: Play gasping sound effect
                    //Chat.AddActionMsgToChat(gameObject, "You gasp for breath", $"{gameObject.name} gasps");
                }

                if (oxygenPressure > 0 && oxygenPressure < Gas.minOxygenPressureBreathing)
                {
                    float ratio = 1 - oxygenPressure / Gas.minOxygenPressureBreathing;
                    bloodSystem.OxygenDamage += 1 * ratio;
                    oxygenUsed = breathGasMix.GetGas(AtmosGasses.Oxygen) * ratio;
                }
                else
                {
                    bloodSystem.OxygenDamage += 1;
                }
                IsSuffocating = true;
            }
            else
            {
                IsSuffocating = false;
                bloodSystem.OxygenDamage -= 2.5f;
                breatheCooldown = 4;
            }
            return oxygenUsed;
        }

        private void CheckPressureDamage()
        {
            // Give damage to the creature if the pressure is too high or too low
            if (InternalPressure < Gas.minOxygenPressureBreathing)
            {
                ApplyDamage(Gas.LowPressureDamage, DamageType.Brute);
            }
            else if (InternalPressure > Gas.HazardHighPressure)
            {
                float damage = Mathf.Min(((InternalPressure / Gas.HazardHighPressure) - 1) * Gas.PressureDamageCoefficient,
                    Gas.HighPressureDamage);

                ApplyDamage(damage, DamageType.Brute);
            }
        }

        private bool IsEVACompatible()
        {
            // TODO: Check the head and outerwear of a player for valid EVA capable equipment

            return false;
        }

        private void ApplyDamage(float amount, DamageType damageType)
        {
            creatureHealth.ApplyDamage(null, amount, AttackType.Internal, damageType);
        }
    }
}