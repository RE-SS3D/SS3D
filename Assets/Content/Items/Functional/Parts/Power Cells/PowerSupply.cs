using System;
using UnityEngine;
using Mirror;

namespace SS3D.Engine.Inventory
{
    [Serializable]
    public struct PowerSupply
    {
	// current charge
        [SerializeField] [SyncVar] private int charge;
	// max charge possible
        [SerializeField] private int maxCharge;
	// the rate the battery charges
        [SerializeField] private int chargeRate;
	// if it charges by itself
        [SerializeField] private bool selfRecharge;

        public int Charge => charge;
        public int MaxCharge => maxCharge;
        public int ChargeRate => chargeRate;
        public bool SelfRecharge => selfRecharge;

        public PowerSupply(int charge, int maxCharge, int chargeRate, bool selfRecharge)
        {
            this.charge = charge;
            this.maxCharge = maxCharge;
            this.chargeRate = chargeRate;
            this.selfRecharge = selfRecharge;
        }

        public PowerSupply WithCharge(int ccharge)
        {
            return new PowerSupply(ccharge, maxCharge, chargeRate, selfRecharge);
        }
    }
}