//C# Example (LookAtPointEditor.cs)
using UnityEditor;
using UnityEngine;
using SS3D.Engine.Inventory;
using SS3D.Content.Furniture.Storage;
using SS3D.Engine.Interactions;

using System.Collections.Generic;

[CustomEditor(typeof(ContainerDescriptor))]
public class ContainerDescriptorEditor : Editor
{
    private ContainerDescriptor containerDescriptor;
    private GUIStyle TitleStyle;
    private bool showIcon = false;
    private AttachedContainer attachedContainer; 
    private ContainerInteractive containerInteractive;

    public void OnEnable()
    {
        TitleStyle = new GUIStyle();
        TitleStyle.fontSize = 13;
        TitleStyle.fontStyle = FontStyle.Bold;
        TitleStyle.normal.textColor = Color.white;

        containerDescriptor = (ContainerDescriptor)target;
        AddBase();

        attachedContainer = containerDescriptor.attachedContainer;
        containerInteractive = containerDescriptor.containerInteractive;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField(containerDescriptor.containerName, TitleStyle);

        string containerName = EditorGUILayout.TextField("Container Name", containerDescriptor.containerName);
        HandleContainerName(containerName);


        bool isInteractive = EditorGUILayout.Toggle("is Interactive", containerDescriptor.isInteractive);
        HandleIsInteractive(isInteractive);


        if (containerDescriptor.isInteractive)
        {
            bool hasUi = EditorGUILayout.Toggle("has UI", containerDescriptor.hasUi);
            HandleHasUi(hasUi);

            bool isOpenable = EditorGUILayout.Toggle("is Openable", containerDescriptor.isOpenable);
            HandleIsOpenable(isOpenable);

            bool hasCustomInteraction = EditorGUILayout.Toggle("has custom interaction", containerDescriptor.hasCustomInteraction);
            HandleCustomInteraction(hasCustomInteraction);
        }

        if (containerDescriptor.isOpenable)
        {
            bool onlyStoreWhenOpen = EditorGUILayout.Toggle("Only store when open", containerDescriptor.onlyStoreWhenOpen);
            HandleOnlyStoreWhenOpen(onlyStoreWhenOpen);       
        }
        else if(containerDescriptor.hasUi)
        {  
            // check if the gameObject has a open animation
            foreach (AnimatorControllerParameter controllerParameter in containerDescriptor.gameObject.GetComponent<Animator>().parameters)
            {
                if (controllerParameter.name == "Open")
                {
                    bool openWhenContainerViewed = EditorGUILayout.Toggle("open when container viewed", containerDescriptor.openWhenContainerViewed);
                    HandleOpenWhenContainerViewed(openWhenContainerViewed);
                }
            }      
        }

        if (containerDescriptor.hasUi)
        {
            float maxDistance = EditorGUILayout.FloatField("Max distance", containerDescriptor.maxDistance);
            HandleMaxDistance(maxDistance);
        }

        Vector2Int size = EditorGUILayout.Vector2IntField("Size", containerDescriptor.size);
        HandleSize(size);

        Filter startFilter = (Filter)EditorGUILayout.ObjectField("Filter", containerDescriptor.startFilter, typeof(Filter), true);
        HandleStartFilter(startFilter);

        bool hideItems = EditorGUILayout.Toggle("Hide Items", containerDescriptor.hideItems);
        HandleHideItems(hideItems);
        
        bool attachItems = EditorGUILayout.Toggle("Attach Items", containerDescriptor.attachItems);
        HandleAttachItems(attachItems);

        Vector3 attachmentOffset = EditorGUILayout.Vector3Field("Attachment Offset", containerDescriptor.attachmentOffset);
        HandleAttachmentOffset(attachmentOffset);

        ShowIcons();
        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// Handles showing and setting icons in editor.
    /// </summary>
    public void ShowIcons()
    {
        showIcon = EditorGUILayout.Foldout(showIcon, "Show icons");

        if (showIcon)
        {
            if (containerDescriptor.isOpenable)
            {
                Sprite openIcon = (Sprite)EditorGUILayout.ObjectField("Open container icon", containerDescriptor.openIcon, typeof(Sprite), true);
                SerializedProperty sp = serializedObject.FindProperty("openIcon");
                sp.objectReferenceValue = openIcon;
            }

            if (containerDescriptor.isInteractive)
            {
                Sprite storeIcon = (Sprite)EditorGUILayout.ObjectField("Store container icon", containerDescriptor.storeIcon, typeof(Sprite), true);
                SerializedProperty sp = serializedObject.FindProperty("storeIcon");
                sp.objectReferenceValue = storeIcon;
            }

            if (containerDescriptor.hasUi)
            {
                Sprite viewIcon = (Sprite)EditorGUILayout.ObjectField("View container icon", containerDescriptor.viewIcon, typeof(Sprite), true);
                SerializedProperty sp = serializedObject.FindProperty("viewIcon");
                sp.objectReferenceValue = viewIcon;
            }

            if (containerDescriptor.isInteractive && !containerDescriptor.hasUi)
            {
                Sprite takeIcon = (Sprite)EditorGUILayout.ObjectField("Take container icon", containerDescriptor.takeIcon, typeof(Sprite), true);
                SerializedProperty sp = serializedObject.FindProperty("takeIcon");
                sp.objectReferenceValue = takeIcon;
            }
        }
    }

    /// <summary>
    /// Creates a basic container by adding all necessary components to the game object.
    /// </summary>
    public void AddBase()
    {
        if (!containerDescriptor.initialized)
        {
            AddAttached();
            HandleIsInteractive(true);
            SerializedProperty sp = serializedObject.FindProperty("initialized");
            sp.boolValue = true;
            serializedObject.ApplyModifiedProperties();
        }
    }

    private void HandleHasUi(bool hasUi)
    {
        SerializedProperty sp = serializedObject.FindProperty("hasUi");
        sp.boolValue = hasUi;
        serializedObject.ApplyModifiedProperties();
    }

    private void HandleOpenWhenContainerViewed(bool openWhenContainerViewed)
    {
        SerializedProperty sp = serializedObject.FindProperty("openWhenContainerViewed");
        sp.boolValue = openWhenContainerViewed;
        serializedObject.ApplyModifiedProperties();
    }

    private void HandleStartFilter(Filter startFilter)
    {
        SerializedProperty sp = serializedObject.FindProperty("startFilter");
        sp.objectReferenceValue = startFilter;
        serializedObject.ApplyModifiedProperties();
    }
    private void HandleSize(Vector2Int size)
    {
        SerializedProperty sp = serializedObject.FindProperty("size");
        sp.vector2IntValue = size;
        serializedObject.ApplyModifiedProperties();
    }
    private void HandleAttachmentOffset(Vector3 attachmentOffset)
    {
        SerializedProperty sp = serializedObject.FindProperty("attachmentOffset");
        sp.vector3Value = attachmentOffset;
        serializedObject.ApplyModifiedProperties();
    }
    private void HandleAttachItems(bool attachItems)
    {
        SerializedProperty sp = serializedObject.FindProperty("attachItems");
        sp.boolValue = attachItems;
        serializedObject.ApplyModifiedProperties();
    }

    private void HandleMaxDistance(float maxDistance)
    {      
        SerializedProperty sp = serializedObject.FindProperty("maxDistance");
        sp.floatValue = maxDistance;
        serializedObject.ApplyModifiedProperties();
    }
    private void HandleIsInteractive(bool isInteractive)
    {
        SerializedProperty sp = serializedObject.FindProperty("isInteractive");
        sp.boolValue = isInteractive;
        serializedObject.ApplyModifiedProperties();
        if (isInteractive && containerDescriptor.containerInteractive == null)
        {
            AddInteractive();
        }
        else if (!isInteractive && containerDescriptor.containerInteractive != null)
        {
            RemoveInteractive();
        }
    }

    private void HandleCustomInteraction(bool hasCustomInteraction)
    { 
        SerializedProperty sp = serializedObject.FindProperty("hasCustomInteraction");
        sp.boolValue = hasCustomInteraction;
        serializedObject.ApplyModifiedProperties();
    }

    private void HandleContainerName(string containerName)
    {
        SerializedProperty sp = serializedObject.FindProperty("containerName");
        sp.stringValue = containerName;
        serializedObject.ApplyModifiedProperties();
    }
    private void HandleIsOpenable(bool isOpenable)
    {
        
        SerializedProperty sp = serializedObject.FindProperty("isOpenable");
        sp.boolValue = isOpenable;
        serializedObject.ApplyModifiedProperties();

        // Openable container are always interactive.
        if (isOpenable)
        {
            HandleIsInteractive(true);
        }    
    }

    private void HandleOnlyStoreWhenOpen(bool onlyStoreWhenOpen)
    {
        SerializedProperty sp = serializedObject.FindProperty("onlyStoreWhenOpen");
        sp.boolValue = onlyStoreWhenOpen;
        serializedObject.ApplyModifiedProperties();
    }
    private void HandleHideItems(bool hideItems)
    {
        SerializedProperty sp = serializedObject.FindProperty("hideItems");
        sp.boolValue = hideItems;
        serializedObject.ApplyModifiedProperties();
    }
    private void AddInteractive()
    {
        SerializedProperty sp = serializedObject.FindProperty("containerInteractive");
        sp.objectReferenceValue = containerDescriptor.gameObject.AddComponent<ContainerInteractive>();
        serializedObject.ApplyModifiedProperties();
        containerDescriptor.containerInteractive.containerDescriptor = containerDescriptor;
    }

    private void RemoveInteractive()
    {
        DestroyImmediate(containerInteractive, true);

        HandleIsInteractive(false);
        HandleIsOpenable(false);
        HandleOnlyStoreWhenOpen(false);
        HandleCustomInteraction(false);
        HandleOpenWhenContainerViewed(false);
    }

    private void AddAttached()
    {
        containerDescriptor.attachedContainer = containerDescriptor.gameObject.AddComponent<AttachedContainer>();
        containerDescriptor.attachedContainer.containerDescriptor = containerDescriptor;
    }

    private void OnDestroy()
    {
        if(containerDescriptor == null)
        {
            DestroyImmediate(attachedContainer, true);
            DestroyImmediate(containerInteractive, true);
        }
    }
}



	
