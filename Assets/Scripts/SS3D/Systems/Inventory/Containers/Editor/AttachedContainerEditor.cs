//C# Example (LookAtPointEditor.cs)
using UnityEditor;
using UnityEngine;
using SS3D.Systems.Inventory.Containers;
using FishNet.Object;
using SS3D.Systems;


/// <summary>
/// Class handling the inspector display of a attachedContainer.
/// It allows displaying only compatible parameters in editor, as well as adding and removing necessary
/// scripts to make the container work.
/// </summary>
[CustomEditor(typeof(AttachedContainer))]
public class AttachedContainerEditor : Editor
{
    private AttachedContainer attachedContainer;
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

        attachedContainer = (AttachedContainer)target;
        if (attachedContainer.AutomaticContainerSetUp)
        {
            AddBase();
            containerInteractive = attachedContainer.ContainerInteractive;
            containerItemDisplay = attachedContainer.ContainerItemDisplay;
        }


    }


    public override void OnInspectorGUI()
    {

        EditorGUILayout.LabelField(attachedContainer.ContainerName, TitleStyle);
        serializedObject.Update();

        // If automatic set up is off, just display every container related parameters.
        if (!attachedContainer.AutomaticContainerSetUp)
        {
            DrawDefaultInspector();
            return;
        }
        else
        {
            AddBase();
            containerInteractive = attachedContainer.ContainerInteractive;
            containerItemDisplay = attachedContainer.ContainerItemDisplay;
        }

        bool automaticContainerSetUp = EditorGUILayout.Toggle(
            new GUIContent("Automatic container setup", ""),
            attachedContainer.AutomaticContainerSetUp);
        HandleAutomaticContainerSetUp(automaticContainerSetUp);

        string containerName = EditorGUILayout.TextField(
            new GUIContent("Container Name", "the name of the container, appearing in container related interactions"),
            attachedContainer.ContainerName);
        HandleContainerName(containerName);


        bool isInteractive = EditorGUILayout.Toggle(
            new GUIContent("is Interactive", "Set if the container can be interacted with or not. Adds the ContainerInteractive file, which contains default interactions for the container"),
            attachedContainer.IsInteractive);
        HandleIsInteractive(isInteractive);


        if (attachedContainer.IsInteractive)
        {
            bool hasUi = EditorGUILayout.Toggle(
                new GUIContent("has UI", "Set if the container has an UI"),
                attachedContainer.HasUi);
            HandleHasUi(hasUi);

            bool isOpenable = EditorGUILayout.Toggle(
                new GUIContent("is Openable", "Set if the container has an open/close interaction"),
                attachedContainer.IsOpenable);
            HandleIsOpenable(isOpenable);

            bool hasCustomInteraction = EditorGUILayout.Toggle(
                new GUIContent("has custom interaction", "Set if the container should use the default interaction of ContainerInteractive.cs, or custom ones in another script"),
                attachedContainer.HasCustomInteraction);
            HandleCustomInteraction(hasCustomInteraction);
        }

        if (attachedContainer.IsOpenable)
        {
            bool onlyStoreWhenOpen = EditorGUILayout.Toggle(
                new GUIContent("Only store when open", "Set if objects can be stored in the container without using the open interaction first"),
                attachedContainer.OnlyStoreWhenOpen);
            HandleOnlyStoreWhenOpen(onlyStoreWhenOpen);
        }

        if(attachedContainer.HasUi)
        {
            // Check if there's an animation with an Open parameter before allowing the openWhenContainerViewed field.
            var animator = attachedContainer.gameObject.GetComponent<Animator>();
            if (animator != null)
            {
                foreach (AnimatorControllerParameter controllerParameter in attachedContainer.gameObject.GetComponent<Animator>().parameters)
                {
                    if (controllerParameter.name != "Open") { continue; }

                    bool openWhenContainerViewed = EditorGUILayout.Toggle(
                        new GUIContent("open when container viewed", "Set if the open animation should run when the container UI is viewed"),
                        attachedContainer.OpenWhenContainerViewed);
                    HandleOpenWhenContainerViewed(openWhenContainerViewed);
                }
            }
        }

        if (attachedContainer.HasUi)
        {
            float maxDistance = EditorGUILayout.FloatField(
                new GUIContent("Max distance", "max distance between the observer and the container before the UI closes on it's own"),
                attachedContainer.MaxDistance);
            HandleMaxDistance(maxDistance);
        }

        Vector2Int size = EditorGUILayout.Vector2IntField(
            new GUIContent("Size", "Defines the size of the container, every item takes a defined place inside a container"),
            attachedContainer.Size);
        HandleSize(size);

        bool hideItems = EditorGUILayout.Toggle(
            new GUIContent("Hide items", "Set if items should be attached as children of the container"),
            attachedContainer.HideItems);
        HandleHideItems(hideItems);

        if (!hideItems)
        {
            Vector3 attachmentOffset = EditorGUILayout.Vector3Field(
                new GUIContent("Attachment Offset", "define the position of the items inside the container"),
                attachedContainer.AttachmentOffset);
            HandleAttachmentOffset(attachmentOffset);

            bool hasCustomDisplay = EditorGUILayout.Toggle(
                new GUIContent("Has custom display", "adds the container item display script, defines custom positions for items in the container"),
                attachedContainer.HasCustomDisplay);
            HandleHasCustomDisplay(hasCustomDisplay);

            if (hasCustomDisplay)
            {
                int numberDisplay = EditorGUILayout.IntField(
                    new GUIContent("number of display", "the number of items to display in custom position"),
                    attachedContainer.NumberDisplay);
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
            attachedContainer.AttachItems);
        HandleAttachItems(attachItems);

        Filter startFilter = (Filter)EditorGUILayout.ObjectField(
            new GUIContent("Filter", "Filter on the container, controls what can go in the container"),
            attachedContainer.StartFilter, typeof(Filter), true);
        HandleStartFilter(startFilter);

        ContainerType containerType = (ContainerType) EditorGUILayout.EnumFlagsField(
    new GUIContent("Container type", "Container type mostly allow to discriminate between diffent containers on a single prefab."),
     attachedContainer.Type);
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
            if (attachedContainer.IsOpenable)
            {
                Sprite openIcon = (Sprite)EditorGUILayout.ObjectField(
                    "Open container icon", attachedContainer.OpenIcon, typeof(Sprite), true);
                SerializedProperty sp = serializedObject.FindProperty("OpenIcon");
                sp.objectReferenceValue = openIcon;
            }

            if (attachedContainer.IsInteractive)
            {
                Sprite storeIcon = (Sprite)EditorGUILayout.ObjectField(
                    "Store container icon", attachedContainer.StoreIcon, typeof(Sprite), true);
                SerializedProperty sp = serializedObject.FindProperty("StoreIcon");
                sp.objectReferenceValue = storeIcon;
            }

            if (attachedContainer.HasUi)
            {
                Sprite viewIcon = (Sprite)EditorGUILayout.ObjectField(
                    "View container icon", attachedContainer.ViewIcon, typeof(Sprite), true);
                SerializedProperty sp = serializedObject.FindProperty("ViewIcon");
                sp.objectReferenceValue = viewIcon;
            }

            if (attachedContainer.IsInteractive && !attachedContainer.HasUi)
            {
                Sprite takeIcon = (Sprite)EditorGUILayout.ObjectField(
                    "Take container icon", attachedContainer.TakeIcon, typeof(Sprite), true);
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
        if (!attachedContainer.Initialized)
        {

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
        if(hasCustomDisplay && attachedContainer.ContainerItemDisplay == null)
        {
            AddCustomDisplay();
        }
        if(!hasCustomDisplay && attachedContainer.ContainerItemDisplay != null)
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
        if (isInteractive && attachedContainer.ContainerInteractive == null)
        {
            AddInteractive();
        }
        else if (!isInteractive && attachedContainer.ContainerInteractive != null)
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
        GameObject networkedParent = GetParentNetworkIdentity(attachedContainer.gameObject);
        sp.objectReferenceValue = networkedParent != null ? networkedParent.AddComponent<ContainerInteractive>() : attachedContainer.gameObject.AddComponent<ContainerInteractive>();
        serializedObject.ApplyModifiedProperties();
        attachedContainer.ContainerInteractive.attachedContainer = attachedContainer;
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
        sp.objectReferenceValue = attachedContainer.gameObject.AddComponent<ContainerItemDisplay>();
        serializedObject.ApplyModifiedProperties();
        attachedContainer.ContainerItemDisplay.attachedContainer = attachedContainer;
    }

    private void RemoveCustomDisplay()
    {
        DestroyImmediate(containerItemDisplay, true);
    }

    private GameObject GetParentNetworkIdentity(GameObject g)
    {
        var networkIdentity = g.GetComponentInParent<NetworkObject>();
        return networkIdentity ? networkIdentity.gameObject : null;
    }

    private void OnDestroy()
    {
        if(attachedContainer == null)
        {
            DestroyImmediate(containerInteractive, true);
        }
    }
}




