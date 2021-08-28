//C# Example (LookAtPointEditor.cs)
using UnityEditor;
using UnityEngine;
using SS3D.Engine.Inventory;

[CustomEditor(typeof(ContainerController))]
public class ContainerControllerEditor : Editor
{

    public void OnEnable()
    {
        // put here graphical stuff
    }

    public override void OnInspectorGUI()
	{
		ContainerController containerController = (ContainerController)target;

		if (GUILayout.Button("Add Container", GUILayout.Width(250)))
		{
			containerController.AddBaseComponent();
		}
		int count = containerController.ContainerDescriptors.Count;
		for (int i = count-1; i >=0; i--)
        {
			GUILayout.Label("Container number " + (count -i).ToString());
			ContainerController.ContainerDescriptor containerDescriptor = containerController.ContainerDescriptors[i];
			containerDescriptor.IsOpenable = EditorGUILayout.Toggle("isOpenable", containerDescriptor.IsOpenable);
			containerDescriptor.IsStorage = EditorGUILayout.Toggle("isStorage", containerDescriptor.IsStorage);
			containerDescriptor.HasContainerType = (ContainerType) EditorGUILayout.EnumPopup(containerDescriptor.HasContainerType);

			containerDescriptor.AttachedContainerGenerator.Size = EditorGUILayout.Vector2IntField("Size", containerDescriptor.AttachedContainerGenerator.Size);
			containerDescriptor.AttachedContainerGenerator.StartFilter = (Filter) EditorGUILayout.ObjectField("Filter",containerDescriptor.AttachedContainerGenerator.StartFilter, typeof(Filter), true);

			containerDescriptor.AttachedContainer.HideItems = EditorGUILayout.Toggle("Hide Items", containerDescriptor.AttachedContainer.HideItems);
			containerDescriptor.AttachedContainer.AttachItems = EditorGUILayout.Toggle("Attach Items", containerDescriptor.AttachedContainer.AttachItems);

			if (GUILayout.Button("Remove Container", GUILayout.Width(250)))
			{
				containerController.RemoveContainer(containerDescriptor.AttachedContainer);
			}
		}


		if (GUILayout.Button("Remove All Containers", GUILayout.Width(250)))
		{
			containerController.RemoveAllContainers();
		}


	}

	
}