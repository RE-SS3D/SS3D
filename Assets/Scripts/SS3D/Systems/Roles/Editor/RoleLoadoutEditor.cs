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
        roleLoadout.leftHand = EditorGUILayout.Toggle("Left Hand", roleLoadout.leftHand);
        roleLoadout.rightHand = EditorGUILayout.Toggle("Right Hand", roleLoadout.rightHand);
        roleLoadout.leftPocket = EditorGUILayout.Toggle("Left Pocket", roleLoadout.leftPocket);
        roleLoadout.rightPocket = EditorGUILayout.Toggle("Right Pocket", roleLoadout.rightPocket);

        if (roleLoadout.leftHand)
        {
            roleLoadout.leftHandItem = (ItemId)EditorGUILayout.
                EnumPopup("Left Hand Item:", roleLoadout.leftHandItem);
        }

        if (roleLoadout.rightHand)
        {
            roleLoadout.rightHandItem = (ItemId)EditorGUILayout.
                EnumPopup("Right Hand Item:", roleLoadout.rightHandItem);
        }

        if (roleLoadout.leftPocket)
        {
            roleLoadout.leftPocketItem = (ItemId)EditorGUILayout.
                EnumPopup("Left Pocket Item:", roleLoadout.leftPocketItem);
        }

        if (roleLoadout.rightPocket)
        {
            roleLoadout.rightPocketItem = (ItemId)EditorGUILayout.
                EnumPopup("Right Pocket Item:", roleLoadout.rightPocketItem);
        }
    }
}