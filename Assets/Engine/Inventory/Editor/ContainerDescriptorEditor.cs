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
    private ContainerSync containerSync;
    private ContainerItemDisplay containerItemDisplay;

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
        containerItemDisplay = containerDescriptor.containerItemDisplay;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField(containerDescriptor.containerName, TitleStyle);

        string containerName = EditorGUILayout.TextField(new GUIContent("Container Name", "the name of the container, appearing in container related interactions"), containerDescriptor.containerName);
        HandleContainerName(containerName);


        bool isInteractive = EditorGUILayout.Toggle(new GUIContent("is Interactive", "Set if the container can be interacted with or not. Adds the ContainerInteractive file, which contains default interactions for the container"), containerDescriptor.isInteractive);
        HandleIsInteractive(isInteractive);


        if (containerDescriptor.isInteractive)
        {
            bool hasUi = EditorGUILayout.Toggle(new GUIContent("has UI", "Set if the container has an UI"), containerDescriptor.hasUi);
            HandleHasUi(hasUi);

            bool isOpenable = EditorGUILayout.Toggle(new GUIContent("is Openable", "Set if the container has an open/close interaction"), containerDescriptor.isOpenable);
            HandleIsOpenable(isOpenable);

            bool hasCustomInteraction = EditorGUILayout.Toggle(new GUIContent("has custom interaction", "Set if the container should use the default interaction of ContainerInteractive.cs, or custom ones in another script"), containerDescriptor.hasCustomInteraction);
            HandleCustomInteraction(hasCustomInteraction);
        }

        if (containerDescriptor.isOpenable)
        {
            bool onlyStoreWhenOpen = EditorGUILayout.Toggle(new GUIContent("Only store when open", "Set if objects can be stored in the container without using the open interaction first"), containerDescriptor.onlyStoreWhenOpen);
            HandleOnlyStoreWhenOpen(onlyStoreWhenOpen);       
        }
        else if(containerDescriptor.hasUi)
        {  
            // check if the gameObject has a open animation
            foreach (AnimatorControllerParameter controllerParameter in containerDescriptor.gameObject.GetComponent<Animator>().parameters)
            {
                if (controllerParameter.name == "Open")
                {
                    bool openWhenContainerViewed = EditorGUILayout.Toggle(new GUIContent("open when container viewed", "Set if the open animation should run when the container UI is opened"), containerDescriptor.openWhenContainerViewed);
                    HandleOpenWhenContainerViewed(openWhenContainerViewed);
                }
            }      
        }

        if (containerDescriptor.hasUi)
        {
            float maxDistance = EditorGUILayout.FloatField(new GUIContent("Max distance", "max distance between the observer and the container before the UI closes on it's own"), containerDescriptor.maxDistance);
            HandleMaxDistance(maxDistance);
        }

        Vector2Int size = EditorGUILayout.Vector2IntField(new GUIContent("Size", "Defines the size of the container, every item takes a defined place inside a container"), containerDescriptor.size);
        HandleSize(size);

        Filter startFilter = (Filter)EditorGUILayout.ObjectField(new GUIContent("Filter", "Filter on the container, controls what can go in the container"), containerDescriptor.startFilter, typeof(Filter), true);
        HandleStartFilter(startFilter);

        bool hideItems = EditorGUILayout.Toggle(new GUIContent("Hide items", "Set if items should be attached as children of the container"), containerDescriptor.hideItems);
        HandleHideItems(hideItems);

        if (!hideItems)
        {
            Vector3 attachmentOffset = EditorGUILayout.Vector3Field(new GUIContent("Attachment Offset", "define the position of the items inside the container"), containerDescriptor.attachmentOffset);
            HandleAttachmentOffset(attachmentOffset);

            bool hasCustomDisplay = EditorGUILayout.Toggle(new GUIContent("Has custom display", "adds the container item display script, defines custom positions for items in the container"), containerDescriptor.hasCustomDisplay);
            HandleHasCustomDisplay(hasCustomDisplay);

            if (hasCustomDisplay)
            {
                int numberDisplay = EditorGUILayout.IntField(new GUIContent("number of display", "the number of items to display in custom position"), containerDescriptor.numberDisplay);
                HandleNumberDisplay(numberDisplay);

                SerializedProperty sp = serializedObject.FindProperty("displays");
                sp.arraySize = numberDisplay;
                for (int i = 0; i < sp.arraySize; ++i)
                {
                    SerializedProperty transformProp = sp.GetArrayElementAtIndex(i);
                    EditorGUILayout.PropertyField(transformProp, new GUIContent("Element " + i));
                }
            }
        }

        bool attachItems = EditorGUILayout.Toggle(new GUIContent("Attach Items", "Set if items should be attached as children of the container game object"), containerDescriptor.attachItems);
        HandleAttachItems(attachItems);




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
            AddSync();
            SerializedProperty sp = serializedObject.FindProperty("initialized");
            sp.boolValue = true;
            serializedObject.ApplyModifiedProperties();
        }
    }

    private void HandleNumberDisplay(int numberDisplay)
    {
        SerializedProperty sp = serializedObject.FindProperty("numberDisplay");
        sp.intValue = numberDisplay;
        serializedObject.ApplyModifiedProperties();
    }

    private void HandleHasCustomDisplay(bool hasCustomDisplay)
    {
        SerializedProperty sp = serializedObject.FindProperty("hasCustomDisplay");
        sp.boolValue = hasCustomDisplay;
        serializedObject.ApplyModifiedProperties();
        if(hasCustomDisplay && containerDescriptor.containerItemDisplay == null)
        {
            AddCustomDisplay();
        }
        if(!hasCustomDisplay && containerDescriptor.containerItemDisplay != null)
        {
            RemoveCustomDisplay();
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

    private void AddCustomDisplay()
    {
        SerializedProperty sp = serializedObject.FindProperty("containerItemDisplay");
        sp.objectReferenceValue = containerDescriptor.gameObject.AddComponent<ContainerItemDisplay>();
        serializedObject.ApplyModifiedProperties();
        containerDescriptor.containerItemDisplay.containerDescriptor = containerDescriptor;
    }

    private void RemoveCustomDisplay()
    {
        DestroyImmediate(containerItemDisplay, true);
    }

    private void AddAttached()
    {
        containerDescriptor.attachedContainer = containerDescriptor.gameObject.AddComponent<AttachedContainer>();
        containerDescriptor.attachedContainer.containerDescriptor = containerDescriptor;
    }

    private void AddSync()
    {
        if(containerDescriptor.gameObject.GetComponent<ContainerSync>() == null)
        {
            SerializedProperty sp = serializedObject.FindProperty("containerSync");
            sp.objectReferenceValue = containerDescriptor.gameObject.AddComponent<ContainerSync>();
            serializedObject.ApplyModifiedProperties();           
        }
        
    }

    private void RemoveSync()
    {
        GameObject g = Selection.activeGameObject;
        if(g != null)
        {
            var containerDescriptors = g.GetComponents<ContainerDescriptor>();
            if (containerDescriptors != null && containerDescriptors.Length == 0)
            {
                DestroyImmediate(g.GetComponent<ContainerSync>());
            }
        }
    }

    private void OnDestroy()
    {
        if(containerDescriptor == null)
        {
            DestroyImmediate(attachedContainer, true);
            DestroyImmediate(containerInteractive, true);
            RemoveSync();
        }
    }
}



	
