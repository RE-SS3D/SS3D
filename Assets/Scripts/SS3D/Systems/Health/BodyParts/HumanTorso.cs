using SS3D.Data;
using SS3D.Data.Enums;
using SS3D.Systems;
using SS3D.Systems.Health;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HumanTorso : BodyPart
{
    public Heart heart;
    public Lungs leftLung;
    public Lungs rightLung;

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
        AddInternalBodyPart(heart);
        AddInternalBodyPart(leftLung);
        AddInternalBodyPart(rightLung);
    }

    protected override void SpawnOrgans()
    {
        GameObject heartPrefab = Assets.Get<GameObject>((int)AssetDatabases.BodyParts, (int)BodyPartsIds.HumanHeart);
        GameObject leftLungPrefab = Assets.Get<GameObject>((int)AssetDatabases.BodyParts, (int)BodyPartsIds.HumanLungLeft);
        GameObject rightLungPrefab = Assets.Get<GameObject>((int)AssetDatabases.BodyParts, (int)BodyPartsIds.HumanLungRight);

        GameObject heartGameObject = Instantiate(heartPrefab);
        GameObject leftLungGameObject = Instantiate(leftLungPrefab);
        GameObject rightLungGameObject = Instantiate(rightLungPrefab);

        heart = heartGameObject.GetComponent<Heart>();
        leftLung = leftLungGameObject.GetComponent<Lungs>();
        rightLung = rightLungGameObject.gameObject.GetComponent<Lungs>();

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

    protected override void AfterSpawningCopiedBodyPart()
    {
        return;
    }

    protected override void BeforeDestroyingBodyPart()
    {
        return;
    }
}
