using System;
using DG.Tweening;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Inputs;
using SS3D.Systems.ScreenEffects;
using SS3D.UI.MachineInterface.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface
{
    /// <summary>
    /// Renders machine interface panels via UI Toolkit.
    /// UIDocument stays disabled while closed so the panel does not clear camera depth
    /// used by the selection pick pass.
    /// <para>
    /// Adding a new machine interface:
    /// </para>
    /// <list type="number">
    /// <item><description>Add a constant to <see cref="MachineInterfaceIds"/>.</description></item>
    /// <item><description>Define snapshot, FishNet serializer, view model, and mapper types.</description></item>
    /// <item><description>Create UXML/USS under Content/Systems/UI/MachineInterface and a binder implementing <see cref="IMachineInterfaceBinder"/>.</description></item>
    /// <item><description>Add a networked controller on the machine prefab (inherit <see cref="MachineInterfaceBehaviour"/>; use concrete TargetRpc snapshot types—FishNet does not support generic RPC parameters).</description></item>
    /// <item><description>Add paths to <see cref="MachineUiAssetPaths"/> and an entry in <see cref="MachineUiCatalog.RegisterAll"/>; run <c>SS3D → Machine Interface → Rebuild Asset Catalog</c>.</description></item>
    /// <item><description>Register the snapshot type in <see cref="MachineInterfaceNetworkRegistry"/>.</description></item>
    /// <item><description>Register an <see cref="IMachineOptimisticControlHandler"/> for client optimistic controls when adding interactive controls.</description></item>
    /// <item><description>Optional: add control IDs to <see cref="MachineInterfaceControlIds"/> and a dev scenario in <see cref="MachineInterfaceDevHarness"/>.</description></item>
    /// </list>
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class MachineInterfaceHost : View
    {
        /// <summary>
        /// Strength passed to <see cref="ScreenEffectsSubSystem.SetUiBackdropBlur"/> while a diegetic panel is open.
        /// Softens the 3D world; the UITK overlay stays sharp on top.
        /// </summary>
        private const float DiegeticBackdropBlur = 0.7f;

        /// <summary>Full-screen scrim alpha behind the diegetic chassis (0..1).</summary>
        private const float DiegeticBackdropDimAlpha = 0.55f;

        private const float AnimDuration = 0.22f;
        private const float OpenScaleFrom = 0.92f;
        private const float OpenTranslateYFrom = 20f;

        [SerializeField]
        private UIDocument _document;

        private MachineUiAssetCatalog _catalog;
        private VisualElement _overlayRoot;
        private VisualElement _panelRoot;
        private MachineWindow _window;
        private DiegeticDeviceShell _diegeticShell;
        private IMachineInterfaceBinder _binder;
        private string _openInterfaceId;
        private bool _overlayReady;
        private Sequence _panelSequence;
        private bool _isClosing;
        private Action _pendingCloseComplete;
        private float _scrimAlpha;
        private float _panelScale = 1f;
        private float _panelTranslateY;

        public bool IsOpen => _panelRoot != null;

        public bool Open(string interfaceId, IMachineInterfaceViewModel viewModel)
        {
            if (!EnsureDocumentActive())
            {
                return false;
            }

            if (!MachineInterfaceRegistry.TryGetUi(interfaceId, out MachineInterfaceUiRegistration registration))
            {
                Debug.LogWarning($"MachineInterfaceHost has no UI registration for: {interfaceId}", this);
                return false;
            }

            if (registration.Template == null)
            {
                Debug.LogError($"MachineInterfaceHost is missing the template for {interfaceId}.", this);
                return false;
            }

            // Drop any in-flight close without invoking its completion callback — SubSystem.Open resets state.
            CancelPanelAnimation();
            ClosePanelOnly();
            if (!CreatePanel(viewModel.Title, registration))
            {
                return false;
            }

            _openInterfaceId = interfaceId;
            TemplateContainer template = registration.Template.CloneTree();
            bool isDiegetic = registration.ShellKind == MachineInterfaceShellKind.DiegeticDevice;
            template.style.backgroundColor = Color.clear;

            if (isDiegetic)
            {
                _diegeticShell = template.Q<DiegeticDeviceShell>();

                // Keep the cloned TemplateContainer in the hierarchy so UXML style sheets stay attached.
                _panelRoot = template;
                template.style.flexShrink = 1;
                template.style.maxHeight = Length.Percent(100);
                VisualElement layoutRoot = GetDiegeticAnimRoot();
                layoutRoot.style.alignSelf = Align.Center;
                layoutRoot.style.flexShrink = 1;
                layoutRoot.style.maxHeight = Length.Percent(100);
                layoutRoot.style.marginTop = 24;
                _overlayRoot.Add(template);

                ApplyDiegeticPanelStyles(template, _diegeticShell, registration);
                PrepareDiegeticOpenPose(layoutRoot);
                SetBackdropBlur(DiegeticBackdropBlur);
            }
            else
            {
                ApplyModalPanelStyles(_window, template, registration);

                _window.Content.Add(template);
                _overlayRoot.Add(_window);
                _panelRoot = _window;
                SetScrimAlpha(0f);
                SetBackdropBlur(0f);
            }

            SetOverlayInteractive(true);

            _binder = registration.CreateBinder(_panelRoot);
            WireBinder(_binder);
            _binder.Bind(viewModel);

            WireCloseHandler();

            if (isDiegetic)
            {
                PlayDiegeticOpen(GetDiegeticAnimRoot());
            }

            return true;
        }

        public void Refresh(IMachineInterfaceViewModel viewModel)
        {
            _binder?.Bind(viewModel);
        }

        /// <summary>
        /// Closes the panel. Diegetic panels animate out before the document is disabled;
        /// <paramref name="onComplete"/> runs after teardown (or immediately for modal / empty).
        /// </summary>
        public void Close(Action onComplete = null)
        {
            if (_panelRoot == null)
            {
                ShutdownDocument();
                onComplete?.Invoke();
                return;
            }

            VisualElement animRoot = GetDiegeticAnimRoot();
            if (animRoot == null || _diegeticShell == null)
            {
                CloseImmediate();
                onComplete?.Invoke();
                return;
            }

            if (_isClosing)
            {
                if (onComplete != null)
                {
                    _pendingCloseComplete = onComplete;
                }

                return;
            }

            _isClosing = true;
            _pendingCloseComplete = onComplete;
            PlayDiegeticClose(animRoot);
        }

        /// <summary>Kills tweens and tears down immediately (destroy / reopen).</summary>
        public void CloseImmediate()
        {
            CancelPanelAnimation();
            ClosePanelOnly();
            ShutdownDocument();
        }

        protected override void OnAwake()
        {
            base.OnAwake();

            if (_document == null)
            {
                _document = GetComponent<UIDocument>();
            }

            if (!TryLoadCatalog())
            {
                return;
            }

            if (_document != null && _document.panelSettings == null)
            {
                _document.panelSettings = _catalog.PanelSettings;
            }

            MachineUiCatalog.RegisterAll(_catalog.ToCatalogAssets());
            ShutdownDocument();
            InputInterface.RegisterDocument(_document);
        }

        protected override void OnDestroyed()
        {
            InputInterface.UnregisterDocument(_document);
            CloseImmediate();
            base.OnDestroyed();
        }

        private bool TryLoadCatalog()
        {
            _catalog = Resources.Load<MachineUiAssetCatalog>(MachineUiAssetPaths.ResourcesCatalogName);
            if (_catalog == null)
            {
                Debug.LogError(
                    $"MachineInterfaceHost could not load Resources/{MachineUiAssetPaths.ResourcesCatalogName}. "
                    + "Run SS3D → Machine Interface → Rebuild Asset Catalog and commit the asset.",
                    this);
                return false;
            }

            if (!_catalog.HasRequiredAssets(out string missingField))
            {
                Debug.LogError(
                    $"MachineUiAssetCatalog is missing required assets ({missingField}). "
                    + "Run SS3D → Machine Interface → Rebuild Asset Catalog.",
                    this);
                return false;
            }

            return true;
        }

        private void ApplySharedTokenStyles(VisualElement target)
        {
            MachineInterfaceHostHelpers.ApplyTemplateStyle(target, _catalog.Ss3dTokensStyle);
            MachineInterfaceHostHelpers.ApplyTemplateStyle(target, _catalog.Ss3dTypographyStyle);
        }

        private void ApplyDiegeticPanelStyles(
            TemplateContainer template,
            DiegeticDeviceShell shell,
            MachineInterfaceUiRegistration registration)
        {
            ApplyDiegeticBaseStyles(template);
            MachineInterfaceHostHelpers.ApplyStyleSheets(template, registration.ComponentStyles);
            MachineInterfaceHostHelpers.ApplyTemplateStyle(template, registration.TemplateStyle);

            if (shell == null)
            {
                return;
            }

            ApplyDiegeticBaseStyles(shell);
            MachineInterfaceHostHelpers.ApplyStyleSheets(shell, registration.ComponentStyles);
            MachineInterfaceHostHelpers.ApplyTemplateStyle(shell, registration.TemplateStyle);

            if (shell.ScreenContent != null)
            {
                ApplyDiegeticBaseStyles(shell.ScreenContent);
                MachineInterfaceHostHelpers.ApplyStyleSheets(shell.ScreenContent, registration.ComponentStyles);
                MachineInterfaceHostHelpers.ApplyTemplateStyle(shell.ScreenContent, registration.TemplateStyle);
            }
        }

        private void ApplyModalPanelStyles(
            MachineWindow window,
            TemplateContainer template,
            MachineInterfaceUiRegistration registration)
        {
            ApplySharedTokenStyles(template);
            MachineInterfaceHostHelpers.ApplyStyleSheets(template, registration.ComponentStyles);
            MachineInterfaceHostHelpers.ApplyTemplateStyle(template, registration.TemplateStyle);

            if (window == null)
            {
                return;
            }

            ApplySharedTokenStyles(window);
            MachineInterfaceHostHelpers.ApplyStyleSheets(window, registration.ComponentStyles);
        }

        private void ApplyDiegeticBaseStyles(VisualElement template)
        {
            ApplySharedTokenStyles(template);
            MachineInterfaceHostHelpers.ApplyTemplateStyle(template, _catalog.DiegeticTokensStyle);
            MachineInterfaceHostHelpers.ApplyTemplateStyle(template, _catalog.DiegeticTonesStyle);
        }

        private bool CreatePanel(string title, MachineInterfaceUiRegistration registration)
        {
            if (registration.ShellKind == MachineInterfaceShellKind.DiegeticDevice)
            {
                _window = null;
                _diegeticShell = null;
                return true;
            }

            return CreateWindow(title, registration.Wide);
        }

        private void WireCloseHandler()
        {
            if (_window != null)
            {
                _window.CloseClicked += HandleCloseRequested;
            }

            if (_diegeticShell != null)
            {
                _diegeticShell.CloseClicked += HandleCloseRequested;
            }
        }

        private void UnwireCloseHandler()
        {
            if (_window != null)
            {
                _window.CloseClicked -= HandleCloseRequested;
            }

            if (_diegeticShell != null)
            {
                _diegeticShell.CloseClicked -= HandleCloseRequested;
            }
        }

        private bool CreateWindow(string title, bool wide = false)
        {
            _window = new MachineWindow { Title = title };
            if (wide)
            {
                _window.AddToClassList("machine-window--wide");
            }

            _window.style.left = Length.Percent(50);
            _window.style.top = Length.Percent(50);
            _window.style.translate = new Translate(Length.Percent(-50), Length.Percent(-50));
            _window.pickingMode = PickingMode.Position;

            if (_catalog.MachineWindowStyle != null)
            {
                _window.styleSheets.Add(_catalog.MachineWindowStyle);
            }

            ApplySharedTokenStyles(_window);

            return true;
        }

        private VisualElement GetDiegeticAnimRoot()
        {
            if (_diegeticShell != null)
            {
                return _diegeticShell;
            }

            return _panelRoot;
        }

        private void PrepareDiegeticOpenPose(VisualElement animRoot)
        {
            _panelScale = OpenScaleFrom;
            _panelTranslateY = OpenTranslateYFrom;
            animRoot.style.opacity = 0f;
            animRoot.style.scale = new Scale(new Vector3(_panelScale, _panelScale, 1f));
            animRoot.style.translate = new Translate(0f, _panelTranslateY);
            SetScrimAlpha(0f);
        }

        private void PlayDiegeticOpen(VisualElement animRoot)
        {
            if (animRoot == null)
            {
                return;
            }

            KillActiveSequence();

            _panelSequence = DOTween.Sequence();
            _panelSequence.Append(DOTween.To(
                    () => animRoot.style.opacity.value,
                    value => animRoot.style.opacity = value,
                    1f,
                    AnimDuration)
                .SetEase(Ease.OutCirc));
            _panelSequence.Join(DOTween.To(
                    () => _panelScale,
                    value =>
                    {
                        _panelScale = value;
                        animRoot.style.scale = new Scale(new Vector3(value, value, 1f));
                    },
                    1f,
                    AnimDuration)
                .SetEase(Ease.OutCirc));
            _panelSequence.Join(DOTween.To(
                    () => _panelTranslateY,
                    value =>
                    {
                        _panelTranslateY = value;
                        animRoot.style.translate = new Translate(0f, value);
                    },
                    0f,
                    AnimDuration)
                .SetEase(Ease.OutCirc));
            _panelSequence.Join(DOTween.To(
                    () => _scrimAlpha,
                    SetScrimAlpha,
                    DiegeticBackdropDimAlpha,
                    AnimDuration)
                .SetEase(Ease.OutCirc));
        }

        private void PlayDiegeticClose(VisualElement animRoot)
        {
            // Only kill the running tween — do not clear _pendingCloseComplete / _isClosing
            // (Close() just set those; wiping them would skip FinishClose and leave input locked).
            KillActiveSequence();
            SetBackdropBlur(0f);

            _panelSequence = DOTween.Sequence();
            _panelSequence.Append(DOTween.To(
                    () => animRoot.style.opacity.value,
                    value => animRoot.style.opacity = value,
                    0f,
                    AnimDuration)
                .SetEase(Ease.OutCirc));
            _panelSequence.Join(DOTween.To(
                    () => _panelScale,
                    value =>
                    {
                        _panelScale = value;
                        animRoot.style.scale = new Scale(new Vector3(value, value, 1f));
                    },
                    OpenScaleFrom,
                    AnimDuration)
                .SetEase(Ease.OutCirc));
            _panelSequence.Join(DOTween.To(
                    () => _panelTranslateY,
                    value =>
                    {
                        _panelTranslateY = value;
                        animRoot.style.translate = new Translate(0f, value);
                    },
                    OpenTranslateYFrom,
                    AnimDuration)
                .SetEase(Ease.OutCirc));
            _panelSequence.Join(DOTween.To(
                    () => _scrimAlpha,
                    SetScrimAlpha,
                    0f,
                    AnimDuration)
                .SetEase(Ease.OutCirc));
            _panelSequence.OnComplete(CompleteDiegeticClose);
        }

        private void CompleteDiegeticClose()
        {
            ClosePanelOnly();
            ShutdownDocument();
            _isClosing = false;
            Action callback = _pendingCloseComplete;
            _pendingCloseComplete = null;
            callback?.Invoke();
        }

        /// <summary>Stops tweens and drops any pending close callback without invoking it.</summary>
        private void CancelPanelAnimation()
        {
            KillActiveSequence();
            _pendingCloseComplete = null;
            _isClosing = false;
        }

        private void KillActiveSequence()
        {
            _panelSequence?.Kill();
            _panelSequence = null;
        }

        private void ClosePanelOnly()
        {
            if (_binder != null)
            {
                _binder.CloseRequested -= HandleCloseRequested;
                _binder.BoolControlChanged -= HandleBoolControlChanged;
                _binder.NumericControlChanged -= HandleNumericControlChanged;
                _binder.ActionControlChanged -= HandleActionControlChanged;
                _binder.Disconnect();
                _binder = null;
            }

            UnwireCloseHandler();

            if (_panelRoot != null)
            {
                _panelRoot.RemoveFromHierarchy();
            }

            _panelRoot = null;
            _window = null;
            _diegeticShell = null;
            _openInterfaceId = null;
            SetScrimAlpha(0f);
            SetBackdropBlur(0f);
            SetOverlayInteractive(false);
        }

        private void ShutdownDocument()
        {
            SetScrimAlpha(0f);
            SetBackdropBlur(0f);
            _overlayReady = false;
            _overlayRoot = null;

            if (_document != null)
            {
                _document.enabled = false;
            }
        }

        private bool EnsureDocumentActive()
        {
            if (_document == null)
            {
                _document = GetComponent<UIDocument>();
            }

            if (_document == null)
            {
                Debug.LogError("MachineInterfaceHost requires a UIDocument on the same GameObject.", this);
                return false;
            }

            if (!_document.enabled)
            {
                _document.enabled = true;
                _overlayReady = false;
                _overlayRoot = null;
            }

            return EnsureOverlay();
        }

        private bool EnsureOverlay()
        {
            if (_overlayReady)
            {
                return true;
            }

            VisualElement root = _document.rootVisualElement;
            if (root == null)
            {
                Debug.LogError(
                    "MachineInterfaceHost could not access UIDocument.rootVisualElement. Assign Panel Settings on the UIDocument.",
                    this);
                return false;
            }

            _overlayRoot = root;
            _overlayRoot.style.flexGrow = 0;
            _overlayRoot.style.flexShrink = 1;
            _overlayRoot.style.minHeight = 0;
            _overlayRoot.style.backgroundColor = Color.clear;
            _scrimAlpha = 0f;
            _overlayReady = true;
            SetOverlayInteractive(false);
            return true;
        }

        private void SetOverlayInteractive(bool interactive)
        {
            if (_overlayRoot == null)
            {
                return;
            }

            if (interactive)
            {
                _overlayRoot.style.display = DisplayStyle.Flex;
                _overlayRoot.style.flexDirection = FlexDirection.Column;
                _overlayRoot.style.justifyContent = Justify.Center;
                _overlayRoot.style.flexGrow = 1;
                _overlayRoot.style.flexShrink = 1;
                _overlayRoot.style.minHeight = 0;
                _overlayRoot.pickingMode = PickingMode.Position;
            }
            else
            {
                _overlayRoot.style.display = DisplayStyle.None;
                _overlayRoot.style.flexGrow = 0;
                _overlayRoot.style.flexShrink = 1;
                _overlayRoot.pickingMode = PickingMode.Ignore;
            }
        }

        private void SetScrimAlpha(float alpha)
        {
            _scrimAlpha = alpha;
            if (_overlayRoot == null)
            {
                return;
            }

            _overlayRoot.style.backgroundColor = alpha <= 0.001f
                ? Color.clear
                : new Color(0f, 0f, 0f, alpha);
        }

        private static void SetBackdropBlur(float intensity)
        {
            if (!SubSystems.TryGet(out ScreenEffectsSubSystem screenEffects))
            {
                return;
            }

            screenEffects.SetUiBackdropBlur(intensity);
        }

        private void WireBinder(IMachineInterfaceBinder binder)
        {
            binder.CloseRequested += HandleCloseRequested;
            binder.BoolControlChanged += HandleBoolControlChanged;
            binder.NumericControlChanged += HandleNumericControlChanged;
            binder.ActionControlChanged += HandleActionControlChanged;
        }

        private void HandleCloseRequested()
        {
            if (SubSystems.TryGet(out MachineInterfaceSubSystem subsystem))
            {
                subsystem.RequestCloseFromUi(_openInterfaceId);
            }
        }

        private void HandleBoolControlChanged(byte controlId, bool isOn)
        {
            if (SubSystems.TryGet(out MachineInterfaceSubSystem subsystem))
            {
                subsystem.NotifyBoolControl(controlId, isOn);
            }
        }

        private void HandleNumericControlChanged(byte controlId, float delta)
        {
            if (SubSystems.TryGet(out MachineInterfaceSubSystem subsystem))
            {
                subsystem.NotifyNumericControl(controlId, delta);
            }
        }

        private void HandleActionControlChanged(byte controlId, int value)
        {
            if (SubSystems.TryGet(out MachineInterfaceSubSystem subsystem))
            {
                subsystem.NotifyActionControl(controlId, value);
            }
        }
    }
}
