using SS3D.Data;
using SS3D.Data.Generated;
using SS3D.Systems.Health;
using System.Collections;

public class HumanTorso : BodyPart
{
    public Heart Heart;
    public Lungs LeftLung;
    public Lungs RightLung;

    protected override bool IsDetachable => false;

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
        Heart heartPrefab = Assets.Get<Heart>(AssetDatabases.Items, Items.HumanHeart);
        Lungs leftLungPrefab = Assets.Get<Lungs>(AssetDatabases.Items, Items.HumanLungLeft);
        Lungs rightLungPrefab = Assets.Get<Lungs>(AssetDatabases.Items, Items.HumanLungRight);

        Heart = Instantiate(heartPrefab);
        LeftLung = Instantiate(leftLungPrefab);
        RightLung = Instantiate(rightLungPrefab);

        Spawn(Heart.GameObject, Owner);
        Spawn(LeftLung.GameObject, Owner);
        Spawn(RightLung.GameObject, Owner);
    }

    protected override void AddInitialLayers()
    {
        TryAddBodyLayer(new MuscleLayer(this));
        TryAddBodyLayer(new BoneLayer(this));
        TryAddBodyLayer(new CirculatoryLayer(this, 8f));
        TryAddBodyLayer(new NerveLayer(this));

        InvokeOnBodyPartLayerAdded();
    }

    protected override void AfterSpawningCopiedBodyPart() { }

    protected override void BeforeDestroyingBodyPart() { }
}
