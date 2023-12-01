using FishNet.Object;
using SS3D.Systems.Tile;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace System.Electricity
{
    public class BasicBattery : NetworkBehaviour, IPowerStorage
    {
        private float _maxCapacity = 10;

        private float _storedPower = 0;
        public float StoredPower { get => _storedPower; set => _storedPower = value >= 0 ? MathF.Max(MaxCapacity, value) : MathF.Max(0f, value); }

        public float MaxCapacity => _maxCapacity;

        public float RemainingCapacity => _maxCapacity - _storedPower;

        public PlacedTileObject TileObject => GetComponent<PlacedTileObject>();
    }
}
