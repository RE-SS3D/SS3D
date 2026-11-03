using System.Linq;
using Coimbra;
using SS3D.Interactions.Interfaces;
using SS3D.Core;
using SS3D.Interactions;
using SS3D.Systems.Inputs;
using SS3D.Systems.Interactions.UI;
using SS3D.Systems.Health.Interactions;
using SS3D.UI.Shell;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.Editor.Automation
{
    public static partial class LobbyLifecyclePilot
    {
        private const string InteractionOverlayMode = "interaction-overlay";
        private static UIDocument _overlayProbeDocument;
        private static PanelSettings _overlayProbeSettings;
        private static ArmedInteractionOverlayView _armedProbe;
        private static RadialInteractionMenuView _radialProbe;
        private static Vector2 _overlayProbePoint;

        // Guards: armed and radial interaction overlays stay aligned under UI scaling · Not cheaper because: needs rendered panels at a real screen scale; stays a visual step (D6)
        [MenuItem("SS3D/Testing/Run Interaction Overlay Regressions")]
        public static void RunInteractionOverlayRegressions() => Start(InteractionOverlayMode);

        private static Vector2 ProbePanelPoint() => InputInterface.ScreenToPanel(
            _overlayProbeDocument.rootVisualElement.panel, _overlayProbePoint);

        private static void VerifyArmedProbe(bool centered)
        {
            VisualElement root = _overlayProbeDocument.rootVisualElement;
            Rect reticle = root.Q(className: "armed-interaction-reticle").worldBound;
            Rect chip = root.Q(className: "armed-interaction-chip").worldBound;
            Require(Vector2.Distance(reticle.center, ProbePanelPoint()) < 1f,
                "Armed reticle follows screen coordinates under panel scaling");
            Require(chip.width > 0 && chip.height > 0 && !float.IsNaN(chip.x), "Action chip has finite visible geometry");
            Require(chip.xMin >= 0 && chip.yMin >= 0 && chip.xMax <= root.worldBound.xMax + 1f
                && chip.yMax <= root.worldBound.yMax + 1f, "Action chip stays inside the panel");
            VisualElement badge = root.Q(className: "armed-interaction-cant");
            if (badge.resolvedStyle.display != DisplayStyle.None)
            {
                Rect bounds = badge.worldBound;
                Require(bounds.xMin >= 0 && bounds.yMin >= 0 && bounds.xMax <= root.worldBound.xMax + 1f
                    && bounds.yMax <= root.worldBound.yMax + 1f, "Invalid-target badge stays inside the panel");
            }

            if (centered)
            {
                Require(Mathf.Abs(chip.center.x - ProbePanelPoint().x) < 1f, "Action chip is centered beneath its reticle");
                Require(chip.yMin > reticle.yMax, "Action chip does not overlap its reticle");
            }
        }

        private static Step[] BuildInteractionOverlaySteps() => BuildSandboxSteps().Take(4).Concat(new Step[]
        {
            new Step
            {
                Name = "Create a scaled runtime panel and show the CPR radial",
                Enter = () =>
                {
                    _overlayProbeSettings = Object.Instantiate(SubSystems.Get<UiShellSubSystem>().Document.panelSettings);
                    _overlayProbeSettings.scaleMode = PanelScaleMode.ConstantPixelSize;
                    _overlayProbeSettings.scale = 1.5f;
                    _overlayProbeSettings.sortingOrder = 1000;
                    _overlayProbeDocument = new GameObject("InteractionOverlayProbe").AddComponent<UIDocument>();
                    _overlayProbeDocument.panelSettings = _overlayProbeSettings;
                    _armedProbe = new ArmedInteractionOverlayView(AssetDatabase.LoadAssetAtPath<StyleSheet>(
                        "Assets/Content/Systems/UI/Interactions/ArmedInteractionOverlay/ArmedInteractionOverlay.uss"));
                    _radialProbe = new RadialInteractionMenuView(AssetDatabase.LoadAssetAtPath<StyleSheet>(
                        "Assets/Content/Systems/UI/Interactions/RadialInteractionMenu/RadialInteractionMenu.uss"), null, null);
                    _radialProbe.Attach(_overlayProbeDocument.rootVisualElement);
                    _armedProbe.Attach(_overlayProbeDocument.rootVisualElement);
                    _overlayProbePoint = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                    Settle(0.5);
                },
                Until = Settled,
            },
            new Step
            {
                Name = "Scaled radial stays centered at the pointer",
                Enter = () =>
                {
                    _radialProbe.Show(new IInteraction[] { new CprInteraction() }, new InteractionEvent(null, null), _overlayProbePoint);
                    Settle(0.5);
                },
                Until = Settled,
                Verify = () =>
                {
                    Rect radial = _overlayProbeDocument.rootVisualElement.Q(className: "radial-interaction-menu").worldBound;
                    Require(Vector2.Distance(radial.center, ProbePanelPoint()) < 1f, "Radial menu is centered in panel coordinates");
                    Capture("cpr-radial-scaled");
                },
            },
            new Step
            {
                Name = "CPR chip follows the scaled pointer without overlapping it",
                Enter = () =>
                {
                    _radialProbe.Hide();
                    _armedProbe.Show("Perform CPR - Patient");
                    _armedProbe.UpdateCursorPosition(_overlayProbePoint);
                    _armedProbe.SetTargetState(true, true);
                    Settle(0.5);
                },
                Until = Settled,
                Verify = () => { VerifyArmedProbe(true); Capture("cpr-armed-scaled"); },
            },
            new Step
            {
                Name = "CPR chip fits at the bottom-right screen edge",
                Enter = () =>
                {
                    _overlayProbePoint = new Vector2(Screen.width - 5f, 5f);
                    _armedProbe.UpdateCursorPosition(_overlayProbePoint);
                    _armedProbe.SetTargetState(true, false);
                    Settle(0.5);
                },
                Until = Settled,
                Verify = () => { VerifyArmedProbe(false); Capture("cpr-armed-edge"); },
            },
            new Step
            {
                Name = "CPR chip tracks a different panel scale",
                Enter = () =>
                {
                    _overlayProbeSettings.scale = 0.75f;
                    _overlayProbePoint = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                    _armedProbe.UpdateCursorPosition(_overlayProbePoint);
                    Settle(0.5);
                },
                Until = Settled,
                Verify = () => VerifyArmedProbe(true),
            },
            new Step
            {
                Name = "Release the probe panel",
                Enter = () =>
                {
                    _armedProbe.Detach();
                    _radialProbe.Detach();
                    _overlayProbeDocument.gameObject.Dispose(true);
                    Object.Destroy(_overlayProbeSettings);
                    Settle(0.2);
                },
                Until = Settled,
            },
        }).ToArray();
    }
}
