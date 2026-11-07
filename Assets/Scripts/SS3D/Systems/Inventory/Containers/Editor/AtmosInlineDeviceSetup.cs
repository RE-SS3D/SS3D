#if UNITY_EDITOR
using FishNet.Object;
using SS3D.Data.AssetDatabases;
using SS3D.Systems.Atmospherics.Pipes;
using SS3D.Systems.Audio;
using SS3D.Systems.Electricity;
using SS3D.Systems.IdAccess;
using SS3D.Systems.Examine;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.Connections;
using SS3D.Systems.Tile.TileMapCreator;
using SS3D.UI.MachineInterface;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using DB = UnityEditor.AssetDatabase;

namespace SS3D.Systems.Atmospherics.Editor
{
    /// <summary>
    /// Puts <c>GasPump</c> on the Surface line and builds the volume pump, both valves and both
    /// manifolds from it. Tier B recipe — no MenuItem.
    /// </summary>
    public static class AtmosInlineDeviceSetup
    {
        private const string GasPumpPrefab = "Assets/Content/WorldObjects/Furniture/Machines/Atmospherics/GasPump/GasPump.prefab";
        private const string GasPumpTile = "Assets/Content/Data/TileMap/Resources/Furniture/Machines/Atmos/GasPump.asset";
        private const string PipeFolder = "Assets/Content/WorldObjects/Structures/Pipes/Atmospherics/";
        private const string SurfacePipePrefab = PipeFolder + "AtmosPipesSurface.prefab";
        private const string MachineryModel = "Assets/Art/Models/Structures/Pipes/PipeMachinery.fbx";
        private const string DeviceFolder = "Assets/Content/WorldObjects/Structures/Pipes/Atmospherics/Devices/";
        private const string ExamineFolder = "Assets/Content/Data/Examine/String/Structures/Pipes/";
        private const string GasPumpExamine = "Assets/Content/Data/Examine/String/Furniture/Machines/Atmospherics/GasPump.asset";
        private const string ScrubberPrefab = "Assets/Content/WorldObjects/Furniture/Machines/Atmospherics/Scrubber/Scrubber.prefab";
        private const string ScrubberTile = "Assets/Content/Data/TileMap/Resources/Furniture/Machines/Atmos/Scrubber.asset";
        private const string AirIntakeName = "ScrubberIntake";
        private const string AirIntakePrefab = "Assets/Content/WorldObjects/Furniture/Machines/Atmospherics/Scrubber/ScrubberIntake.prefab";
        private const string TileFolder = "Assets/Content/Data/TileMap/Resources/Structures/Pipes/Atmospherics/";
        // Floor-standing machines file under Machines in the palette by their folder (tile.md, Map Editor palette).
        // Map Editor variant families (palette S6): one folder per family, split by pipe layers.
        private const string InlineFilterTileFolder = TileFolder + "InlineFilterVariants/";
        private const string FilterTileFolder = TileFolder + "FilterVariants/";
        private const string MixerTileFolder = TileFolder + "MixerVariants/";
        private const string MachineTileFolder = "Assets/Content/Data/TileMap/Resources/Furniture/Machines/Atmos/";
        private const string ReferenceFolder = "Assets/Content/Data/ObjectAssetReferences/";
        private const string ItemsDatabase = "Assets/Content/Data/Databases/Items.asset";
        private const string ItemsDatabaseGuid = "c2644d7b34e956743a0bc485e7ca5e37";
        private const string InletCap = "InletCap";
        private const string OutletCap = "OutletCap";
        private const string FlowArrow = "FlowArrow";
        private const float PortCapCentre = 0.43f;
        private const float PortCapLength = 0.14f;
        private const float FlowArrowHeight = 0.32f;
        private const string OpenIcon = "Assets/Art/Graphics/UI/Interactions/InteractionIcons/ValveOpen.png";
        private const string ClosedIcon = "Assets/Art/Graphics/UI/Interactions/InteractionIcons/ValveClose.png";

        // The name in PipeMachinery.fbx really does end in a space.
        private const string InjectorMesh = "Injector ";

        /// <summary>
        /// A <c>Scrubber</c> that takes O₂ and N₂ instead of CO₂, for feeding a line from a chamber.
        /// </summary>
        public static void BuildAirIntake()
        {
            var scrubberTile = DB.LoadAssetAtPath<TileObjectSo>(ScrubberTile);
            GameObject root = PrefabUtility.LoadPrefabContents(ScrubberPrefab);
            try
            {
                root.name = AirIntakeName;
                var scrubber = new SerializedObject(root.GetComponent<ScrubberController>());
                scrubber.FindProperty("_isAirIntake").boolValue = true;
                scrubber.ApplyModifiedPropertiesWithoutUndo();
                ClearAssetReference(root);
                FishNet.Managing.Object.ManagedObjects.InitializePrefab(root.GetComponent<NetworkObject>(), -1);
                PrefabUtility.SaveAsPrefabAsset(root, AirIntakePrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            TileObjectSo tile = RegisterTileObject(AirIntakeName, AirIntakePrefab, scrubberTile.layer, MachineTileFolder);
            tile.placementYOffset = scrubberTile.placementYOffset;
            EditorUtility.SetDirty(tile);
            DB.SaveAssets();
        }

        public static void Run()
        {
            if (!DB.IsValidFolder(DeviceFolder.TrimEnd('/')))
                DB.CreateFolder("Assets/Content/WorldObjects/Structures/Pipes/Atmospherics", "Devices");

            AddPumpVisuals();

            var gasPumpTile = DB.LoadAssetAtPath<TileObjectSo>(GasPumpTile);
            gasPumpTile.layer = TileLayer.PipeSurface;
            gasPumpTile.placementYOffset = 0f;
            EditorUtility.SetDirty(gasPumpTile);

            BuildFromPump("AtmosPumpVolume", root =>
            {
                ShowMesh(root, "PumpVolume");
                var pump = new SerializedObject(root.GetComponent<AtmosPumpController>());
                pump.FindProperty("_pumpRegulation").enumValueIndex = (int)PumpRegulation.Volume;
                pump.ApplyModifiedPropertiesWithoutUndo();
                SetPanel(root, "VOLUME PUMP · ATMOSPHERICS", "PMV-14 · pipe volume pump", "PMV-14", "Pipe Volume Pump");
            });

            BuildFromPump("AtmosValveDigital", root =>
            {
                ShowMesh(root, "ValveDigital");
                AddPipeRun(root);
                RemovePumpVisuals(root);
                ReplacePumpWithValve(root, turnedByHand: false);
                Object.DestroyImmediate(root.GetComponent<AmbientLoopSource>());
                SetPanel(root, "VALVE · ATMOSPHERICS", "VLV-10 · digital pipe valve", "VLV-10", "Digital Pipe Valve");
            });

            BuildFromPump("AtmosValveManual", root =>
            {
                ShowMesh(root, "ValveManual");
                AddPipeRun(root);
                GameObject handle = AddChildMesh(root, "ValveManualHandle");
                RemovePumpVisuals(root);
                ReplacePumpWithValve(root, turnedByHand: true, handle.transform);
                RemovePumpParts(root);
            });

            BuildManifold(
                "AtmosManifold3",
                "PipesManifold3",
                TileLayer.PipeLeft,
                TileLayer.PipeMiddle,
                TileLayer.PipeRight);
            BuildManifold(
                "AtmosManifold4",
                "PipesManifold4",
                TileLayer.PipeLeft,
                TileLayer.PipeMiddle,
                TileLayer.PipeRight,
                TileLayer.PipeSurface);

            BuildAirIntake();
            BuildFiltersAndMixers();
            Debug.Log("[AtmosInlineDeviceSetup] Built inline devices, manifolds and the air intake.");
        }

        /// <summary>
        /// Filters in both forms, mixer plates, the meter and the injector. Needs
        /// <see cref="Run"/>'s pump visuals, so it can run alone once those exist.
        /// </summary>
        public static void BuildFiltersAndMixers()
        {
            BuildFilter("AtmosFilterL2", "FilterL2", sideOnLeft: true, AtmosDevicePipeResolver.InlineLayers, InlineFilterTileFolder);
            BuildFilter("AtmosFilterR2", "FilterR2", sideOnLeft: false, AtmosDevicePipeResolver.InlineLayers, InlineFilterTileFolder);
            BuildFilter("AtmosFilterL", "FilterL", sideOnLeft: true, AtmosDevicePipeResolver.PlateLayers, FilterTileFolder);
            BuildFilter("AtmosFilterR", "FilterR", sideOnLeft: false, AtmosDevicePipeResolver.PlateLayers, FilterTileFolder);
            BuildMixer("AtmosMixerL", "MixerL", sideOnLeft: true);
            BuildMixer("AtmosMixerR", "MixerR", sideOnLeft: false);

            GameObject meter = PrefabUtility.LoadPrefabContents(GasPumpPrefab);
            try
            {
                meter.name = "AtmosMeter";
                ShowMesh(meter, "Meter");
                RemovePumpVisuals(meter);
                RemovePumpParts(meter);
                Object.DestroyImmediate(meter.GetComponent<AtmosPumpController>());
                meter.AddComponent<AtmosMeterController>();
                meter.AddComponent<MeterInterfaceController>();
                SaveTilePrefab(meter, "AtmosMeter", TileLayer.FurnitureBase);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(meter);
            }

            GameObject injector = PrefabUtility.LoadPrefabContents(GasPumpPrefab);
            try
            {
                injector.name = "AtmosInjector";
                ShowMesh(injector, InjectorMesh);
                RemovePumpVisuals(injector);
                RemovePumpParts(injector);
                Object.DestroyImmediate(injector.GetComponent<AtmosPumpController>());
                var settings = new SerializedObject(injector.AddComponent<AtmosInjectorController>());
                settings.FindProperty("_onIcon").objectReferenceValue = DB.LoadAssetAtPath<Sprite>(OpenIcon);
                settings.FindProperty("_offIcon").objectReferenceValue = DB.LoadAssetAtPath<Sprite>(ClosedIcon);
                settings.ApplyModifiedPropertiesWithoutUndo();
                SaveTilePrefab(injector, "AtmosInjector", TileLayer.FurnitureBase);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(injector);
            }

            DB.SaveAssets();
        }

        private static void BuildFilter(string name, string mesh, bool sideOnLeft, TileLayer[] lines, string tileFolder)
        {
            BuildThreeWay(name, mesh, sideOnLeft, lines, tileFolder, root =>
            {
                root.AddComponent<AtmosFilterController>();
                return root.AddComponent<FilterInterfaceController>();
            });
        }

        private static void BuildMixer(string name, string mesh, bool sideOnLeft)
        {
            // No machine-form art, so the plate also serves on Surface.
            BuildThreeWay(name, mesh, sideOnLeft, AtmosDevicePipeResolver.PlateOrSurfaceLayers, MixerTileFolder, root =>
            {
                root.AddComponent<AtmosMixerController>();
                return root.AddComponent<MixerInterfaceController>();
            });
        }

        private static void BuildThreeWay(
            string name,
            string mesh,
            bool sideOnLeft,
            TileLayer[] lines,
            string tileFolder,
            System.Func<GameObject, Component> addDevice)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(GasPumpPrefab);
            try
            {
                root.name = name;
                ShowMesh(root, mesh);
                RemovePumpVisuals(root);
                AddFlowArrow(root);
                Object.DestroyImmediate(root.GetComponent<AmbientLoopSource>());
                Object.DestroyImmediate(root.GetComponent<PumpInterfaceController>());
                Object.DestroyImmediate(root.GetComponent<AtmosPumpController>());

                Component panel = addDevice(root);
                var power = root.GetComponent<BasicPowerConsumer>();
                var device = new SerializedObject(root.GetComponent<AtmosThreeWayDeviceBase>());
                device.FindProperty("_powerConsumer").objectReferenceValue = power;
                device.FindProperty("_sideOnLeft").boolValue = sideOnLeft;
                SerializedProperty deviceLines = device.FindProperty("_lines");
                deviceLines.arraySize = lines.Length;
                for (int i = 0; i < lines.Length; i++)
                    deviceLines.GetArrayElementAtIndex(i).enumValueIndex = (int)lines[i];

                device.ApplyModifiedPropertiesWithoutUndo();

                var panelSettings = new SerializedObject(panel);
                panelSettings.FindProperty("_powerConsumer").objectReferenceValue = power;
                panelSettings.ApplyModifiedPropertiesWithoutUndo();
                SaveTilePrefab(root, name, tileFolder: tileFolder);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // Valves and manifolds start from the pump too, and drop these in RemovePumpVisuals.
        private static void AddPumpVisuals()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(GasPumpPrefab);
            try
            {
                RemovePumpVisuals(root);
                var pump = new SerializedObject(root.GetComponent<AtmosPumpController>());
                pump.FindProperty("_inletCap").objectReferenceValue = AddPortCap(root, InletCap, -1f);
                pump.FindProperty("_outletCap").objectReferenceValue = AddPortCap(root, OutletCap, 1f);
                pump.FindProperty("_targetOutletPressureKpa").intValue = AtmosPortConstants.PumpDefaultTargetKpa;
                pump.ApplyModifiedPropertiesWithoutUndo();
                AddFlowArrow(root);
                ClearAssetReference(root);
                PrefabUtility.SaveAsPrefabAsset(root, GasPumpPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void RemovePumpVisuals(GameObject root)
        {
            foreach (string child in new[] { InletCap, OutletCap, FlowArrow })
            {
                Transform found = root.transform.Find(child);
                if (found != null)
                    Object.DestroyImmediate(found.gameObject);
            }
        }

        private static GameObject AddPortCap(GameObject root, string name, float side)
        {
            GameObject cap = AddPipeMesh(root, name);
            cap.transform.localPosition = new Vector3(0f, 0f, side * PortCapCentre);
            cap.transform.localScale = new Vector3(1f, 1f, PortCapLength);
            cap.SetActive(false);
            return cap;
        }

        // Points the way gas leaves: +Z, the device's front.
        private static void AddFlowArrow(GameObject root)
        {
            var arrow = new GameObject(FlowArrow);
            arrow.transform.SetParent(root.transform, false);
            arrow.transform.localPosition = new Vector3(0f, FlowArrowHeight, 0f);
            arrow.AddComponent<HologramOnly>();
            AddArrowBar(arrow, Vector3.back * 0.075f, 0f, 0.35f);
            AddArrowBar(arrow, new Vector3(0.075f, 0f, 0.025f), -45f, 0.21f);
            AddArrowBar(arrow, new Vector3(-0.075f, 0f, 0.025f), 45f, 0.21f);
            arrow.SetActive(false);
        }

        private static void AddArrowBar(GameObject arrow, Vector3 position, float yaw, float length)
        {
            GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(bar.GetComponent<Collider>());
            bar.transform.SetParent(arrow.transform, false);
            bar.transform.localPosition = position;
            bar.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            bar.transform.localScale = new Vector3(0.05f, 0.03f, length);
        }

        private static void BuildFromPump(string name, System.Action<GameObject> change)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(GasPumpPrefab);
            try
            {
                root.name = name;
                change(root);
                SaveTilePrefab(root, name);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void BuildManifold(string name, string mesh, params TileLayer[] lines)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(GasPumpPrefab);
            try
            {
                root.name = name;
                ShowMesh(root, mesh);
                RemovePumpVisuals(root);
                RemovePumpParts(root);
                Object.DestroyImmediate(root.GetComponent<AtmosPumpController>());
                root.AddComponent<GasPipeManifold>().SetLines(lines);
                AddLineRuns(root, lines);
                AllowOneFacingPerAxis(root);
                SaveTilePrefab(root, name, TileLayer.PipeManifold);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void RemovePumpParts(GameObject root)
        {
            Object.DestroyImmediate(root.GetComponent<AmbientLoopSource>());
            Object.DestroyImmediate(root.GetComponent<PumpInterfaceController>());
            Object.DestroyImmediate(root.GetComponent<AuthLogDeviceBehaviour>());
            Object.DestroyImmediate(root.GetComponent<ElectricDeviceAdjacencyConnector>());
            Object.DestroyImmediate(root.GetComponent<BasicPowerConsumer>());
        }

        private static void SaveTilePrefab(GameObject root, string name, TileLayer layer = TileLayer.PipeSurface, string tileFolder = null)
        {
            string examinePath = ExamineFolder + name + ".asset";
            if (DB.LoadMainAssetAtPath(examinePath) == null)
                DB.CopyAsset(GasPumpExamine, examinePath);

            var examinable = new SerializedObject(root.GetComponent<SimpleExaminable>());
            examinable.FindProperty("key").objectReferenceValue = DB.LoadMainAssetAtPath(examinePath);
            examinable.ApplyModifiedPropertiesWithoutUndo();

            ClearAssetReference(root);
            FishNet.Managing.Object.ManagedObjects.InitializePrefab(root.GetComponent<NetworkObject>(), -1);
            string path = DeviceFolder + name + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            // Floor-standing devices (meter, injector) file under Machines › Atmos like the scrubber intake.
            RegisterTileObject(name, path, layer, tileFolder ?? (layer == TileLayer.FurnitureBase ? MachineTileFolder : TileFolder));
        }

        // The copied prefab's reference names the source; placement sets the real one.
        private static void ClearAssetReference(GameObject root)
        {
            var placed = new SerializedObject(root.GetComponent<PlacedTileObject>());
            placed.FindProperty("_asset").objectReferenceValue = null;
            placed.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ReplacePumpWithValve(GameObject root, bool turnedByHand, Transform handle = null)
        {
            var valve = root.AddComponent<AtmosValveController>();
            var settings = new SerializedObject(valve);
            settings.FindProperty("_turnedByHand").boolValue = turnedByHand;
            settings.FindProperty("_handle").objectReferenceValue = handle;
            settings.FindProperty("_openIcon").objectReferenceValue = DB.LoadAssetAtPath<Sprite>(OpenIcon);
            settings.FindProperty("_closedIcon").objectReferenceValue = DB.LoadAssetAtPath<Sprite>(ClosedIcon);
            if (!turnedByHand)
                settings.FindProperty("_powerConsumer").objectReferenceValue = root.GetComponent<BasicPowerConsumer>();

            settings.ApplyModifiedPropertiesWithoutUndo();
            Object.DestroyImmediate(root.GetComponent<AtmosPumpController>());
        }

        private static void SetPanel(GameObject root, string title, string modelLabel, string deviceTitle, string subtitle)
        {
            var panel = new SerializedObject(root.GetComponent<PumpInterfaceController>());
            panel.FindProperty("_title").stringValue = title;
            panel.FindProperty("_modelLabel").stringValue = modelLabel;
            panel.FindProperty("_deviceTitle").stringValue = deviceTitle;
            panel.FindProperty("_deviceKind").stringValue = subtitle;
            panel.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Mesh LoadMesh(string name)
        {
            foreach (Object asset in DB.LoadAllAssetsAtPath(MachineryModel))
            {
                if (asset is Mesh mesh && mesh.name == name)
                    return mesh;
            }

            throw new System.ArgumentException($"{MachineryModel} has no mesh named {name}");
        }

        private static void ShowMesh(GameObject root, string name)
        {
            Mesh mesh = LoadMesh(name);
            root.GetComponent<MeshFilter>().sharedMesh = mesh;
            root.GetComponent<MeshCollider>().sharedMesh = mesh;
            FitMaterials(root.GetComponent<MeshRenderer>(), mesh);
        }

        private static void FitMaterials(MeshRenderer renderer, Mesh mesh)
        {
            Material[] materials = renderer.sharedMaterials;
            if (materials.Length == mesh.subMeshCount)
                return;

            System.Array.Resize(ref materials, mesh.subMeshCount);
            renderer.sharedMaterials = materials;
        }

        private static GameObject AddChildMesh(GameObject root, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(root.transform, false);
            child.AddComponent<MeshFilter>().sharedMesh = LoadMesh(name);
            child.AddComponent<MeshRenderer>().sharedMaterial = root.GetComponent<MeshRenderer>().sharedMaterial;
            return child;
        }

        // A valve is shorter than its tile, so the pipe shows running through it.
        private static void AddPipeRun(GameObject root) => AddPipeMesh(root, "PipeRun");

        private static GameObject AddPipeMesh(GameObject root, string name, string pipePrefab = SurfacePipePrefab)
        {
            var pipe = DB.LoadAssetAtPath<GameObject>(pipePrefab);
            var child = new GameObject(name);
            child.transform.SetParent(root.transform, false);
            child.AddComponent<MeshFilter>().sharedMesh = pipe.GetComponent<MeshFilter>().sharedMesh;
            child.AddComponent<MeshRenderer>().sharedMaterial = pipe.GetComponent<MeshRenderer>().sharedMaterial;
            return child;
        }

        // Each line runs through the tile in the pipe's own mesh, so it meets the pipes at the ports.
        private static void AddLineRuns(GameObject root, TileLayer[] lines)
        {
            foreach (TileLayer line in lines)
            {
                string pipe = line switch
                {
                    TileLayer.PipeLeft => "AtmosPipesSupply",
                    TileLayer.PipeMiddle => "AtmosPipesMixed",
                    TileLayer.PipeRight => "AtmosPipesWaste",
                    _ => "AtmosPipesSurface",
                };
                AddPipeMesh(root, pipe + "Run", PipeFolder + pipe + ".prefab");
            }
        }

        // Front and back are one axis, and the lines sit at different heights, so a half turn would
        // put each run on the wrong line.
        /// <summary>
        /// Gives a prefab its tile asset and a place in the Items database and addressables.
        /// </summary>
        private static TileObjectSo RegisterTileObject(string name, string path, TileLayer layer, string tileFolder = TileFolder)
        {
            var reference = EnsureAsset<ObjectAssetReference>(ReferenceFolder + name + ".asset");
            reference.Id = DB.AssetPathToGUID(path);
            reference.Database = ItemsDatabaseGuid;
            EditorUtility.SetDirty(reference);

            var tile = EnsureAsset<TileObjectSo>(tileFolder + name + ".asset");
            tile.PrefabAsset = reference;
            tile.layer = layer;
            EditorUtility.SetDirty(tile);

            var database = DB.LoadAssetAtPath<SS3D.Data.AssetDatabases.AssetDatabase>(ItemsDatabase);
            AddressableAssetSettingsDefaultObject.Settings.CreateOrMoveEntry(reference.Id, database.AssetGroup).address = path;
            database.Assets[reference.Id] = DB.LoadAssetAtPath<GameObject>(path);
            EditorUtility.SetDirty(database);
            return tile;
        }

        private static T EnsureAsset<T>(string path) where T : ScriptableObject
        {
            var asset = DB.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;

            string folder = path.Substring(0, path.LastIndexOf('/'));
            if (!DB.IsValidFolder(folder))
                DB.CreateFolder(folder.Substring(0, folder.LastIndexOf('/')), folder.Substring(folder.LastIndexOf('/') + 1));

            asset = ScriptableObject.CreateInstance<T>();
            DB.CreateAsset(asset, path);
            return asset;
        }

        private static void AllowOneFacingPerAxis(GameObject root)
        {
            var rotation = new SerializedObject(root.AddComponent<CustomGhostTileObjectRotation>());
            SerializedProperty allowed = rotation.FindProperty("_allowedDirections");
            allowed.arraySize = 2;
            allowed.GetArrayElementAtIndex(0).enumValueIndex = (int)Direction.North;
            allowed.GetArrayElementAtIndex(1).enumValueIndex = (int)Direction.East;
            rotation.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
