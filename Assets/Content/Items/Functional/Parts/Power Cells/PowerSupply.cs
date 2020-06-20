using System;
using UnityEngine;
using Mirror;

namespace SS3D.Engine.Inventory
{
    [Serializable]
    public struct PowerSupply
    {
        [SerializeField] [SyncVar] private int charge;
        [SerializeField] private int maxCharge;
        [SerializeField] private int chargeRate;
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