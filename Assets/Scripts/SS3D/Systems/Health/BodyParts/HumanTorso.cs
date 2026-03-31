using SS3D.Data;
using SS3D.Data.Generated;
using SS3D.Data.Networking;
using SS3D.Systems.Health;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

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

    protected override async void SpawnOrgans()
    {
        Task<AssetHandle<Heart>> loadHeartTask = new AssetRequest<Heart>(Items.HumanHeart).LoadAsync();
        Task<AssetHandle<Lungs>> loadLeftLungTask = new AssetRequest<Lungs>(Items.HumanLungLeft).LoadAsync();
        Task<AssetHandle<Lungs>> loadRightTask = new AssetRequest<Lungs>(Items.HumanLungRight).LoadAsync();
        
        await Task.WhenAll(loadHeartTask, loadLeftLungTask, loadRightTask);
        
        AssetHandle<Heart> heartHandle = loadHeartTask.Result;
        AssetHandle<Lungs> leftLungHandle = loadLeftLungTask.Result;
        AssetHandle<Lungs> rightLungHandle = loadRightTask.Result;

        List<Task> spawnTasks = new();
        if (heartHandle)
        {
            Heart = Instantiate(heartHandle.Asset);
            spawnTasks.Add(NetworkSpawner.SpawnAsync(Heart, Items.HumanHeart, Owner));
        }

        if (leftLungHandle)
        {
            LeftLung = Instantiate(leftLungHandle.Asset);
            spawnTasks.Add(NetworkSpawner.SpawnAsync(LeftLung, Items.HumanLungLeft, Owner));
        }

        if (rightLungHandle)
        {
            RightLung = Instantiate(rightLungHandle.Asset);
            spawnTasks.Add(NetworkSpawner.SpawnAsync(RightLung, Items.HumanLungRight, Owner));
        }
        
        await Task.WhenAll(spawnTasks);
        
        heartHandle.Dispose();
        leftLungHandle.Dispose();
        rightLungHandle.Dispose();
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
