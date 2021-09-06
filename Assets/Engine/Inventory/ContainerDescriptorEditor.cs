//C# Example (LookAtPointEditor.cs)
using UnityEditor;
using UnityEngine;
using SS3D.Engine.Inventory;
using SS3D.Content.Furniture.Storage;

using System.Collections.Generic;

[CustomEditor(typeof(ContainerDescriptor))]
public class ContainerDescriptorEditor : Editor
{
    private ContainerDescriptor containerDescriptor;
    private GUIStyle TitleStyle;
    private bool showIcon = false;
    private AttachedContainer attachedContainer;
    private AttachedContainerGenerator attachedContainerGenerator;
    private ContainerSync containerSync;
    private OpenableContainer openableContainer;
    private StorageContainer storageContainer;
    private VisibleContainer visibleContainer;

    public void OnEnable()
    {
        TitleStyle = new GUIStyle();
        TitleStyle.fontSize = 13;
        TitleStyle.fontStyle = FontStyle.Bold;

        containerDescriptor = (ContainerDescriptor)target;
        AddBase();

        attachedContainer = containerDescriptor.attachedContainer;
        attachedContainerGenerator = containerDescriptor.attachedContainerGenerator;
        containerSync = containerDescriptor.containerSync;
        openableContainer = containerDescriptor.openableContainer;
        storageContainer = containerDescriptor.storageContainer;
        visibleContainer = containerDescriptor.visibleContainer;     
    }

    public override void OnInspectorGUI()
    {
        //VisibleContainer v = containerDescriptor.gameObject.GetComponent<VisibleContainer>();
        //DestroyImmediate(v);

        serializedObject.Update();

        EditorGUILayout.LabelField(containerDescriptor.containerName, TitleStyle);

        HandleContainerName();
        HandleIsOpenable();

        if (containerDescriptor.isOpenable)
        {
            HandleOnlyStoreWhenOpen();       
        }

        HandleIsStorage();

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

    
    public bool CheckEnabledType(System.Enum e)
    {
        ContainerType containerType = (ContainerType)e;

        if (containerDescriptor.isOpenable)
        {
            if (containerType == ContainerType.Normal || containerType == ContainerType.Pile)
                return true;
            else
                return false;
        }
        if (containerDescriptor.isStorage)
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
            AddGenerator();
            AddStorage();
            AddVisible();
            AddSync();
            containerDescriptor.initialized = true;
        }
    }

    private void HandleStartFilter()
    {
        Filter startFilter = (Filter)EditorGUILayout.ObjectField("Filter", containerDescriptor.startFilter, typeof(Filter), true);
        SerializedProperty sp = serializedObject.FindProperty("startFilter");
        sp.objectReferenceValue = startFilter;
    }
    private void HandleSize()
    {
        Vector2Int size = EditorGUILayout.Vector2IntField("Size", containerDescriptor.size);
        SerializedProperty sp = serializedObject.FindProperty("size");
        sp.vector2IntValue = size;
    }
    private void HandleAttachmentOffset()
    {
        Vector3 attachmentOffset = EditorGUILayout.Vector3Field("Attachment Offset", containerDescriptor.attachmentOffset);
        SerializedProperty sp = serializedObject.FindProperty("attachmentOffset");
        sp.vector3Value = attachmentOffset;
    }
    private void HandleAttachItems()
    {
        bool attachItems = EditorGUILayout.Toggle("Attach Items", containerDescriptor.attachItems);
        SerializedProperty sp = serializedObject.FindProperty("attachItems");
        sp.boolValue = attachItems;
    }
    private void HandleContainerType()
    {
        ContainerType containerType = (ContainerType)EditorGUILayout.EnumPopup(new GUIContent("Container type"), containerDescriptor.containerType, CheckEnabledType, false);
        SerializedProperty sp = serializedObject.FindProperty("containerType");
        sp.enumValueIndex = (int)containerType;

        if (containerType == ContainerType.Normal && containerDescriptor.visibleContainer == null)
        {
            AddVisible();
        }
        // Hidden containers are never visible. For pile containers, it's not clear yet as they could have an UI. It might change in the future.
        if (containerType == ContainerType.Hidden || containerType == ContainerType.Pile)
        {
            RemoveVisible();
        }
    }
    private void HandleMaxDistance()
    {
        float maxDistance = EditorGUILayout.FloatField("Max distance", containerDescriptor.maxDistance);
        SerializedProperty sp = serializedObject.FindProperty("maxDistance");
        sp.floatValue = maxDistance;
    }
    private void HandleIsStorage()
    {
        bool isStorage = EditorGUILayout.Toggle("isStorage", containerDescriptor.isStorage);
        SerializedProperty sp = serializedObject.FindProperty("isStorage");
        sp.boolValue = isStorage;
        if (isStorage && containerDescriptor.storageContainer == null)
        {
            Debug.Log("before add storage :" + containerDescriptor.storageContainer);
            AddStorage();
            Debug.Log("after add storage :" + containerDescriptor.storageContainer);
            RemoveOpenable();
        }
        else if (!isStorage && containerDescriptor.storageContainer != null)
        {
            RemoveStorage();
        }
    }
    private void HandleContainerName()
    {
        string containerName = EditorGUILayout.TextField("Container Name", containerDescriptor.containerName);
        SerializedProperty sp = serializedObject.FindProperty("containerName");
        sp.stringValue = containerName;
    }
    private void HandleIsOpenable()
    {
        bool isOpenable = EditorGUILayout.Toggle("is Openable", containerDescriptor.isOpenable);
        SerializedProperty sp = serializedObject.FindProperty("isOpenable");
        sp.boolValue = isOpenable;

        if (isOpenable && containerDescriptor.openableContainer == null)
        {
            RemoveStorage();
            AddOpenable();
            // Openable containers can't be hidden.
            if (containerDescriptor.containerType == ContainerType.Hidden) containerDescriptor.containerType = ContainerType.Normal;
        }
        else if (!isOpenable && containerDescriptor.openableContainer != null)
        {
            RemoveOpenable();
        }
    }

    private void HandleOnlyStoreWhenOpen()
    {
        bool onlyStoreWhenOpen = EditorGUILayout.Toggle("Only store when open", containerDescriptor.onlyStoreWhenOpen);
        SerializedProperty sp = serializedObject.FindProperty("onlyStoreWhenOpen");
        sp.boolValue = onlyStoreWhenOpen;
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
    }

    private void AddVisible()
    {
        SerializedProperty sp = serializedObject.FindProperty("visibleContainer");
        sp.objectReferenceValue = containerDescriptor.gameObject.AddComponent<VisibleContainer>();
        serializedObject.ApplyModifiedProperties();
        
        containerDescriptor.visibleContainer.containerDescriptor = containerDescriptor;
    }
    private void RemoveVisible()
    {
            DestroyImmediate(visibleContainer, true);
    }

    private void AddOpenable()
    {
        SerializedProperty sp = serializedObject.FindProperty("openableContainer");
        sp.objectReferenceValue = containerDescriptor.gameObject.AddComponent<OpenableContainer>();
        serializedObject.ApplyModifiedProperties();

        containerDescriptor.openableContainer.containerDescriptor = containerDescriptor;
        SerializedProperty sp2 = serializedObject.FindProperty("isOpenable");
        sp2.boolValue = true;
        serializedObject.ApplyModifiedProperties();
    }
    private void RemoveOpenable()
    {
        DestroyImmediate(openableContainer, true);
        containerDescriptor.isOpenable = false;
        SerializedProperty sp = serializedObject.FindProperty("isOpenable");
        sp.boolValue = false;
        serializedObject.ApplyModifiedProperties();
    }
    private void AddStorage()
    {
        SerializedProperty sp = serializedObject.FindProperty("storageContainer");
        sp.objectReferenceValue = containerDescriptor.gameObject.AddComponent<StorageContainer>();
        serializedObject.ApplyModifiedProperties();

        containerDescriptor.storageContainer.containerDescriptor = containerDescriptor;
        SerializedProperty sp2 = serializedObject.FindProperty("isStorage");
        sp2.boolValue = true;
        serializedObject.ApplyModifiedProperties();
    }

    private void RemoveStorage()
    {
        DestroyImmediate(storageContainer);
        SerializedProperty sp = serializedObject.FindProperty("isStorage");
        sp.boolValue = false;
        serializedObject.ApplyModifiedProperties();
    }

    private void AddGenerator()
    {
        containerDescriptor.attachedContainerGenerator = containerDescriptor.gameObject.AddComponent<AttachedContainerGenerator>();
        containerDescriptor.attachedContainerGenerator.containerDescriptor = containerDescriptor;
    }

    private void RemoveGenerator()
    {
        DestroyImmediate(attachedContainerGenerator, true);
    }

    private void AddAttached()
    {
        containerDescriptor.attachedContainer = containerDescriptor.gameObject.AddComponent<AttachedContainer>();
        containerDescriptor.attachedContainer.containerDescriptor = containerDescriptor;
    }

    private void RemoveAttached()
    {
        DestroyImmediate(attachedContainer, true);
    }

    private void AddSync()
    {
        // There should be only one container sync script for any game object.
        containerDescriptor.containerSync = containerDescriptor.gameObject.GetComponent<ContainerSync>();
        if (containerDescriptor.containerSync == null)
        {
            containerDescriptor.containerSync = containerDescriptor.gameObject.AddComponent<ContainerSync>();
        }
    }

    private void RemoveSync()
    {
        if (Selection.activeGameObject.GetComponent<AttachedContainer>() == null)
        {
            DestroyImmediate(containerSync, true);
        }          
    }

    private void OnDestroy()
    {
        if(containerDescriptor == null)
        {
            RemoveContainer();
        }
    }

    private void RemoveContainer()
    {
        DestroyImmediate(attachedContainer, true);
        DestroyImmediate(attachedContainerGenerator, true);
        DestroyImmediate(openableContainer, true);
        DestroyImmediate(visibleContainer, true);
        DestroyImmediate(storageContainer, true);
        if (Selection.activeGameObject.GetComponent<AttachedContainer>() == null)
        {
            DestroyImmediate(containerSync, true);
        }
    }

}



	
