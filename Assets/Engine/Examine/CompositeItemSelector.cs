using System;
using System.Collections.Generic;
using SS3D.Engine.Tiles;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;


namespace SS3D.Engine.Examine
{
    /// <summary>
    /// For composite GameObjects composed of multiple Examinable GameObjects, this script allows
    /// us to return specifically which subordinate GameObject the cursor is over.
    /// </summary>
    public class CompositeItemSelector : MonoBehaviour
    {
        public event ExaminableChangedHandler ExaminableChanged;

        public delegate void ExaminableChangedHandler(GameObject examinable);

        // The currently hovered examinable
        public GameObject CurrentExaminable
        {
            get => currentExaminable;
            private set
            {
                GameObject oldExaminable = currentExaminable;
                currentExaminable = value;
                if (oldExaminable != currentExaminable)
                {
                    OnExaminableChanged();
                }
            }
        }

        // The texture the GPU renders to
        private RenderTexture renderTexture;

        // The texture that will store the data we extract
        private Texture2D readbackTexture;

        // Keeps track of examinable objects
        private ExaminableIdentifiers identifiers;

        // Indicate whether mouse is currently over the UI
        private bool mouseOverUI;

        // Confirms whether we need to render the image during OnPostRender.
        private bool shouldRender;
        private bool isRendering;
        private RaycastHit[] raycastHits;
        private GameObject currentExaminable;

        public void Start()
        {
            identifiers = new ExaminableIdentifiers();
            raycastHits = new RaycastHit[32];
            FitRenderTexture();
            readbackTexture = new Texture2D(1, 1, renderTexture.graphicsFormat, TextureCreationFlags.None);
            Camera.onPreRender += OnAnyPreRender;
        }

        private void OnDestroy()
        {
            Camera.onPreRender -= OnAnyPreRender;
            renderTexture.Release();
        }

        private void OnAnyPreRender(Camera cam)
        {
            if (isRendering || !shouldRender || mouseOverUI)
            {
                return;
            }

            if (cam != CameraManager.singleton.playerCamera)
            {
                return;
            }

            FitRenderTexture();

            int mouseX = (int) Input.mousePosition.x;
            int mouseY = (int) Input.mousePosition.y;

            if (mouseX < 0 || mouseY < 0 || mouseX + 1 > renderTexture.width ||
                mouseY + 1 > renderTexture.height)
            {
                return;
            }

            isRendering = true;

            var buffer = new CommandBuffer();
            buffer.SetViewMatrix(cam.worldToCameraMatrix);
            buffer.SetProjectionMatrix(cam.projectionMatrix);
            buffer.SetRenderTarget(new RenderTargetIdentifier(renderTexture));
            buffer.ClearRenderTarget(true, true, Color.clear);
            identifiers.AddCommands(buffer);
            buffer.RequestAsyncReadback(renderTexture, 0, mouseX, 1,
                mouseY, 1, 0, 1, OnCompleteReadback);

            Graphics.ExecuteCommandBuffer(buffer);
        }

        private void OnCompleteReadback(AsyncGPUReadbackRequest request)
        {
            try
            {
                if (request.hasError)
                {
                    Debug.LogError("Error in examine GPU readback");
                    return;
                }

                shouldRender = false;

                readbackTexture.LoadRawTextureData(request.GetData<byte>());
                readbackTexture.Apply();

                Color pixel = readbackTexture.GetPixel(0, 0);
                if (pixel.a < 1)
                {
                    CurrentExaminable = null;
                    return;
                }

                CurrentExaminable = identifiers.FindExaminable(pixel);
            }
            finally
            {
                isRendering = false;
            }
        }

        private void FitRenderTexture()
        {
            int width = Screen.width;
            int height = Screen.height;

            bool recreate = false;
            if (renderTexture && (renderTexture.width != width || renderTexture.height != height))
            {
                renderTexture.Release();
                recreate = true;
            }

            if (!renderTexture || recreate)
            {
                renderTexture = new RenderTexture(width, height, 16);
            }
        }

        public void CalculateSelectedGameObject()
        {
            // Ensure we don't update information while still processing a frame
            if (isRendering)
            {
                return;
            }
            
            // Identify if mouse is over the user interface, or over objects in the game world.
            if (EventSystem.current.IsPointerOverGameObject())
            {
                // The cursor is over the UI. If the UI is examinable, this will override objects in the game world.
                mouseOverUI = true;
                CurrentExaminable = null;

                // Get a list of all the UI elements under the cursor
                var pointerEventData = new PointerEventData(EventSystem.current) {position = Input.mousePosition};
                List<RaycastResult> UIhits = new List<RaycastResult>();
                EventSystem.current.RaycastAll(pointerEventData, UIhits);

                // Get the UI to give us the GameObject that a slot is displaying.
                foreach (var hit in UIhits)
                {
                    ISlotProvider slot = hit.gameObject.GetComponent<ISlotProvider>();
                    if (slot != null)
                    {
                        CurrentExaminable = slot.GetCurrentGameObjectInSlot();
                        return;
                    }
                }

                return;
            }

            mouseOverUI = false;
            identifiers.Clear();

            // Raycast to cursor position. Need to get all possible hits, because the initial hit may have gaps through which we can see other Examinables
            Ray ray = CameraManager.singleton.playerCamera.ScreenPointToRay(new Vector2(Input.mousePosition.x,
                Input.mousePosition.y));
            int rayCount = Physics.SphereCastNonAlloc(ray, 0.1f, raycastHits, 100f);

            // Record examinables and meshes for each hit object
            for (int i = 0; i < rayCount; i++)
            {
                GameObject hitObject = raycastHits[i].transform.gameObject;
                if (identifiers.HasGameObject(hitObject))
                {
                    continue;
                }
                // Check if we hit a tile
                var tileObject = hitObject.GetComponent<PlacedTileObject>();
                if (tileObject)
                {
                    // Add examinables for all tile objects
                    AddExaminablesForTile(tileObject);
                    continue;
                }
                AddExaminablesRecursive(hitObject);
            }

            // We want to render at the next opportunity
            shouldRender = true;
        }

        private void AddExaminablesRecursive(GameObject gameObject, IExaminable current = null)
        {
            if (!gameObject.activeInHierarchy)
            {
                return;
            }

            IExaminable old = current;
            current = gameObject.GetComponent<IExaminable>();

            if (current == null)
            {
                current = old;
            }
            else if (old != current)
            {
                GameObject examinable = ((MonoBehaviour) current).gameObject;
                if (identifiers.HasExaminable(examinable))
                {
                    return;
                }

                identifiers.AddExaminable(examinable);
            }

            if (current != null)
            {
                var meshFilter = gameObject.GetComponent<MeshFilter>();
                if (meshFilter)
                {
                    identifiers.AddMesh(meshFilter.sharedMesh, gameObject, ((MonoBehaviour) current).gameObject);
                }
                else
                {
                    var meshRenderer = gameObject.GetComponent<SkinnedMeshRenderer>();
                    if (meshRenderer)
                    {
                        var mesh = new Mesh();
                        meshRenderer.BakeMesh(mesh);
                        identifiers.AddMesh(mesh, gameObject, ((MonoBehaviour) current).gameObject);
                    }
                }
            }

            Transform objectTransform = gameObject.transform;
            int childCount = objectTransform.childCount;
            for (var i = 0; i < childCount; i++)
            {
                AddExaminablesRecursive(objectTransform.GetChild(i).gameObject, current);
            }
        }

        private void AddExaminablesForTile(PlacedTileObject tile)
        {
            // Get the chunk and chunk position for the tile
            var tileMap = tile.GetComponentInParent<TileMap>();
            Vector3 worldPosition = tile.transform.position;
            Vector2Int chunkKey = tileMap.GetKey(worldPosition);
            TileChunk tileChunk = tileMap.GetChunk(chunkKey);
            Vector2Int tileOffset = tileChunk.GetXY(worldPosition);
            // Get the placed objects for all tile layers and add their examinables
            TileLayer[] tileLayers = TileHelper.GetTileLayers();
            foreach (TileLayer layer in tileLayers)
            {
                TileObject tileObject = tileChunk.GetTileObject(layer, tileOffset.x, tileOffset.y);
                if (tileObject == null)
                {
                    continue;
                }
                PlacedTileObject[] placedObjects = tileObject.GetAllPlacedObjects();
                foreach (PlacedTileObject placedObject in placedObjects)
                {
                    if (placedObject)
                    {
                        AddExaminablesRecursive(placedObject.gameObject);
                    }
                }
            }
        }

        protected virtual void OnExaminableChanged()
        {
            ExaminableChanged?.Invoke(currentExaminable);
        }

        class ExaminableIdentifiers
        {
            private const byte ColorDistance = 10;
            private static readonly int ShaderColorId = Shader.PropertyToID("_Color");

            private readonly Material material = new Material(Shader.Find("Unlit/Color"));
            private readonly Dictionary<Color32, GameObject> examinables = new Dictionary<Color32, GameObject>();

            private readonly Dictionary<GameObject, MaterialPropertyBlock> materialProperties =
                new Dictionary<GameObject, MaterialPropertyBlock>();

            private readonly List<Tuple<Mesh, GameObject>> meshes = new List<Tuple<Mesh, GameObject>>();
            private readonly List<Color32> colors = new List<Color32>();
            private readonly List<MaterialPropertyBlock> propertyBlocks = new List<MaterialPropertyBlock>();


            public void Clear()
            {
                examinables.Clear();
                materialProperties.Clear();
                meshes.Clear();
            }

            public bool HasExaminable(GameObject examinable)
            {
                return examinables.ContainsValue(examinable);
            }

            public bool HasGameObject(GameObject gameObject)
            {
                if (HasExaminable(gameObject))
                {
                    return true;
                }

                foreach (Tuple<Mesh, GameObject> pair in meshes)
                {
                    if (gameObject == pair.Item2)
                    {
                        return true;
                    }
                }

                return false;
            }

            public void AddExaminable(GameObject examinable)
            {
                if (HasExaminable(examinable))
                {
                    return;
                }

                Color color;
                if (examinables.Count >= colors.Count)
                {
                    color = AllocateColor();
                    MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
                    propertyBlock.SetColor(ShaderColorId, color);
                    propertyBlocks.Add(propertyBlock);
                }
                else
                {
                    color = colors[examinables.Count];
                }

                examinables.Add(color, examinable);
            }

            public GameObject FindExaminable(Color color)
            {
                examinables.TryGetValue(color, out GameObject value);
                return value;
            }

            public void AddMesh(Mesh mesh, GameObject obj, GameObject examinable)
            {
                var tuple = new Tuple<Mesh, GameObject>(mesh, obj);
                if (meshes.Contains(tuple))
                {
                    return;
                }

                Color32? color = null;
                foreach (KeyValuePair<Color32, GameObject> pair in examinables)
                {
                    if (pair.Value == examinable)
                    {
                        color = pair.Key;
                        break;
                    }
                }

                if (color == null)
                {
                    return;
                }

                int colorIndex = colors.IndexOf(color.Value);
                materialProperties.Add(obj, propertyBlocks[colorIndex]);
                meshes.Add(tuple);
            }

            public void AddCommands(CommandBuffer buffer)
            {
                foreach (Tuple<Mesh, GameObject> filter in meshes)
                {
                    Mesh mesh = filter.Item1;
                    if (!mesh)
                    {
                        return;
                    }

                    MaterialPropertyBlock propertyBlock = materialProperties[filter.Item2];
                    for (var i = 0; i < mesh.subMeshCount; i++)
                    {
                        buffer.DrawMesh(mesh, filter.Item2.transform.localToWorldMatrix, material, i, 0,
                            propertyBlock);
                    }
                }
            }

            private Color32 AllocateColor()
            {
                Color32 lastColor = colors.Count == 0 ? new Color32(255, 255, 255, 255) : colors[colors.Count - 1];
                Color32 newColor = lastColor;
                if (newColor.r >= ColorDistance)
                {
                    newColor.r -= ColorDistance;
                }
                else if (newColor.g >= ColorDistance)
                {
                    newColor.g -= ColorDistance;
                }
                else if (newColor.b >= ColorDistance)
                {
                    newColor.g -= ColorDistance;
                }
                else
                {
                    Debug.LogWarning("Examinable selector ran out of colors, misreads will occur!");
                }

                colors.Add(newColor);

                return newColor;
            }
        }
    }
}