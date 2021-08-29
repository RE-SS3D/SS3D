//C# Example (LookAtPointEditor.cs)
using UnityEditor;
using UnityEngine;
using SS3D.Engine.Inventory;
using System.Collections.Generic;

[CustomEditor(typeof(ContainerController))]
public class ContainerControllerEditor : Editor
{
	private ContainerController.ContainerDescriptor containerDescriptor;
	private SerializedProperty ContainerDescriptors;
	private ContainerController containerController;
	private GUIStyle TitleStyle;

    public void OnEnable()
    {
		TitleStyle = new GUIStyle();
		TitleStyle.fontSize = 13;
		TitleStyle.fontStyle = FontStyle.Bold;

		containerController = (ContainerController)target;
		ContainerDescriptors = serializedObject.FindProperty("ContainerDescriptors");
	}

    public override void OnInspectorGUI()
	{
		serializedObject.Update();

		if (GUILayout.Button("Add Container", GUILayout.Width(250)))
		{
			containerController.AddBaseComponent();
		}
		int count = containerController.ContainerDescriptors.Count;
		for (int i = count-1; i >=0; i--)
        {
			GUILayout.BeginVertical("box");
			GUILayout.Label("Container " + (count -i).ToString(), TitleStyle);
			containerDescriptor = containerController.ContainerDescriptors[i];
			containerDescriptor.IsOpenable = EditorGUILayout.Toggle("isOpenable", containerDescriptor.IsOpenable);
            if (containerDescriptor.IsOpenable)
            {
				containerDescriptor.OpenableContainer.OnlyStoreWhenOpen = EditorGUILayout.Toggle("OnlyStoreWhenOpen", containerDescriptor.OpenableContainer.OnlyStoreWhenOpen);
			}

			containerDescriptor.IsStorage = EditorGUILayout.Toggle("isStorage", containerDescriptor.IsStorage);
			if (containerDescriptor.HasContainerType != ContainerType.Hidden && containerDescriptor.HasContainerType != ContainerType.Pile)
				containerDescriptor.MaxDistance = EditorGUILayout.FloatField("Max distance", containerDescriptor.MaxDistance);


			containerDescriptor.HasContainerType = (ContainerType)EditorGUILayout.EnumPopup(new GUIContent("Container Type"), containerDescriptor.HasContainerType, CheckEnabledType, false);

			containerDescriptor.AttachedContainerGenerator.Size = EditorGUILayout.Vector2IntField("Size", containerDescriptor.AttachedContainerGenerator.Size);
			containerDescriptor.AttachedContainerGenerator.StartFilter = (Filter) EditorGUILayout.ObjectField("Filter",containerDescriptor.AttachedContainerGenerator.StartFilter, typeof(Filter), true);

			containerDescriptor.AttachedContainer.HideItems = EditorGUILayout.Toggle("Hide Items", containerDescriptor.AttachedContainer.HideItems);
			containerDescriptor.AttachedContainer.AttachItems = EditorGUILayout.Toggle("Attach Items", containerDescriptor.AttachedContainer.AttachItems);

			if (GUILayout.Button("Remove Container", GUILayout.Width(150)))
			{
				containerController.RemoveContainer(containerDescriptor.AttachedContainer);
			}
			GUILayout.EndVertical();
		}

		if (GUILayout.Button("Remove All Containers", GUILayout.Width(250)))
		{
			containerController.RemoveAllContainers();
		}
		serializedObject.ApplyModifiedProperties();

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