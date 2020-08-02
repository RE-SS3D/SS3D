using Mirror;
using System.Collections;
using UnityEngine;

namespace SS3D.Engine.Health
{
    /// <summary>
    ///		Health Monitoring component for all Living entities
    ///     Monitors the state of the entities health on the server and acts accordingly
    /// </summary>
    public class HealthStateManager : NetworkBehaviour
    {
        //Cached members
        private float overallHealthCache;
        private ConsciousState consciousStateCache;
        private bool isSuffocatingCache;
        private float temperatureCache;
        private float pressureCache;
        private int heartRateCache;
        private float bloodLevelCache;
        private float oxygenDamageCache;
        private float toxinLevelCache;
        private bool isHuskCache;
        private int brainDamageCache;

        private CreatureHealth creatureHealth;
        float tickRate = 1f;
        float tick = 0f;
        bool init = false;

        private void Start()
        {
            creatureHealth = GetComponent<CreatureHealth>();
        }

        public override void OnStartServer()
        {
            InitServerCache();
            base.OnStartServer();
        }

        private void InitServerCache()
        {
            overallHealthCache = creatureHealth.OverallHealth;
            consciousStateCache = creatureHealth.ConsciousState;
            isSuffocatingCache = creatureHealth.respiratorySystem.IsSuffocating;
            temperatureCache = creatureHealth.respiratorySystem.InternalTemperature;
            pressureCache = creatureHealth.respiratorySystem.InternalPressure;

            UpdateBloodCaches();

            if (creatureHealth.brainSystem != null)
            {
                isHuskCache = creatureHealth.brainSystem.IsHuskServer;
                brainDamageCache = creatureHealth.brainSystem.BrainDamageAmt;
            }
            init = true;
        }

        private void UpdateBloodCaches()
        {
            heartRateCache = creatureHealth.bloodSystem.HeartRate;
            bloodLevelCache = creatureHealth.bloodSystem.BloodLevel;
            oxygenDamageCache = creatureHealth.bloodSystem.OxygenDamage;
            toxinLevelCache = creatureHealth.bloodSystem.ToxinLevel;
        }

        /// ---------------------------
        /// SYSTEM MONITOR
        /// ---------------------------
        private void Update()
        {
            if (isServer && init)
            {
                MonitorCrucialStats();
                tick += Time.deltaTime;
                if (tick > tickRate)
                {
                    tick = 0f;
                    MonitorNonCrucialStats();
                }
            }
        }

        // Monitoring stats that need to be updated straight away on client if there is any change
        [Server]
        void MonitorCrucialStats()
        {
            CheckOverallHealth();
            CheckRespiratoryHealth();
            CheckTemperature();
            CheckPressure();
            CheckCrucialBloodHealth();
            CheckConsciousState();
        }

        // Monitoring stats that don't need to be updated straight away on clients
        // (changes are updated at 1 second intervals)
        [Server]
        void MonitorNonCrucialStats()
        {
            CheckNonCrucialBloodHealth();
            if (creatureHealth.brainSystem != null)
            {
                CheckNonCrucialBrainHealth();
            }
        }

        void CheckConsciousState()
        {
            if (consciousStateCache != creatureHealth.ConsciousState)
            {
                consciousStateCache = creatureHealth.ConsciousState;
                SendConsciousUpdate();
            }
        }

        void CheckOverallHealth()
        {
            if (overallHealthCache != creatureHealth.OverallHealth)
            {
                overallHealthCache = creatureHealth.OverallHealth;
                SendOverallUpdate();
            }
        }

        void CheckRespiratoryHealth()
        {
            if (isSuffocatingCache != creatureHealth.respiratorySystem.IsSuffocating)
            {
                isSuffocatingCache = creatureHealth.respiratorySystem.IsSuffocating;
                SendRespiratoryUpdate();
            }
        }

        void CheckTemperature()
        {
            if (temperatureCache != creatureHealth.respiratorySystem.InternalTemperature)
            {
                temperatureCache = creatureHealth.respiratorySystem.InternalTemperature;
                SendTemperatureUpdate();
            }
        }

        void CheckPressure()
        {
            if (pressureCache != creatureHealth.respiratorySystem.InternalPressure)
            {
                pressureCache = creatureHealth.respiratorySystem.InternalPressure;
                SendPressureUpdate();
            }
        }

        void CheckCrucialBloodHealth()
        {
            if (toxinLevelCache != creatureHealth.bloodSystem.ToxinLevel ||
                heartRateCache != creatureHealth.bloodSystem.HeartRate)
            {
                UpdateBloodCaches();
                SendBloodUpdate();
            }
        }

        void CheckNonCrucialBloodHealth()
        {
            if (bloodLevelCache != creatureHealth.bloodSystem.BloodLevel ||
                oxygenDamageCache != creatureHealth.bloodSystem.OxygenDamage)
            {
                UpdateBloodCaches();
                SendBloodUpdate();
            }
        }

        void CheckNonCrucialBrainHealth()
        {
            if (isHuskCache != creatureHealth.brainSystem.IsHuskServer ||
                brainDamageCache != creatureHealth.brainSystem.BrainDamageAmt)
            {
                isHuskCache = creatureHealth.brainSystem.IsHuskServer;
                brainDamageCache = creatureHealth.brainSystem.BrainDamageAmt;
                SendBrainUpdate();
            }
        }

        /// ---------------------------
        /// SEND TO ALL SERVER --> CLIENT
        /// ---------------------------

        void SendConsciousUpdate()
        {
            HealthConsciousMessage.SendToAll(gameObject, creatureHealth.ConsciousState);
        }

        void SendOverallUpdate()
        {
            HealthOverallMessage.Send(gameObject, gameObject, creatureHealth.OverallHealth);
        }

        void SendBloodUpdate()
        {
            HealthBloodMessage.Send(gameObject, gameObject, heartRateCache, bloodLevelCache,
                oxygenDamageCache, toxinLevelCache);
        }

        void SendBrainUpdate()
        {
            if (creatureHealth.brainSystem != null)
            {
                HealthBrainMessage.SendToAll(gameObject, creatureHealth.brainSystem.IsHuskServer,
                    creatureHealth.brainSystem.BrainDamageAmt);
            }
        }

        /// ---------------------------
        /// SEND TO INDIVIDUAL CLIENT
        /// ---------------------------

        void SendOverallUpdate(GameObject requester)
        {
            HealthOverallMessage.Send(requester, gameObject, creatureHealth.OverallHealth);
        }

        void SendConsciousUpdate(GameObject requester)
        {
            HealthConsciousMessage.Send(requester, gameObject, creatureHealth.ConsciousState);
        }

        void SendBloodUpdate(GameObject requester)
        {
            HealthBloodMessage.Send(requester, gameObject, heartRateCache, bloodLevelCache,
                oxygenDamageCache, toxinLevelCache);
        }

        void SendRespiratoryUpdate()
        {
            HealthRespiratoryMessage.Send(gameObject, isSuffocatingCache);
        }

        void SendTemperatureUpdate()
        {
            HealthTemperatureMessage.Send(gameObject, temperatureCache);
        }

        void SendPressureUpdate()
        {
            HealthPressureMessage.Send(gameObject, pressureCache);
        }

        void SendBrainUpdate(GameObject requester)
        {
            if (creatureHealth.brainSystem != null)
            {
                HealthBrainMessage.Send(requester, gameObject, creatureHealth.brainSystem.IsHuskServer,
                    creatureHealth.brainSystem.BrainDamageAmt);
            }
        }

        /// ---------------------------
        /// CLIENT REQUESTS
        /// ---------------------------

        public void ProcessClientUpdateRequest(GameObject requester)
        {
            StartCoroutine(ControlledClientUpdate(requester));
            Logger.Log("Server received a request for health update from: " + requester.name + " for: " + gameObject.name);
        }

        /// <summary>
        /// This is mainly used to update new Clients on connect.
        /// So we do not spam too many net messages at once for a direct
        /// client update, control the rate of update slowly:
        /// </summary>
        IEnumerator ControlledClientUpdate(GameObject requester)
        {
            SendConsciousUpdate(requester);

            yield return WaitFor.Seconds(.1f);

            SendOverallUpdate(requester);

            yield return WaitFor.Seconds(.1f);

            SendBloodUpdate(requester);

            yield return WaitFor.Seconds(.1f);

            SendRespiratoryUpdate();

            yield return WaitFor.Seconds(.1f);

            SendTemperatureUpdate();

            yield return WaitFor.Seconds(.1f);

            SendPressureUpdate();

            yield return WaitFor.Seconds(.1f);

            if (creatureHealth.brainSystem != null)
            {
                SendBrainUpdate(requester);
                yield return WaitFor.Seconds(.1f);
            }

            for (int i = 0; i < creatureHealth.BodyParts.Count; i++)
            {
                HealthBodyPartMessage.Send(requester, gameObject,
                    creatureHealth.BodyParts[i].Type,
                    creatureHealth.BodyParts[i].BruteDamage,
                    creatureHealth.BodyParts[i].BurnDamage);
                yield return WaitFor.Seconds(.1f);
            }
        }
    }
}