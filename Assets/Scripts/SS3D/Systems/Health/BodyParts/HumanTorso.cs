using SS3D.Data.Generated;
using SS3D.Systems.Entities;
using SS3D.Systems.Health;
using System.Collections;
using UnityEngine;

public class HumanTorso : BodyPart
{
    public Heart Heart;
    public Lungs LeftLung;
    public Lungs RightLung;

    public override void OnStartServer()
    {
        base.OnStartServer();
        SpawnOrgans();
        StartCoroutine(AddInternalOrgans());
    }

    /// <summary>
    /// Add specific torso internal organs, heart, lungs, and more to come..
    /// Need to do it with a delay to prevent some Unity bug since OnStartServer() is called Before Start();
    /// </summary>
    private IEnumerator AddInternalOrgans()
    {
        yield return null;
        AddInternalBodyPart(Heart);
        AddInternalBodyPart(LeftLung);
        AddInternalBodyPart(RightLung);
    }

    protected override void SpawnOrgans()
    {
        GameObject heartPrefab = Items.HumanHeart;
        GameObject leftLungPrefab = Items.HumanLungLeft;
        GameObject rightLungPrefab = Items.HumanLungRight;;
        
        GameObject heartGameObject = Instantiate(heartPrefab);
        GameObject leftLungGameObject = Instantiate(leftLungPrefab);
        GameObject rightLungGameObject = Instantiate(rightLungPrefab);
        
        Heart = heartGameObject.GetComponent<Heart>();
        LeftLung = leftLungGameObject.GetComponent<Lungs>();
        RightLung = rightLungGameObject.gameObject.GetComponent<Lungs>();
        
        Spawn(heartGameObject, Owner);
        Spawn(leftLungGameObject, Owner);
        Spawn(rightLungGameObject, Owner);
    }

    protected override void AddInitialLayers()
    {
        TryAddBodyLayer(new MuscleLayer(this));
        TryAddBodyLayer(new BoneLayer(this));
        TryAddBodyLayer(new CirculatoryLayer(this, 8f));
        TryAddBodyLayer(new NerveLayer(this));

        InvokeOnBodyPartLayerAdded();
    }
    
    protected override void InflictDamage(BodyLayer layer, DamageTypeQuantity damageTypeQuantity)
    {
        layer.InflictDamage(damageTypeQuantity);
        if (IsDestroyed)
        {
            DestroyBodyPart();
        }
        else if (IsSevered && !_isDetached)
        {
            GetComponentInParent<Entity>().Kill();
        }
    }

    protected override void AfterSpawningCopiedBodyPart() { }

    protected override void BeforeDestroyingBodyPart() { }
}
