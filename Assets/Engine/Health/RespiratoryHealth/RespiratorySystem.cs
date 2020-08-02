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
                // TODO: Get current tile from player

                //TileObject currentTile = tileManager.GetPositionClosestTo()

                //if (!IsEVACompatible())
                //{
                //    temperature = atmos.GasMix.Temperature;
                //    pressure = atmos.GasMix.Pressure;
                //    CheckPressureDamage();
                //}
                //else
                //{
                //    InternalPressure = 101.325f;
                //    InternalTemperature = 293.15f;
                //}

                //if (creatureHealth.OverallHealth >= HealthThreshold.SoftCrit)
                //{
                //    if (Breathe(atmos))
                //    {
                //        AtmosManager.Update(atmos);
                //    }
                //}
                //else
                //{
                //    bloodSystem.OxygenDamage += 1;
                //}
            }
        }

        private bool Breathe(AtmosObject atmos)
        {
            breatheCooldown--; //not timebased, but tickbased
            if (breatheCooldown > 0)
            {
                return false;
            }

            // if no internal breathing is possible, get the air from the surroundings
            AtmosContainer container = atmos.GetAtmosContainer();

            //    GasMix gasMix = container.GasMix;
            //    GasMix breathGasMix = gasMix.RemoveVolume(AtmosConstants.BREATH_VOLUME, true);

            //    float oxygenUsed = HandleBreathing(breathGasMix);

            //    if (oxygenUsed > 0)
            //    {
            //        breathGasMix.RemoveGas(Gas.Oxygen, oxygenUsed);
            //        breathGasMix.AddGas(Gas.CarbonDioxide, oxygenUsed);
            //    }

            //    gasMix += breathGasMix;
            //    container.GasMix = gasMix;

            //    return oxygenUsed > 0;
            return true;
        }

        private AtmosContainer GetInternalGasMix()
        {
            // TODO: Check the player inventory for internals
            return null;
        }

        private float HandleBreathing(AtmosContainer breathGasMix)
        {
            float oxygenPressure = breathGasMix.GetPartialPressure(AtmosGasses.Oxygen);

            float oxygenUsed = 0;

            //if (oxygenPressure < OXYGEN_SAFE_MIN)
            //{
            //    if (Random.value < 0.1)
            //    {
            //        // TODO: Play gasping sound effect
            //        //Chat.AddActionMsgToChat(gameObject, "You gasp for breath", $"{gameObject.name} gasps");
            //    }

            //    if (oxygenPressure > 0)
            //    {
            //        float ratio = 1 - oxygenPressure / OXYGEN_SAFE_MIN;
            //        bloodSystem.OxygenDamage += 1 * ratio;
            //        oxygenUsed = breathGasMix.GetMoles(Gas.Oxygen) * ratio;
            //    }
            //    else
            //    {
            //        bloodSystem.OxygenDamage += 1;
            //    }
            //    IsSuffocating = true;
            //}
            //else
            //{
            //    oxygenUsed = breathGasMix.GetMoles(Gas.Oxygen);
            //    IsSuffocating = false;
            //    bloodSystem.OxygenDamage -= 2.5f;
            //    breatheCooldown = 4;
            //}
            return oxygenUsed;
        }

        private void CheckPressureDamage()
        {
            // TODO: Give damage to the player if the pressure is too high
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