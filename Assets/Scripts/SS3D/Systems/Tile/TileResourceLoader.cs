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
    public class TileResourceLoader: MonoBehaviour
    {
        public Sprite _missingIcon;

        public bool IsInitialized { get; private set; } = false;

        private List<GenericObjectSo> _assets;

        public void Start()
        {
            LoadAssets();
        }

        private void LoadAssets()
        {
            _assets = new List<GenericObjectSo>();

            GenericObjectSo[] tempAssets = Resources.LoadAll<GenericObjectSo>("");
            StartCoroutine(LoadAssetsWithIcon(tempAssets));
            //LoadAssetsWithIcon(tempAssets);
        }

        private IEnumerator LoadAssetsWithIcon(GenericObjectSo[] assets)
        {
            List<Texture2D> tempIcons = new List<Texture2D>();
            
            foreach (var asset in assets)
            {
                Texture2D texture = RuntimePreviewGenerator.GenerateModelPreviewWithShader(asset.prefab.transform,
                    Shader.Find("Legacy Shaders/Diffuse"), null, 128, 128, true, true);

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
                _assets.Add(assets[i]);
            }
            IsInitialized = true;
            yield return null;
        }

        

        public GenericObjectSo GetAsset(string assetName)
        {
            GenericObjectSo genericObjectSo = _assets.FirstOrDefault(tileObject => tileObject.nameString == assetName);
            if (genericObjectSo == null)
                Punpun.Warning(this, "Requested tile asset {assetName} was not found.", Logs.Generic, assetName);

            return genericObjectSo;
        }

        public List<GenericObjectSo> GetAllAssets()
        {
            return _assets;
        }
    }
}