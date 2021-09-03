//C# Example (LookAtPointEditor.cs)
using UnityEditor;
using UnityEngine;
using SS3D.Engine.Inventory;
using System.Collections.Generic;

[CustomEditor(typeof(ContainerDescriptor))]
public class ContainerDescriptorEditor : Editor
{
    private ContainerDescriptor containerDescriptor;
    private GUIStyle TitleStyle;

    public void OnEnable()
    {
        TitleStyle = new GUIStyle();
        TitleStyle.fontSize = 13;
        TitleStyle.fontStyle = FontStyle.Bold;

        containerDescriptor = (ContainerDescriptor)target;
        containerDescriptor.AddBase();
    }

    public override void OnInspectorGUI()
    {
        EditorGUILayout.LabelField(containerDescriptor.ContainerName, TitleStyle);
        containerDescriptor.ContainerName = EditorGUILayout.TextField("Container Name", containerDescriptor.ContainerName);
        containerDescriptor.IsOpenable = EditorGUILayout.Toggle("is Openable", containerDescriptor.IsOpenable);

        if (containerDescriptor.IsOpenable)
        {
            containerDescriptor.OnlyStoreWhenOpen = EditorGUILayout.Toggle("OnlyStoreWhenOpen", containerDescriptor.OnlyStoreWhenOpen);
        }

        containerDescriptor.IsStorage = EditorGUILayout.Toggle("isStorage", containerDescriptor.IsStorage);

        if (containerDescriptor.ContainerType != ContainerType.Hidden && containerDescriptor.ContainerType != ContainerType.Pile)
        {
            containerDescriptor.MaxDistance = EditorGUILayout.FloatField("Max distance", containerDescriptor.MaxDistance);
        }

        containerDescriptor.ContainerType = (ContainerType)EditorGUILayout.EnumPopup(new GUIContent("Container Type"), containerDescriptor.ContainerType, CheckEnabledType, false);
        containerDescriptor.Size = EditorGUILayout.Vector2IntField("Size", containerDescriptor.Size);
        containerDescriptor.StartFilter = (Filter)EditorGUILayout.ObjectField("Filter", containerDescriptor.StartFilter, typeof(Filter), true);

        if(containerDescriptor.ContainerType != ContainerType.Hidden)
        {
            containerDescriptor.HideItems = EditorGUILayout.Toggle("Hide Items", containerDescriptor.HideItems);
        }

        containerDescriptor.AttachItems = EditorGUILayout.Toggle("Attach Items", containerDescriptor.AttachItems);
        containerDescriptor.attachedContainer = (AttachedContainer)EditorGUILayout.ObjectField("Attached container", containerDescriptor.attachedContainer, typeof(AttachedContainer), true);
    }

    public bool CheckEnabledType(System.Enum e)
    {
        ContainerType containerType = (ContainerType)e;

        if (containerDescriptor.IsOpenable)
        {
            if (containerType == ContainerType.Normal || containerType == ContainerType.Pile)
                return true;
            else
                return false;
        }
        if (containerDescriptor.IsStorage)
        {
            return true;
        }
        return false;
    }

}



	
