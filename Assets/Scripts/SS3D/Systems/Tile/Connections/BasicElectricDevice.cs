using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Tile;
using System.Collections;
using System.Collections.Generic;
using System.Electricity;
using UnityEngine;

public class BasicElectricDevice : NetworkActor, IElectricDevice
{
    public PlacedTileObject TileObject => gameObject.GetComponent<PlacedTileObject>();

    void Start()
    {
        ElectricitySystem electricitySystem = Subsystems.Get<ElectricitySystem>();
        electricitySystem.AddElectricalElement(this);
    }

    protected override void OnDestroyed()
    {
        base.OnDestroyed();
        ElectricitySystem electricitySystem = Subsystems.Get<ElectricitySystem>();
        electricitySystem.RemoveElectricalElement(this);
    }
}
