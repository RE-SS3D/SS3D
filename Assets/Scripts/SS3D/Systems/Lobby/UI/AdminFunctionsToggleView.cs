using Coimbra.Services.Events;
using FishNet;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Core.Settings;
using SS3D.Logging;
using SS3D.Permissions;
using SS3D.Permissions.Events;
using SS3D.UI.Buttons;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace SS3D.Systems.Lobby.UI
{
    /// <summary>
    /// Adds the host-only server setting that lets every player use admin functions.
    /// </summary>
    public class AdminFunctionsToggleView : Actor
    {
        private const string NormalText = "<sprite name=\"deny\"> admin funcs: admins";
        private const string PressedText = "<sprite name=\"approve\"> admin funcs: all";

        private ToggleLabelButton _toggleButton;

        protected override void OnStart()
        {
            base.OnStart();

            CreateToggleButton();
            AddHandle(UserPermissionsChangedEvent.AddListener(HandleUserPermissionsUpdated));
            RefreshToggleButton();
        }

        protected override void OnDestroyed()
        {
            base.OnDestroyed();

            if (_toggleButton != null)
            {
                _toggleButton.OnPressedDown -= HandleToggleButtonPressed;
            }
        }

        private void CreateToggleButton()
        {
            ToggleLabelButton templateButton = GetComponentsInChildren<ToggleLabelButton>(true)
                .FirstOrDefault(button => button.gameObject.name == "Start Round");

            if (templateButton == null)
            {
                Log.Warning(this, "Could not find a button template for the admin functions toggle", Logs.UI);
                return;
            }

            _toggleButton = Instantiate(templateButton, templateButton.transform.parent);
            _toggleButton.gameObject.name = "Admin Functions For All";
            _toggleButton.NormalText(NormalText);
            _toggleButton.PressedText(PressedText);
            _toggleButton.OnPressedDown += HandleToggleButtonPressed;

            if (_toggleButton.transform is RectTransform rectTransform)
            {
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 190f);
            }

            if (_toggleButton.transform.parent.TryGetComponent(out HorizontalOrVerticalLayoutGroup layoutGroup))
            {
                layoutGroup.padding.right = 0;
                layoutGroup.spacing = Mathf.Max(layoutGroup.spacing, 5f);
            }

            DisableCopiedLocalizers(_toggleButton.gameObject);
        }

        private void HandleUserPermissionsUpdated(ref EventContext context, in UserPermissionsChangedEvent e)
        {
            RefreshToggleButton();
        }

        private void HandleToggleButtonPressed(bool enabled)
        {
            PermissionSubSystem permissionSystem = SubSystems.Get<PermissionSubSystem>();

            if (!CanToggleAdminFunctions(permissionSystem))
            {
                RefreshToggleButton();
                return;
            }

            permissionSystem.CmdSetAdminFunctionsEnabledForAll(enabled);
        }

        private void RefreshToggleButton()
        {
            if (_toggleButton == null)
            {
                return;
            }

            PermissionSubSystem permissionSystem = SubSystems.Get<PermissionSubSystem>();

            _toggleButton.Pressed = permissionSystem != null && permissionSystem.AdminFunctionsEnabledForAll;
            _toggleButton.Disabled = !CanToggleAdminFunctions(permissionSystem);
            _toggleButton.RefreshVisuals();
        }

        private static bool CanToggleAdminFunctions(PermissionSubSystem permissionSystem)
        {
            if (permissionSystem == null || !permissionSystem.HasLoadedPermissions)
            {
                return false;
            }

            return InstanceFinder.IsHost && permissionSystem.IsAtLeast(LocalPlayer.Ckey, ServerRoleTypes.ServerOwner);
        }

        private static void DisableCopiedLocalizers(GameObject root)
        {
            foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour != null && behaviour.GetType().Name == "LocalizeStringEvent")
                {
                    behaviour.enabled = false;
                }
            }
        }
    }
}
