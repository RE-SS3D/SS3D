//C# Example (LookAtPointEditor.cs)
using UnityEditor;
using UnityEngine;
using SS3D.Engine.Inventory;

[CustomEditor(typeof(ContainerController))]
public class ContainerControllerEditor : Editor
{

	private string labelText;
	private bool addContainer = false;
	private bool buttonPressed = false;
	public override void OnInspectorGUI()
	{
		
		ContainerController containerController = (ContainerController)target;
		
		if (GUILayout.Button("Add Container", GUILayout.Width(250)))
		{

			containerController.addedContainer = !containerController.addedContainer;
			Debug.Log("add container clicked");

			if (!containerController.hasAttachedContainer)
			{
				containerController.AddBaseComponent();
			}
			else
			{
				containerController.RemoveBaseComponent();
			}
		}

		if (containerController.hasAttachedContainer)
        {
			containerController.isOpenable = EditorGUILayout.Toggle("isOpenable", containerController.isOpenable);
		}

		if (containerController.isOpenable)
        {
			Debug.Log("container is openable");
			if (!containerController.hasOpenableContainer)
            {
				containerController.AddOpenableContainer();
			}
        }
        else
        {
			if (containerController.hasOpenableContainer)
			{
				containerController.RemoveOpenableContainer();
			}
		}

		
	}
}