using Mirror;
using UnityEngine;

namespace SS3D.Engine.Health
{
    /// <summary>
    /// Handles the Brain System for this creature
    /// Updated Server Side and state is sent to clients
    /// Holds the brain for this entity
    /// </summary>
    public class BrainSystem : NetworkBehaviour
    {
        //The brain, Only used on the server
        private Brain brain;
        private BloodSystem bloodSystem;
        private RespiratorySystem respiratorySystem;
        private CreatureHealth creatureHealth;

        /// <summary>
        /// Is this body just a husk (missing brain)
        /// </summary>
        public bool IsHuskServer => brain == null;
        public bool IsHuskClient { get; private set; }

        /// <summary>
        /// How damaged is the brain
        /// </summary>
        /// <returns>Percentage between 0% and 100%.
        /// -1 means there is no brain present</returns>
        public int BrainDamageAmt { get { if (brain == null) { return -1; } return Mathf.Clamp(brain.BrainDamage, 0, 101); } }
        public int BrainDamageAmtClient { get; private set; }

        private float tickRate = 1f;
        private float tick = 0f;
        private bool init = false;

        void Start()
        {
            InitSystem();
        }

        void InitSystem()
        {
            bloodSystem = GetComponent<BloodSystem>();
            respiratorySystem = GetComponent<RespiratorySystem>();
            creatureHealth = GetComponent<CreatureHealth>();

            //Server only
            if (isServer)
            {
                //Spawn a brain and connect the brain to this living entity
                brain = new Brain();
                brain.ConnectBrainToBody(gameObject);
                init = true;
            }
        }

        void Update()
        {
            if (!init)
            {
                return;
            }
            //Server Only:
            if (isServer)
            {
                tick += Time.deltaTime;
                if (tick >= tickRate)
                {
                    tick = 0f;
                    CheckOverallDamage();
                }
            }
        }


        void CheckOverallDamage()
        {
            if (bloodSystem.OxygenDamage > 200)
            {
                if (!creatureHealth.IsDead)
                {
                    creatureHealth.Death();
                }
            }
        }

        /// <summary>
        /// Updated via server NetMsg
        /// </summary>
        public void UpdateClientBrainStats(bool isHusk, int brainDmgAmt)
        {
            if (isServer)
            {
                return;
            }
            IsHuskClient = isHusk;
            BrainDamageAmtClient = brainDmgAmt;
        }
    }
}