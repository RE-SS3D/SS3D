using System.Collections;
using SS3D.Content.Furniture.Storage;
using SS3D.Engine.Inventory;
using UnityEngine;
using Mirror;
using UnityEngine.Assertions;

// This handles the cell charger, responsable for exclusivery recharging batteries
public class CellCharger : NetworkBehaviour
{
    // Visual container for the cell to be put in
    public AttachedContainer AttachedContainer;
    public MeshRenderer wiresMeshRenderer;
    [SerializeField]private Material emissiveMaterial;
    [SerializeField]private Texture offEmissiveMask;
    [SerializeField]private Texture lowEmissiveMask;
    [SerializeField]private Texture midLowEmissiveMask;
    [SerializeField]private Texture midHighEmissiveMask;
    [SerializeField]private Texture highEmissiveMask;
    private int emissionMap = Shader.PropertyToID("_EmissionMap");

    private void Start()
    {
        Assert.IsNotNull(AttachedContainer);

        AttachedContainer.Container.ContentsChanged += (_, items, type) =>
        {
            wiresMeshRenderer.enabled = !AttachedContainer.Container.Empty;
            if (AttachedContainer.Container.Empty)
            {
                emissiveMaterial.SetTexture(emissionMap, offEmissiveMask);
            }
        };
        if (isServer)
        {
            StartCoroutine(StartCharge());
        }
    }

    IEnumerator StartCharge()
    {
        while (true)
        {
            yield return new WaitForSeconds(2f);
            Recharge();
        }
    }

    // Recharges the current cell that is placed on it
    private void Recharge()
    {
        foreach (Item item in AttachedContainer.Container.Items)
        {
            IChargeable chargeable = item.GetComponent<IChargeable>();
            if (chargeable != null)
            {
                
                chargeable.AddCharge(chargeable.GetChargeRate());
                UpdateEmissiveMask(chargeable.GetPowerPercentage());
            }
        }
    }
    private void UpdateEmissiveMask(float powerPercentage) 
    {
        if (-1f <= powerPercentage && powerPercentage < .25f)
        {
            emissiveMaterial.SetTexture(emissionMap, lowEmissiveMask);       
        }
        else if (.25 <= powerPercentage && powerPercentage < .50f)
        {
            emissiveMaterial.SetTexture(emissionMap, midLowEmissiveMask);
        }
        else if (.50 <= powerPercentage && powerPercentage < .75f)
        {
            emissiveMaterial.SetTexture(emissionMap, midHighEmissiveMask);
        }
        else 
        {
            emissiveMaterial.SetTexture(emissionMap, highEmissiveMask);
        }
    }
}