using SS3D.Data.Enums;
using SS3D.Systems.Roles;
using UnityEditor;

/// <summary>
/// Class handling the inspector display of a roleLoadout.
/// It will only show the ItemID fields if the booleans are set to true
/// </summary>
[CustomEditor(typeof(RoleLoadout))]
public class RoleLoadoutEditor : Editor
{
    private RoleLoadout roleLoadout;

    public void OnEnable()
    {
        roleLoadout = (RoleLoadout)target;
    }

    public override void OnInspectorGUI()
    {
        roleLoadout.LeftHand = EditorGUILayout.Toggle("Left Hand", roleLoadout.LeftHand);
        roleLoadout.RightHand = EditorGUILayout.Toggle("Right Hand", roleLoadout.RightHand);
        roleLoadout.LeftPocket = EditorGUILayout.Toggle("Left Pocket", roleLoadout.LeftPocket);
        roleLoadout.RightPocket = EditorGUILayout.Toggle("Right Pocket", roleLoadout.RightPocket);

        if (roleLoadout.LeftHand)
        {
            roleLoadout.LeftHandItem = (ItemId)EditorGUILayout.
                EnumPopup("Left Hand Item:", roleLoadout.LeftHandItem);
        }

        if (roleLoadout.RightHand)
        {
            roleLoadout.RightHandItem = (ItemId)EditorGUILayout.
                EnumPopup("Right Hand Item:", roleLoadout.RightHandItem);
        }

        if (roleLoadout.LeftPocket)
        {
            roleLoadout.LeftPocketItem = (ItemId)EditorGUILayout.
                EnumPopup("Left Pocket Item:", roleLoadout.LeftPocketItem);
        }

        if (roleLoadout.RightPocket)
        {
            roleLoadout.RightPocketItem = (ItemId)EditorGUILayout.
                EnumPopup("Right Pocket Item:", roleLoadout.RightPocketItem);
        }
    }
}