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

        HandleContainerName();
        HandleIsInteractive();
        if (containerDescriptor.isInteractive)
        {
            HandleCustomInteraction();
        }

        if (containerDescriptor.hasCustomInteraction)
        {
            HandleCustomInteractionScript();
        }

        HandleIsOpenable();

        if (containerDescriptor.isOpenable)
        {
            HandleOnlyStoreWhenOpen();       
        }
        else
        {
            HandleOpenWhenContainerViewed();
        }

        if (containerDescriptor.containerType != ContainerType.Hidden && containerDescriptor.containerType != ContainerType.Pile)
        {
            HandleMaxDistance();
        }

        HandleContainerType();
        HandleSize();
        HandleStartFilter();

        if(containerDescriptor.containerType != ContainerType.Hidden)
        {
            HandleHideItems();
        }

        HandleAttachItems();
        HandleAttachmentOffset();

        ShowIcons();
        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// determines which type a container can be depending if it's interactive and openable.
    /// </summary>
    public bool CheckEnabledType(System.Enum e)
    {
        ContainerType containerType = (ContainerType)e;

        //
        if (containerDescriptor.isOpenable)
        {
            if (containerType == ContainerType.Normal || containerType == ContainerType.Pile)
                return true;
            else
                return false;
        }
        if (containerDescriptor.isInteractive && !containerDescriptor.isOpenable)
        {
            return true;
        }
        return false;
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

            if (containerDescriptor.containerType == ContainerType.Normal || containerDescriptor.containerType == ContainerType.Pile)
            {
                Sprite storeIcon = (Sprite)EditorGUILayout.ObjectField("Store container icon", containerDescriptor.storeIcon, typeof(Sprite), true);
                SerializedProperty sp = serializedObject.FindProperty("storeIcon");
                sp.objectReferenceValue = storeIcon;
            }

            if (containerDescriptor.containerType == ContainerType.Normal)
            {
                Sprite viewIcon = (Sprite)EditorGUILayout.ObjectField("View container icon", containerDescriptor.viewIcon, typeof(Sprite), true);
                SerializedProperty sp = serializedObject.FindProperty("viewIcon");
                sp.objectReferenceValue = viewIcon;
            }

            if (containerDescriptor.containerType == ContainerType.Pile)
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
            AddInteractive();
            SerializedProperty sp = serializedObject.FindProperty("initialized");
            sp.boolValue = true;
            serializedObject.ApplyModifiedProperties();
        }
    }
    private void HandleOpenWhenContainerViewed()
    {
        // check if the gameObject has a open animation
        foreach(AnimatorControllerParameter controllerParameter in containerDescriptor.gameObject.GetComponent<Animator>().parameters)
        {
            if(controllerParameter.name == "Open")
            {
                bool openWhenContainerViewed = EditorGUILayout.Toggle("open when container viewed", containerDescriptor.attachItems);
                Debug.Log(openWhenContainerViewed);
                SerializedProperty sp = serializedObject.FindProperty("openWhenContainerViewed");
                sp.boolValue = openWhenContainerViewed;
                Debug.Log(sp.boolValue);
                serializedObject.ApplyModifiedProperties();
            }
        }
    }

    private void HandleStartFilter()
    {
        Filter startFilter = (Filter)EditorGUILayout.ObjectField("Filter", containerDescriptor.startFilter, typeof(Filter), true);
        SerializedProperty sp = serializedObject.FindProperty("startFilter");
        sp.objectReferenceValue = startFilter;
        serializedObject.ApplyModifiedProperties();
    }
    private void HandleSize()
    {
        Vector2Int size = EditorGUILayout.Vector2IntField("Size", containerDescriptor.size);
        SerializedProperty sp = serializedObject.FindProperty("size");
        sp.vector2IntValue = size;
        serializedObject.ApplyModifiedProperties();
    }
    private void HandleAttachmentOffset()
    {
        Vector3 attachmentOffset = EditorGUILayout.Vector3Field("Attachment Offset", containerDescriptor.attachmentOffset);
        SerializedProperty sp = serializedObject.FindProperty("attachmentOffset");
        sp.vector3Value = attachmentOffset;
        serializedObject.ApplyModifiedProperties();
    }
    private void HandleAttachItems()
    {
        bool attachItems = EditorGUILayout.Toggle("Attach Items", containerDescriptor.attachItems);
        SerializedProperty sp = serializedObject.FindProperty("attachItems");
        sp.boolValue = attachItems;
        serializedObject.ApplyModifiedProperties();
    }
    private void HandleContainerType()
    {
        ContainerType containerType = (ContainerType)EditorGUILayout.EnumPopup(new GUIContent("Container type"), containerDescriptor.containerType, CheckEnabledType, false);
        SerializedProperty sp = serializedObject.FindProperty("containerType");
        sp.enumValueIndex = (int)containerType;
        serializedObject.ApplyModifiedProperties();
    }
    private void HandleMaxDistance()
    {
        float maxDistance = EditorGUILayout.FloatField("Max distance", containerDescriptor.maxDistance);
        SerializedProperty sp = serializedObject.FindProperty("maxDistance");
        sp.floatValue = maxDistance;
        serializedObject.ApplyModifiedProperties();
    }
    private void HandleIsInteractive()
    {
        bool isInteractive = EditorGUILayout.Toggle("is Interactive", containerDescriptor.isInteractive);
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

    private void HandleCustomInteraction()
    {
        bool hasCustomInteraction = EditorGUILayout.Toggle("has custom interaction", containerDescriptor.hasCustomInteraction);
        SerializedProperty sp = serializedObject.FindProperty("hasCustomInteraction");
        sp.boolValue = hasCustomInteraction;
        serializedObject.ApplyModifiedProperties();
    }

    private void HandleCustomInteractionScript()
    {
        InteractionTargetNetworkBehaviour customInteractionScript = (InteractionTargetNetworkBehaviour)
            EditorGUILayout.ObjectField("custom interaction script", containerDescriptor.customInteractionScript, typeof(InteractionTargetNetworkBehaviour), true);

            SerializedProperty sp = serializedObject.FindProperty("customInteractionScript");
            sp.objectReferenceValue = customInteractionScript;
            serializedObject.ApplyModifiedProperties();
    }

    private void HandleContainerName()
    {
        string containerName = EditorGUILayout.TextField("Container Name", containerDescriptor.containerName);
        SerializedProperty sp = serializedObject.FindProperty("containerName");
        sp.stringValue = containerName;
        serializedObject.ApplyModifiedProperties();
    }
    private void HandleIsOpenable()
    {
        bool isOpenable = EditorGUILayout.Toggle("is Openable", containerDescriptor.isOpenable);
        SerializedProperty sp = serializedObject.FindProperty("isOpenable");
        sp.boolValue = isOpenable;
        serializedObject.ApplyModifiedProperties();

        // Openable containers can't be hidden.
        if(containerDescriptor.containerType == ContainerType.Hidden && isOpenable)
        {
            SerializedProperty sp2 = serializedObject.FindProperty("containerType");
            sp2.enumValueIndex = (int) ContainerType.Normal;
            serializedObject.ApplyModifiedProperties();
        }

        // Openable container are always interactive.
        if (isOpenable)
        {
            SerializedProperty sp3 = serializedObject.FindProperty("isInteractive");
            sp3.boolValue = true;
            serializedObject.ApplyModifiedProperties();
        }    
    }
    private void HandleOnlyStoreWhenOpen()
    {
        bool onlyStoreWhenOpen = EditorGUILayout.Toggle("Only store when open", containerDescriptor.onlyStoreWhenOpen);
        SerializedProperty sp = serializedObject.FindProperty("onlyStoreWhenOpen");
        sp.boolValue = onlyStoreWhenOpen;
        serializedObject.ApplyModifiedProperties();
    }
    private void HandleHideItems()
    {
        bool hideItems = EditorGUILayout.Toggle("Hide Items", containerDescriptor.hideItems);
        SerializedProperty sp = serializedObject.FindProperty("hideItems");
        if (containerDescriptor.containerType == ContainerType.Hidden)
        {
            sp.boolValue = true;
        }
        else
        {
            sp.boolValue = hideItems;
        }
        serializedObject.ApplyModifiedProperties();
    }
    private void AddInteractive()
    {
        SerializedProperty sp = serializedObject.FindProperty("isInteractive");
        sp.boolValue = true;
        serializedObject.ApplyModifiedProperties();

        SerializedProperty sp2 = serializedObject.FindProperty("containerInteractive");
        sp2.objectReferenceValue = containerDescriptor.gameObject.AddComponent<ContainerInteractive>();
        serializedObject.ApplyModifiedProperties();
        containerDescriptor.containerInteractive.containerDescriptor = containerDescriptor;
    }

    private void RemoveInteractive()
    {
        DestroyImmediate(containerInteractive, true);
        SerializedProperty sp = serializedObject.FindProperty("isInteractive");
        sp.boolValue = false;
        serializedObject.ApplyModifiedProperties();

        SerializedProperty sp2 = serializedObject.FindProperty("isOpenable");
        sp2.boolValue = false;
        serializedObject.ApplyModifiedProperties();

        SerializedProperty sp3 = serializedObject.FindProperty("onlyStoreWhenOpen");
        sp3.boolValue = false;
        serializedObject.ApplyModifiedProperties();

        SerializedProperty sp4= serializedObject.FindProperty("hasCustomInteraction");
        sp4.boolValue = false;
        serializedObject.ApplyModifiedProperties();
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
            RemoveContainer();
        }
    }

    /// <summary>
    /// Remove container related script on the target game object.
    /// </summary>
    private void RemoveContainer()
    {
        DestroyImmediate(attachedContainer, true);
        DestroyImmediate(containerInteractive, true);
    }

}



	
