using System.Collections;
using SS3D.Content.Furniture.Storage;
using SS3D.Engine.Inventory;
using UnityEngine;
using Mirror;
using UnityEngine.Assertions;

public class CellCharger : NetworkBehaviour
{
    public AttachedContainer AttachedContainer;
    
    private StorageContainer storageContainer;
    private Item powerCell;
    public MeshRenderer renderer;

    private void Start()
    {
        Assert.IsNotNull(AttachedContainer);

        AttachedContainer.Container.ContentsChanged += (_, items, type) =>
        {
            renderer.enabled = !AttachedContainer.Container.Empty;
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

    private void Recharge()
    {
        foreach (Item item in AttachedContainer.Container.Items)
        {
            IChargeable chargeable = item.GetComponent<IChargeable>();
            if (chargeable != null)
            {
                chargeable.AddCharge(chargeable.GetChargeRate());
            }
            
        }
    }
}