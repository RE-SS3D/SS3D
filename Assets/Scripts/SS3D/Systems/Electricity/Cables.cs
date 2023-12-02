using SS3D.Core;
using SS3D.Systems.Tile;
using System.Collections;
using System.Collections.Generic;
using System.Electricity;
using UnityEngine;

public class Cables : MonoBehaviour, IElectricDevice
{
    public PlacedTileObject TileObject => GetComponent<PlacedTileObject>();

    // Start is called before the first frame update
    void Start()
    {
        ElectricitySystem electricitySystem = Subsystems.Get<ElectricitySystem>();
        electricitySystem.AddElectricalElement(this);
    }

    void OnDestroy()
    {
        ElectricitySystem electricitySystem = Subsystems.Get<ElectricitySystem>();
        electricitySystem.RemoveElectricalElement(this);
    }
}
