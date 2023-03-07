//C# Example (LookAtPointEditor.cs)
using UnityEditor;
using UnityEngine;
using SS3D.Systems.Inventory.Containers;
using FishNet.Object;
using SS3D.Systems;


/// <summary>
/// Class handling the inspector display of a containerDescriptor.
/// It allows displaying only compatible parameters in editor, as well as adding and removing necessary
/// scripts to make the container work.
/// </summary>
[CustomEditor(typeof(ContainerDescriptor))]
public class ContainerDescriptorEditor : Editor
{
    private ContainerDescriptor containerDescriptor;
    private GUIStyle TitleStyle;
    private bool showIcon = false;

    // References to container related scripts to allow for automating set up and destroying.
    private ContainerInteractive containerInteractive;
    private ContainerItemDisplay containerItemDisplay;

    public void OnEnable()
    {
        // Set the container title in editor.
        TitleStyle = new GUIStyle();
        TitleStyle.fontSize = 13;
        TitleStyle.fontStyle = FontStyle.Bold;
        TitleStyle.normal.textColor = Color.white;

        containerDescriptor = (ContainerDescriptor)target;
        if (containerDescriptor.AutomaticContainerSetUp)
        {
            AddBase();
            containerInteractive = containerDescriptor.ContainerInteractive;
            containerItemDisplay = containerDescriptor.ContainerItemDisplay;
        }


    }


    public override void OnInspectorGUI()
    {

        EditorGUILayout.LabelField(containerDescriptor.ContainerName, TitleStyle);
        serializedObject.Update();

        // If automatic set up is off, just display every container related parameters.
        if (!containerDescriptor.AutomaticContainerSetUp)
        {
            DrawDefaultInspector();
            return;
        }
        else
        {
            AddBase();
            containerInteractive = containerDescriptor.ContainerInteractive;
            containerItemDisplay = containerDescriptor.ContainerItemDisplay;
        }

        bool automaticContainerSetUp = EditorGUILayout.Toggle(
            new GUIContent("Automatic container setup", ""),
            containerDescriptor.AutomaticContainerSetUp);
        HandleAutomaticContainerSetUp(automaticContainerSetUp);

        string containerName = EditorGUILayout.TextField(
            new GUIContent("Container Name", "the name of the container, appearing in container related interactions"),
            containerDescriptor.ContainerName);
        HandleContainerName(containerName);


        bool isInteractive = EditorGUILayout.Toggle(
            new GUIContent("is Interactive", "Set if the container can be interacted with or not. Adds the ContainerInteractive file, which contains default interactions for the container"),
            containerDescriptor.IsInteractive);
        HandleIsInteractive(isInteractive);


        if (containerDescriptor.IsInteractive)
        {
            bool hasUi = EditorGUILayout.Toggle(
                new GUIContent("has UI", "Set if the container has an UI"),
                containerDescriptor.HasUi);
            HandleHasUi(hasUi);

            bool isOpenable = EditorGUILayout.Toggle(
                new GUIContent("is Openable", "Set if the container has an open/close interaction"),
                containerDescriptor.IsOpenable);
            HandleIsOpenable(isOpenable);

            bool hasCustomInteraction = EditorGUILayout.Toggle(
                new GUIContent("has custom interaction", "Set if the container should use the default interaction of ContainerInteractive.cs, or custom ones in another script"),
                containerDescriptor.HasCustomInteraction);
            HandleCustomInteraction(hasCustomInteraction);
        }

        if (containerDescriptor.IsOpenable)
        {
            bool onlyStoreWhenOpen = EditorGUILayout.Toggle(
                new GUIContent("Only store when open", "Set if objects can be stored in the container without using the open interaction first"),
                containerDescriptor.OnlyStoreWhenOpen);
            HandleOnlyStoreWhenOpen(onlyStoreWhenOpen);
        }

        if(containerDescriptor.HasUi)
        {
            // Check if there's an animation with an Open parameter before allowing the openWhenContainerViewed field.
            var animator = containerDescriptor.gameObject.GetComponent<Animator>();
            if (animator != null)
            {
                foreach (AnimatorControllerParameter controllerParameter in containerDescriptor.gameObject.GetComponent<Animator>().parameters)
                {
                    if (controllerParameter.name != "Open") { continue; }

                    bool openWhenContainerViewed = EditorGUILayout.Toggle(
                        new GUIContent("open when container viewed", "Set if the open animation should run when the container UI is viewed"),
                        containerDescriptor.OpenWhenContainerViewed);
                    HandleOpenWhenContainerViewed(openWhenContainerViewed);
                }
            }
        }

        if (containerDescriptor.HasUi)
        {
            float maxDistance = EditorGUILayout.FloatField(
                new GUIContent("Max distance", "max distance between the observer and the container before the UI closes on it's own"),
                containerDescriptor.MaxDistance);
            HandleMaxDistance(maxDistance);
        }

        Vector2Int size = EditorGUILayout.Vector2IntField(
            new GUIContent("Size", "Defines the size of the container, every item takes a defined place inside a container"),
            containerDescriptor.Size);
        HandleSize(size);

        bool hideItems = EditorGUILayout.Toggle(
            new GUIContent("Hide items", "Set if items should be attached as children of the container"),
            containerDescriptor.HideItems);
        HandleHideItems(hideItems);

        if (!hideItems)
        {
            Vector3 attachmentOffset = EditorGUILayout.Vector3Field(
                new GUIContent("Attachment Offset", "define the position of the items inside the container"),
                containerDescriptor.AttachmentOffset);
            HandleAttachmentOffset(attachmentOffset);

            bool hasCustomDisplay = EditorGUILayout.Toggle(
                new GUIContent("Has custom display", "adds the container item display script, defines custom positions for items in the container"),
                containerDescriptor.HasCustomDisplay);
            HandleHasCustomDisplay(hasCustomDisplay);

            if (hasCustomDisplay)
            {
                int numberDisplay = EditorGUILayout.IntField(
                    new GUIContent("number of display", "the number of items to display in custom position"),
                    containerDescriptor.NumberDisplay);
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

        bool attachItems = EditorGUILayout.Toggle(
            new GUIContent("Attach Items", "Set if items should be attached as children of the container game object"),
            containerDescriptor.AttachItems);
        HandleAttachItems(attachItems);

        Filter startFilter = (Filter)EditorGUILayout.ObjectField(
            new GUIContent("Filter", "Filter on the container, controls what can go in the container"),
            containerDescriptor.StartFilter, typeof(Filter), true);
        HandleStartFilter(startFilter);

        ContainerType containerType = (ContainerType) EditorGUILayout.EnumFlagsField(
    new GUIContent("Container type", "Container type mostly allow to discriminate between diffent containers on a single prefab."),
     containerDescriptor.Type);
        HandleContainerType(containerType);

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
            if (containerDescriptor.IsOpenable)
            {
                Sprite openIcon = (Sprite)EditorGUILayout.ObjectField(
                    "Open container icon", containerDescriptor.OpenIcon, typeof(Sprite), true);
                SerializedProperty sp = serializedObject.FindProperty("OpenIcon");
                sp.objectReferenceValue = openIcon;
            }

            if (containerDescriptor.IsInteractive)
            {
                Sprite storeIcon = (Sprite)EditorGUILayout.ObjectField(
                    "Store container icon", containerDescriptor.StoreIcon, typeof(Sprite), true);
                SerializedProperty sp = serializedObject.FindProperty("StoreIcon");
                sp.objectReferenceValue = storeIcon;
            }

            if (containerDescriptor.HasUi)
            {
                Sprite viewIcon = (Sprite)EditorGUILayout.ObjectField(
                    "View container icon", containerDescriptor.ViewIcon, typeof(Sprite), true);
                SerializedProperty sp = serializedObject.FindProperty("ViewIcon");
                sp.objectReferenceValue = viewIcon;
            }

            if (containerDescriptor.IsInteractive && !containerDescriptor.HasUi)
            {
                Sprite takeIcon = (Sprite)EditorGUILayout.ObjectField(
                    "Take container icon", containerDescriptor.TakeIcon, typeof(Sprite), true);
                SerializedProperty sp = serializedObject.FindProperty("TakeIcon");
                sp.objectReferenceValue = takeIcon;
            }
        }
    }

    /// <summary>
    /// Creates a basic container by adding all necessary components to the game object.
    /// </summary>
    public void AddBase()
    {
        if (!containerDescriptor.Initialized)
        {
            AddContainer();

            HandleIsInteractive(true);
            //AddSync();
            SerializedProperty sp = serializedObject.FindProperty("Initialized");
            sp.boolValue = true;
            serializedObject.ApplyModifiedProperties();
        }
    }

    private void HandleAutomaticContainerSetUp(bool automaticContainerSetUp)
    {
        SerializedProperty sp = serializedObject.FindProperty("AutomaticContainerSetUp");
        sp.boolValue = automaticContainerSetUp;
        serializedObject.ApplyModifiedProperties();
    }

    private void HandleContainer(Container container)
    {
        SerializedProperty sp = serializedObject.FindProperty("Container");
        sp.objectReferenceValue = container;
        serializedObject.ApplyModifiedProperties();
    }

    private void HandleNumberDisplay(int numberDisplay)
    {
        SerializedProperty sp = serializedObject.FindProperty("NumberDisplay");
        sp.intValue = numberDisplay;
        serializedObject.ApplyModifiedProperties();
    }

    private void HandleHasCustomDisplay(bool hasCustomDisplay)
    {
        SerializedProperty sp = serializedObject.FindProperty("HasCustomDisplay");
        sp.boolValue = hasCustomDisplay;
        serializedObject.ApplyModifiedProperties();
        if(hasCustomDisplay && containerDescriptor.ContainerItemDisplay == null)
        {
            AddCustomDisplay();
        }
        if(!hasCustomDisplay && containerDescriptor.ContainerItemDisplay != null)
        {
            RemoveCustomDisplay();
        }
    }

    private void HandleHasUi(bool hasUi)
    {
        SerializedProperty sp = serializedObject.FindProperty("HasUi");
        sp.boolValue = hasUi;
        serializedObject.ApplyModifiedProperties();
    }

    private void HandleOpenWhenContainerViewed(bool openWhenContainerViewed)
    {
        SerializedProperty sp = serializedObject.FindProperty("OpenWhenContainerViewed");
        sp.boolValue = openWhenContainerViewed;
        serializedObject.ApplyModifiedProperties();
    }

    private void HandleContainerType(ContainerType containerType)
    {
        SerializedProperty sp = serializedObject.FindProperty("Type");
        sp.enumValueFlag = (int) containerType;
        serializedObject.ApplyModifiedProperties();
    }

    private void HandleStartFilter(Filter startFilter)
    {
        SerializedProperty sp = serializedObject.FindProperty("StartFilter");
        sp.objectReferenceValue = startFilter;
        serializedObject.ApplyModifiedProperties();
    }
    private void HandleSize(Vector2Int size)
    {
        SerializedProperty sp = serializedObject.FindProperty("Size");
        sp.vector2IntValue = size;
        serializedObject.ApplyModifiedProperties();
        containerDescriptor.Container.Size = size;
    }
    private void HandleAttachmentOffset(Vector3 attachmentOffset)
    {
        SerializedProperty sp = serializedObject.FindProperty("AttachmentOffset");
        sp.vector3Value = attachmentOffset;
        serializedObject.ApplyModifiedProperties();
    }
    private void HandleAttachItems(bool attachItems)
    {
        SerializedProperty sp = serializedObject.FindProperty("AttachItems");
        sp.boolValue = attachItems;
        serializedObject.ApplyModifiedProperties();
    }

    private void HandleMaxDistance(float maxDistance)
    {
        SerializedProperty sp = serializedObject.FindProperty("MaxDistance");
        sp.floatValue = maxDistance;
        serializedObject.ApplyModifiedProperties();
    }
    private void HandleIsInteractive(bool isInteractive)
    {
        SerializedProperty sp = serializedObject.FindProperty("IsInteractive");
        sp.boolValue = isInteractive;
        serializedObject.ApplyModifiedProperties();
        if (isInteractive && containerDescriptor.ContainerInteractive == null)
        {
            AddInteractive();
        }
        else if (!isInteractive && containerDescriptor.ContainerInteractive != null)
        {
            RemoveInteractive();
        }
    }

    private void HandleCustomInteraction(bool hasCustomInteraction)
    {
        SerializedProperty sp = serializedObject.FindProperty("HasCustomInteraction");
        sp.boolValue = hasCustomInteraction;
        serializedObject.ApplyModifiedProperties();
    }

    private void HandleContainerName(string containerName)
    {
        SerializedProperty sp = serializedObject.FindProperty("ContainerName");
        sp.stringValue = containerName;
        serializedObject.ApplyModifiedProperties();
    }
    private void HandleIsOpenable(bool isOpenable)
    {

        SerializedProperty sp = serializedObject.FindProperty("IsOpenable");
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
        SerializedProperty sp = serializedObject.FindProperty("OnlyStoreWhenOpen");
        sp.boolValue = onlyStoreWhenOpen;
        serializedObject.ApplyModifiedProperties();
    }
    private void HandleHideItems(bool hideItems)
    {
        SerializedProperty sp = serializedObject.FindProperty("HideItems");
        sp.boolValue = hideItems;
        serializedObject.ApplyModifiedProperties();
    }
    private void AddInteractive()
    {
        SerializedProperty sp = serializedObject.FindProperty("ContainerInteractive");
        GameObject networkedParent = GetParentNetworkIdentity(containerDescriptor.gameObject);
        sp.objectReferenceValue = networkedParent != null ? networkedParent.AddComponent<ContainerInteractive>() : containerDescriptor.gameObject.AddComponent<ContainerInteractive>();
        serializedObject.ApplyModifiedProperties();
        containerDescriptor.ContainerInteractive.containerDescriptor = containerDescriptor;
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
        SerializedProperty sp = serializedObject.FindProperty("ContainerItemDisplay");
        sp.objectReferenceValue = containerDescriptor.gameObject.AddComponent<ContainerItemDisplay>();
        serializedObject.ApplyModifiedProperties();
        containerDescriptor.ContainerItemDisplay.containerDescriptor = containerDescriptor;
    }

    private void RemoveCustomDisplay()
    {
        DestroyImmediate(containerItemDisplay, true);
    }


    private void AddContainer()
    {
        SerializedProperty sp = serializedObject.FindProperty("Container");
        sp.objectReferenceValue = containerDescriptor.gameObject.AddComponent<Container>();
        serializedObject.ApplyModifiedProperties();
    }

    private GameObject GetParentNetworkIdentity(GameObject g)
    {
        var networkIdentity = g.GetComponentInParent<NetworkObject>();
        return networkIdentity ? networkIdentity.gameObject : null;
    }

    private void OnDestroy()
    {
        if(containerDescriptor == null)
        {
            DestroyImmediate(containerInteractive, true);
        }
    }
}




