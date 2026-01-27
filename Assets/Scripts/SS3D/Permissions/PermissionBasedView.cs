using Coimbra.Services.Events;
using SS3D.Core;
using SS3D.Logging;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UserPermissionsChangedEvent = SS3D.Permissions.Events.UserPermissionsChangedEvent;

namespace SS3D.Permissions
{
  public class PermissionBasedView : MonoBehaviour
  {
    [System.Serializable]
    private sealed class RoleView
    {
      public ServerRoleTypes minimumRole;
      public List<GameObject> objects;
    }

    [SerializeField] private List<GameObject> _defaultObjects;
    [SerializeField] private List<RoleView> _roleViews;

    private IEnumerator Start()
    {
      UserPermissionsChangedEvent.AddListener(HandleUserPermissionsUpdated);

      return InitialUpdate();
    }

    /// <summary>
    /// Performs the initial update of the permission-based view after permissions have been loaded.
    /// </summary>
    /// <returns>An IEnumerator for coroutine handling.</returns>
    private IEnumerator InitialUpdate()
    {
      PermissionSubSystem permissionSystem = SubSystems.Get<PermissionSubSystem>();

      if (!permissionSystem.HasLoadedPermissions)
      {
        yield return new WaitUntil(() => permissionSystem.HasLoadedPermissions);
      }

      string ckey = Core.Settings.LocalPlayer.Ckey;
      if (string.IsNullOrEmpty(ckey))
      {
        Log.Warning(this, "Local player ckey is null or empty, cannot initialize permission-based view", Logs.UI);
        SetActive(_defaultObjects, true);
        yield break;
      }

      if (permissionSystem.TryGetUserRole(ckey, out ServerRoleTypes role))
      {
        UpdateObjectsVisibility(role);
      }
      else
      {
        UpdateObjectsVisibility(ServerRoleTypes.None);
      }
    }

    private void HandleUserPermissionsUpdated(ref EventContext context, in UserPermissionsChangedEvent e)
    {
      string ckey = Core.Settings.LocalPlayer.Ckey;

      if (string.IsNullOrEmpty(ckey))
      {
        Log.Warning(this, "Local player ckey is null or empty, cannot update permission-based view", Logs.UI);
        return;
      }

      ServerRoleTypes role = ServerRoleTypes.None;
      if (e.Permissions != null)
      {
        e.Permissions.TryGetValue(ckey, out role);
      }

      Log.Information(this, "Permission view updated for {ckey} with role {role}", Logs.UI, ckey, role);
      UpdateObjectsVisibility(role);
    }

    /// <summary>
    /// Updates the visibility of objects based on the given role, starting from the topmost role.
    /// </summary>
    /// <param name="role">The role to determine visibility.</param>
    private void UpdateObjectsVisibility(ServerRoleTypes role)
    {
      bool anyRoleViewMatched = false;

      if (_roleViews != null)
      {
        foreach (RoleView view in _roleViews.Where(view => view != null))
        {
          bool isAllowed = role >= view.minimumRole;
          anyRoleViewMatched |= isAllowed;
          SetActive(view.objects, isAllowed);
        }
      }

      SetActive(_defaultObjects, !anyRoleViewMatched);
    }

    /// <summary>
    /// Sets the active state of a list of GameObjects.
    /// </summary>
    /// <param name="objects">The list of GameObjects to set active or inactive.</param>
    /// <param name="state">The active state to set.</param>
    private void SetActive(List<GameObject> objects, bool state)
    {
      if (objects == null)
      {
        return;
      }

      foreach (GameObject o in objects.Where(o => o != null))
      {
        o.SetActive(state);
      }
    }
  }
}
