#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class ExtendedEditorWindow : EditorWindow
{
    protected SerializedObject serializedObject;
    protected SerializedProperty currentProperty;

    private string selectedPropertyPath;
    protected SerializedProperty SelectedProperty;

    protected void DrawProperties(SerializedProperty prop , bool drawChildren)
    {
        string LastPropPath = string.Empty;
        foreach (SerializedProperty p in prop)
        {
            if(p.isArray && p.propertyType == SerializedPropertyType.Generic)
            {
                EditorGUILayout.BeginHorizontal();
                p.isExpanded = EditorGUILayout.Foldout(p.isExpanded , p.displayName);
                EditorGUILayout.EndHorizontal();

                if(p.isExpanded)
                {
                    EditorGUI.indentLevel++;
                    DrawProperties(p,drawChildren);
                    EditorGUI.indentLevel--;
                }
            }
            else
            {
                if(!string.IsNullOrEmpty(LastPropPath) && p.propertyPath.Contains(LastPropPath)) { continue; }
                LastPropPath = p.propertyPath;
                EditorGUILayout.PropertyField(p, drawChildren);
            }
        }
    }
    protected void DrawSideBar(SerializedProperty prop)
    {
        foreach (SerializedProperty p in prop)
        {
            if(GUILayout.Button(p.displayName))
            {
                selectedPropertyPath = p.propertyPath;
            }
        }

        if(!string.IsNullOrEmpty(selectedPropertyPath))
        {
            SelectedProperty = serializedObject.FindProperty(selectedPropertyPath);
        }

        if(GUILayout.Button("Add"))
        {
            Debug.Log("Added character");
        }
    }

    protected void DrawField(string propName , bool relative )
    {
        if(relative && currentProperty != null)
        {
            EditorGUILayout.PropertyField(currentProperty.FindPropertyRelative(propName) , true);
        }
        else if(!relative && serializedObject != null)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty(propName) , true);
        }
    }
    protected void DrawProp(string _propName)
    {
        EditorGUILayout.PropertyField(serializedObject.FindProperty(_propName) , true);
    }

    protected void ApplyChanges()
    {
        serializedObject.ApplyModifiedProperties();
    }

}
#endif