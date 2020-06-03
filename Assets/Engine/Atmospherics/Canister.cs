using SS3D.Engine.Atmospherics;
using SS3D.Engine.Tiles;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Canister : MonoBehaviour
{
    AtmosObject currentAtmosObject;
    [SerializeField] AtmosGasses gas = AtmosGasses.Oxygen;
    [Range(0f, 20f)]
    [SerializeField] float valvePressure = 1;

    [SerializeField] float content;
    [SerializeField] float maxContent;

    private void Start()
    {
        currentAtmosObject = transform.GetComponentInParent<TileObject>().atmos; 
    }

    private void FixedUpdate()
    {
        if (currentAtmosObject != null && content - valvePressure > 0 && valvePressure > 0)
        {
            currentAtmosObject.AddGas(gas, valvePressure);
            content -= valvePressure/10;
        }
    }
}
