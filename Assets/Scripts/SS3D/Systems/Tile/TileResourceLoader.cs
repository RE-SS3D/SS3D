using JetBrains.Annotations;
using SS3D.Logging;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// Loads assets used by the tilemap. Can be used to retrieve scriptableobjects from a name string.
    /// </summary>
    public sealed class TileResourceLoader: MonoBehaviour
    {
        public Sprite _missingIcon;

        public bool IsInitialized { get; private set; } = false;

        public List<GenericObjectSo> Assets { get; private set; }

        public void Start()
        {
            LoadAssets();
        }

        private void LoadAssets()
        {
            Assets = new();

            GenericObjectSo[] tempAssets = Resources.LoadAll<GenericObjectSo>("");
            StartCoroutine(LoadAssetsWithIcon(tempAssets));
        }

        private IEnumerator LoadAssetsWithIcon(GenericObjectSo[] assets)
        {
	        List<Texture2D> tempIcons = new List<Texture2D>();
	        RuntimePreviewGenerator.OrthographicMode = true;

	        foreach (GenericObjectSo asset in assets)
	        {
		        Transform prefabTransform = asset.prefab.transform;
		        Shader shader = Shader.Find("Unlit/ObjectIcon");

		        Texture2D texture = RuntimePreviewGenerator.GenerateModelPreviewWithShader(prefabTransform, shader, null, 128, 128, true, true);

		        tempIcons.Add(texture);
	        }

	        for (int i = 0; i < assets.Length; i++)
	        {
		        if (tempIcons[i] != null)
		        {
			        assets[i].icon = Sprite.Create(tempIcons[i], new Rect(0, 0, tempIcons[i].width, tempIcons[i].height), new Vector2(0.5f, 0.5f));
		        }
		        else
		        {
			        assets[i].icon = _missingIcon;
		        }

		        Assets.Add(assets[i]);
	        }

	        IsInitialized = true;

	        yield return null;
        }

        [CanBeNull]
        public GenericObjectSo GetAsset(string assetName)
        {
            GenericObjectSo genericObjectSo = Assets.FirstOrDefault(tileObject => tileObject.nameString == assetName);
            
            if (genericObjectSo == null)
            {
	            Log.Warning(this, "Requested tile asset {assetName} was not found.", Logs.Generic, assetName);
            }

            return genericObjectSo;
        }
    }
}