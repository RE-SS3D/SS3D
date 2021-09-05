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
    private bool showIcon = false;

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

        containerDescriptor.AttachmentOffset = EditorGUILayout.Vector3Field("Attachment Offset", containerDescriptor.AttachmentOffset);

        ShowIcons();
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

    public void ShowIcons()
    {
        showIcon = EditorGUILayout.Foldout(showIcon, "Show icons");

        if (showIcon)
        {
            if (containerDescriptor.IsOpenable)
            {
                containerDescriptor.OpenIcon = (Sprite)EditorGUILayout.ObjectField("Open container icon", containerDescriptor.OpenIcon, typeof(Sprite), true);
            }

            if (containerDescriptor.ContainerType == ContainerType.Normal || containerDescriptor.ContainerType == ContainerType.Pile)
            {
                containerDescriptor.StoreIcon = (Sprite)EditorGUILayout.ObjectField("Store container icon", containerDescriptor.StoreIcon, typeof(Sprite), true);
            }

            if (containerDescriptor.ContainerType == ContainerType.Normal)
            {
                containerDescriptor.ViewIcon = (Sprite)EditorGUILayout.ObjectField("View container icon", containerDescriptor.ViewIcon, typeof(Sprite), true);
            }

            if (containerDescriptor.ContainerType == ContainerType.Pile)
            {
                containerDescriptor.TakeIcon = (Sprite)EditorGUILayout.ObjectField("Take container icon", containerDescriptor.TakeIcon, typeof(Sprite), true);
            }
        }
    }
}



	
