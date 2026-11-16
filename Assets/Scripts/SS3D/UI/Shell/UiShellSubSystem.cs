using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.UI.Shell.Catalog;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.Shell
{
    /// <summary>
    /// Owns one shared <see cref="UIDocument"/> and a fixed set of composition layers
    /// (<see cref="UiLayer"/>) that UI Toolkit surfaces attach into instead of each spinning up its
    /// own document. Applies the global token stylesheets once at the root so children inherit them
    /// via the USS cascade, instead of every panel re-applying them on each open.
    /// <para>
    /// Self-bootstraps like <c>ScreenEffectsSubSystem</c> so adopting it needs no Boot/Game scene edit.
    /// Day one scope: HUD/Overlay/Diegetic/Modal/Debug layer roots + shared tokens. Does not (yet) own
    /// the Machine Interface / Main HUD / Storage Panel asset catalogs or documents — see
    /// Documents/architecture/2026-07_ui-shell-consolidation.md.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class UiShellSubSystem : SubSystem
    {
        private static readonly UiLayer[] LayerOrder =
        {
            UiLayer.Hud,
            UiLayer.Overlay,
            UiLayer.Diegetic,
            UiLayer.Modal,
            UiLayer.Debug,
        };

        [SerializeField]
        private UIDocument _document;

        private UiShellAssetCatalog _catalog;
        private readonly VisualElement[] _layers = new VisualElement[LayerOrder.Length];

        /// <summary>The single shared document — registered with <c>InputInterface</c> by callers that need it.</summary>
        public UIDocument Document => _document;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (SubSystems.TryGet(out UiShellSubSystem _))
            {
                return;
            }

            GameObject host = new(nameof(UiShellSubSystem));
            DontDestroyOnLoad(host);
            host.AddComponent<UiShellSubSystem>();
        }

        public VisualElement GetLayer(UiLayer layer)
        {
            TryGetLayer(layer, out VisualElement root);
            return root;
        }

        public bool TryGetLayer(UiLayer layer, out VisualElement root)
        {
            root = _layers[(int)layer];
            return root != null;
        }

        protected override void OnAwake()
        {
            base.OnAwake();

            if (_document == null)
            {
                _document = GetComponent<UIDocument>();
            }

            if (_document == null)
            {
                Debug.LogError("UiShellSubSystem requires a UIDocument on the same GameObject.", this);
                return;
            }

            if (!UiCatalogRuntimeLoader.TryLoad(
                    UiShellAssetPaths.ResourcesCatalogName,
                    UiShellAssetPaths.RebuildMenuPath,
                    this,
                    out _catalog))
            {
                return;
            }

            if (_document.panelSettings == null)
            {
                _document.panelSettings = _catalog.PanelSettings;
            }

            BuildLayers();
        }

        private void BuildLayers()
        {
            VisualElement root = _document.rootVisualElement;
            if (root == null)
            {
                Debug.LogError("UiShellSubSystem could not access UIDocument.rootVisualElement. Assign Panel Settings on the UIDocument.", this);
                return;
            }

            root.style.flexGrow = 1;
            root.pickingMode = PickingMode.Ignore;

            if (_catalog.Ss3dTokensStyle != null)
            {
                root.styleSheets.Add(_catalog.Ss3dTokensStyle);
            }

            if (_catalog.Ss3dTypographyStyle != null)
            {
                root.styleSheets.Add(_catalog.Ss3dTypographyStyle);
            }

            foreach (UiLayer layer in LayerOrder)
            {
                VisualElement layerRoot = new() { name = $"ui-shell-layer-{layer}" };
                // Fully qualified: SubSystem inherits Actor.Position (a Vector3 instance property),
                // which shadows the UnityEngine.UIElements.Position enum for an unqualified reference here.
                layerRoot.style.position = UnityEngine.UIElements.Position.Absolute;
                layerRoot.style.left = 0;
                layerRoot.style.top = 0;
                layerRoot.style.right = 0;
                layerRoot.style.bottom = 0;
                layerRoot.pickingMode = PickingMode.Ignore;
                root.Add(layerRoot);
                _layers[(int)layer] = layerRoot;
            }
        }
    }
}
