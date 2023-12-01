using SS3D.Core;
using SS3D.Systems.Tile;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace System.Electricity
{
    public class BasicPowerGenerator : MonoBehaviour, IPowerProducer
    {
        [SerializeField]
        private float _powerProduction = 10f;
        public float PowerProduction => _powerProduction;

        public PlacedTileObject TileObject => GetComponent<PlacedTileObject>();

        void Start()
        {
            ElectricitySystem electricitySystem = Subsystems.Get<ElectricitySystem>();
            electricitySystem.AddElectricalElement(this);
        }

        void Destroy()
        {
            ElectricitySystem electricitySystem = Subsystems.Get<ElectricitySystem>();
            electricitySystem.RemoveElectricalElement(this);
        }
    }
}
