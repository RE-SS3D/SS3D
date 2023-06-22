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
[CustomEditor(typeof(AttachedContainer), true)]
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
        var SerializedAutomaticContainerSetUp = serializedObject.FindProperty("_automaticContainerSetUp");
        if (SerializedAutomaticContainerSetUp.boolValue)
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

        var SerializedAutomaticContainerSetUp = serializedObject.FindProperty("_automaticContainerSetUp");
        // If automatic set up is off, just display every container related parameters.
        if (!SerializedAutomaticContainerSetUp.boolValue)
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
            SerializedAutomaticContainerSetUp.boolValue);
        HandleAutomaticContainerSetUp(automaticContainerSetUp);

        string containerName = EditorGUILayout.TextField(
            new GUIContent("Container Name", "the name of the container, appearing in container related interactions"),
            attachedContainer.ContainerName);
        HandleContainerName(containerName);


        bool isInteractive = EditorGUILayout.Toggle(
            new GUIContent("is Interactive", "Set if the container can be interacted with or not. Adds the ContainerInteractive file, which contains default interactions for the container"),
            attachedContainer.IsInteractive);
        HandleIsInteractive(isInteractive);

        bool isSlotInUI = EditorGUILayout.Toggle(
    new GUIContent("is slot in UI", "Set if the container should appear as a slot in the inventory UI"),
    attachedContainer.IsSlotInUI);
        HandleIsSlotInUI(isSlotInUI);



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

        var serializedHideItem = serializedObject.FindProperty("_hideItems");
        bool hideItems = EditorGUILayout.Toggle(
            new GUIContent("Hide items", "Set if items should be attached as children of the container"),
            serializedHideItem.boolValue);
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

        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// Creates a basic container by adding all necessary components to the game object.
    /// </summary>
    public void AddBase()
    {
        var SerializedInitialized = serializedObject.FindProperty("_initialized");
        if (!SerializedInitialized.boolValue)
        {

            HandleIsInteractive(true);
            //AddSync();
            SerializedInitialized.boolValue = true;
            serializedObject.ApplyModifiedProperties();
        }
    }

    private void HandleAutomaticContainerSetUp(bool automaticContainerSetUp)
    {
        SerializedProperty sp = serializedObject.FindProperty("_automaticContainerSetUp");
        sp.boolValue = automaticContainerSetUp;
        serializedObject.ApplyModifiedProperties();
    }

    private void HandleNumberDisplay(int numberDisplay)
    {
        SerializedProperty sp = serializedObject.FindProperty("_numberDisplay");
        sp.intValue = numberDisplay;
        serializedObject.ApplyModifiedProperties();
    }

    private void HandleHasCustomDisplay(bool hasCustomDisplay)
    {
        SerializedProperty sp = serializedObject.FindProperty("_hasCustomDisplay");
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
        SerializedProperty sp = serializedObject.FindProperty("_hasUi");
        sp.boolValue = hasUi;
        serializedObject.ApplyModifiedProperties();
    }

    private void HandleOpenWhenContainerViewed(bool openWhenContainerViewed)
    {
        SerializedProperty sp = serializedObject.FindProperty("_openWhenContainerViewed");
        sp.boolValue = openWhenContainerViewed;
        serializedObject.ApplyModifiedProperties();
    }

    private void HandleIsSlotInUI(bool openWhenContainerViewed)
    {
        SerializedProperty sp = serializedObject.FindProperty("_isSlotInUI");
        sp.boolValue = openWhenContainerViewed;
        serializedObject.ApplyModifiedProperties();
    }

    private void HandleContainerType(ContainerType containerType)
    {
        SerializedProperty sp = serializedObject.FindProperty("_type");
        sp.enumValueFlag = (int) containerType;
        serializedObject.ApplyModifiedProperties();
    }

    private void HandleStartFilter(Filter startFilter)
    {
        SerializedProperty sp = serializedObject.FindProperty("_startFilter");
        sp.objectReferenceValue = startFilter;
        serializedObject.ApplyModifiedProperties();
    }
    private void HandleSize(Vector2Int size)
    {
        SerializedProperty sp = serializedObject.FindProperty("_size");
        sp.vector2IntValue = size;
        serializedObject.ApplyModifiedProperties();
    }
    private void HandleAttachmentOffset(Vector3 attachmentOffset)
    {
        SerializedProperty sp = serializedObject.FindProperty("_attachmentOffset");
        sp.vector3Value = attachmentOffset;
        serializedObject.ApplyModifiedProperties();
    }
    private void HandleAttachItems(bool attachItems)
    {
        SerializedProperty sp = serializedObject.FindProperty("_attachItems");
        sp.boolValue = attachItems;
        serializedObject.ApplyModifiedProperties();
    }

    private void HandleMaxDistance(float maxDistance)
    {
        SerializedProperty sp = serializedObject.FindProperty("_maxDistance");
        sp.floatValue = maxDistance;
        serializedObject.ApplyModifiedProperties();
    }
    private void HandleIsInteractive(bool isInteractive)
    {
        SerializedProperty sp = serializedObject.FindProperty("_isInteractive");
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
        SerializedProperty sp = serializedObject.FindProperty("_hasCustomInteraction");
        sp.boolValue = hasCustomInteraction;
        serializedObject.ApplyModifiedProperties();
    }

    private void HandleContainerName(string containerName)
    {
        SerializedProperty sp = serializedObject.FindProperty("_containerName");
        sp.stringValue = containerName;
        serializedObject.ApplyModifiedProperties();
    }
    private void HandleIsOpenable(bool isOpenable)
    {

        SerializedProperty sp = serializedObject.FindProperty("_isOpenable");
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
        SerializedProperty sp = serializedObject.FindProperty("_onlyStoreWhenOpen");
        sp.boolValue = onlyStoreWhenOpen;
        serializedObject.ApplyModifiedProperties();
    }
    private void HandleHideItems(bool hideItems)
    {
        SerializedProperty sp = serializedObject.FindProperty("_hideItems");
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




