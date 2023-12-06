using SS3D.Core;
using SS3D.Systems.Tile;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace System.Electricity
{
    public class BasicPowerGenerator : BasicElectricDevice, IPowerProducer
    {
        [SerializeField]
        private float _powerProduction = 10f;
        public float PowerProduction => _powerProduction;
    }
}
