using FishNet.Object;
using UnityEditor;
using UnityEngine;

namespace SS3D.Systems.Inventory.Containers.Editor
{
    /// <summary>
    /// Class handling the inspector display of a attachedContainer.
    /// It allows displaying only compatible parameters in editor, as well as adding and removing necessary
    /// scripts to make the container work.
    /// </summary>
    [CustomEditor(typeof(AttachedContainer), true)]
    public class AttachedContainerEditor : UnityEditor.Editor
    {
        private AttachedContainer _attachedContainer;

        private GUIStyle _titleStyle;

        private bool _showIcon;

        private ContainerInteractive _containerInteractive;

        private ContainerItemDisplay _containerItemDisplay;

        /// <summary>
        /// Creates a basic container by adding all necessary components to the game object.
        /// </summary>
        public void AddBase()
        {
            SerializedProperty serializedInitialized = serializedObject.FindProperty("_initialized");

            if (!serializedInitialized.boolValue)
            {
                HandleIsInteractive(true);

                // AddSync();
                serializedInitialized.boolValue = true;
                serializedObject.ApplyModifiedProperties();
            }
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField(_attachedContainer.ContainerName, _titleStyle);
            serializedObject.Update();

            SerializedProperty serializedAutomaticContainerSetUp = serializedObject.FindProperty("_automaticContainerSetUp");

            // If automatic set up is off, just display every container related parameters.
            if (!serializedAutomaticContainerSetUp.boolValue)
            {
                DrawDefaultInspector();

                return;
            }

            AddBase();
            _containerInteractive = _attachedContainer.ContainerInteractive;
            _containerItemDisplay = _attachedContainer.ContainerItemDisplay;

            bool automaticContainerSetUp = EditorGUILayout.Toggle(new GUIContent("Automatic container setup", string.Empty), serializedAutomaticContainerSetUp.boolValue);
            HandleAutomaticContainerSetUp(automaticContainerSetUp);

            bool isInteractive = EditorGUILayout.Toggle(new GUIContent("is Interactive", "Set if the container can be interacted with or not. Adds the ContainerInteractive file, which contains default interactions for the container"), _attachedContainer.IsInteractive);
            HandleIsInteractive(isInteractive);

            bool displayAsSlotInUI = EditorGUILayout.Toggle(new GUIContent("displayAsSlotInUI", "Set if the container should display as a slot in UI"), _attachedContainer.DisplayAsSlotInUI);
            HandleDisplayAsSlotInUI(displayAsSlotInUI);

            if (_attachedContainer.IsInteractive)
            {
                bool hasUi = EditorGUILayout.Toggle(new GUIContent("has UI", "Set if the container has an UI"), _attachedContainer.HasUi);
                HandleHasUi(hasUi);

                bool isOpenable = EditorGUILayout.Toggle(new GUIContent("is Openable", "Set if the container has an open/close interaction"), _attachedContainer.IsOpenable);
                HandleIsOpenable(isOpenable);

                bool hasCustomInteraction = EditorGUILayout.Toggle(new GUIContent("has custom interaction", "Set if the container should use the default interaction of ContainerInteractive.cs, or custom ones in another script"), _attachedContainer.HasCustomInteraction);
                HandleCustomInteraction(hasCustomInteraction);
            }

            if (_attachedContainer.IsOpenable)
            {
                bool onlyStoreWhenOpen = EditorGUILayout.Toggle(new GUIContent("Only store when open", "Set if objects can be stored in the container without using the open interaction first"), _attachedContainer.OnlyStoreWhenOpen);
                HandleOnlyStoreWhenOpen(onlyStoreWhenOpen);
            }

            if (_attachedContainer.HasUi)
            {
                // Check if there's an animation with an Open parameter before allowing the openWhenContainerViewed field.
                if (_attachedContainer.gameObject.TryGetComponent<Animator>(out Animator animator))
                {
                    foreach (AnimatorControllerParameter controllerParameter in _attachedContainer.gameObject.GetComponent<Animator>().parameters)
                    {
                        if (controllerParameter.name != "Open")
                        {
                            continue;
                        }

                        bool openWhenContainerViewed = EditorGUILayout.Toggle(new GUIContent("open when container viewed", "Set if the open animation should run when the container UI is viewed"), _attachedContainer.OpenWhenContainerViewed);
                        HandleOpenWhenContainerViewed(openWhenContainerViewed);
                    }
                }
            }

            if (_attachedContainer.HasUi)
            {
                float maxDistance = EditorGUILayout.FloatField(new GUIContent("Max distance", "max distance between the observer and the container before the UI closes on it's own"), _attachedContainer.MaxDistance);
                HandleMaxDistance(maxDistance);
            }

            Vector2Int size = EditorGUILayout.Vector2IntField(new GUIContent("Size", "Defines the size of the container, every item takes a defined place inside a container"), _attachedContainer.Size);
            HandleSize(size);

            SerializedProperty serializedHideItem = serializedObject.FindProperty("_hideItems");
            bool hideItems = EditorGUILayout.Toggle(new GUIContent("Hide items", "Set if items should be attached as children of the container"), serializedHideItem.boolValue);
            HandleHideItems(hideItems);

            if (!hideItems)
            {
                Vector3 attachmentOffset = EditorGUILayout.Vector3Field(new GUIContent("Attachment Offset", "define the position of the items inside the container"), _attachedContainer.AttachmentOffset);
                HandleAttachmentOffset(attachmentOffset);

                bool hasCustomDisplay = EditorGUILayout.Toggle(new GUIContent("Has custom display", "adds the container item display script, defines custom positions for items in the container"), _attachedContainer.HasCustomDisplay);
                HandleHasCustomDisplay(hasCustomDisplay);

                if (hasCustomDisplay)
                {
                    int numberDisplay = EditorGUILayout.IntField(new GUIContent("number of display", "the number of items to display in custom position"), _attachedContainer.NumberDisplay);
                    HandleNumberDisplay(numberDisplay);

                    SerializedProperty sp = serializedObject.FindProperty("_displays");
                    sp.arraySize = numberDisplay;

                    for (int i = 0; i < sp.arraySize; ++i)
                    {
                        SerializedProperty transformProp = sp.GetArrayElementAtIndex(i);
                        EditorGUILayout.PropertyField(transformProp, new GUIContent("Element " + i));
                    }
                }
            }

            bool attachItems = EditorGUILayout.Toggle(new GUIContent("Attach Items", "Set if items should be attached as children of the container game object"), _attachedContainer.AttachItems);
            HandleAttachItems(attachItems);

            Filter startFilter = (Filter)EditorGUILayout.ObjectField(new GUIContent("Filter", "Filter on the container, controls what can go in the container"), _attachedContainer.StartFilter, typeof(Filter), true);
            HandleStartFilter(startFilter);

            ContainerType containerType = (ContainerType)EditorGUILayout.EnumFlagsField(new GUIContent("Container type", "Container type mostly allow to discriminate between diffent containers on a single prefab."), _attachedContainer.Type);
            HandleContainerType(containerType);

            serializedObject.ApplyModifiedProperties();
        }

        protected void OnEnable()
        {
            // Set the container title in editor.
            _titleStyle = new GUIStyle();
            _titleStyle.fontSize = 13;
            _titleStyle.fontStyle = FontStyle.Bold;
            _titleStyle.normal.textColor = Color.white;

            _attachedContainer = (AttachedContainer)target;
            SerializedProperty serializedAutomaticContainerSetUp = serializedObject.FindProperty("_automaticContainerSetUp");

            if (serializedAutomaticContainerSetUp.boolValue)
            {
                AddBase();
                _containerInteractive = _attachedContainer.ContainerInteractive;
                _containerItemDisplay = _attachedContainer.ContainerItemDisplay;
            }
        }

        protected void OnDestroy()
        {
            if (_attachedContainer == null)
            {
                DestroyImmediate(_containerInteractive, true);
            }
        }

        private void HandleAutomaticContainerSetUp(bool automaticContainerSetUp)
        {
            SerializedProperty sp = serializedObject.FindProperty("_automaticContainerSetUp");
            sp.boolValue = automaticContainerSetUp;
            serializedObject.ApplyModifiedProperties();
        }

        private void HandleDisplayAsSlotInUI(bool displayAsSlotInUI)
        {
            SerializedProperty sp = serializedObject.FindProperty("_displayAsSlotInUI");
            sp.boolValue = displayAsSlotInUI;
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

            if (hasCustomDisplay && _attachedContainer.ContainerItemDisplay == null)
            {
                AddCustomDisplay();
            }

            if (!hasCustomDisplay && _attachedContainer.ContainerItemDisplay != null)
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

        private void HandleContainerType(ContainerType containerType)
        {
            SerializedProperty sp = serializedObject.FindProperty("_type");
            sp.enumValueFlag = (int)containerType;
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

            if (isInteractive && _attachedContainer.ContainerInteractive == null)
            {
                AddInteractive();
            }
            else if (!isInteractive && _attachedContainer.ContainerInteractive != null)
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
            GameObject networkedParent = GetParentNetworkIdentity(_attachedContainer.gameObject);
            sp.objectReferenceValue = networkedParent != null ? networkedParent.AddComponent<ContainerInteractive>() : _attachedContainer.gameObject.AddComponent<ContainerInteractive>();
            serializedObject.ApplyModifiedProperties();
            _attachedContainer.ContainerInteractive.AttachedContainer = _attachedContainer;
        }

        private void RemoveInteractive()
        {
            DestroyImmediate(_containerInteractive, true);

            HandleIsInteractive(false);
            HandleIsOpenable(false);
            HandleOnlyStoreWhenOpen(false);
            HandleCustomInteraction(false);
            HandleOpenWhenContainerViewed(false);
        }

        private void AddCustomDisplay()
        {
            SerializedProperty sp = serializedObject.FindProperty("ContainerItemDisplay");
            sp.objectReferenceValue = _attachedContainer.gameObject.AddComponent<ContainerItemDisplay>();
            serializedObject.ApplyModifiedProperties();
            _attachedContainer.ContainerItemDisplay.AttachedContainer = _attachedContainer;
        }

        private void RemoveCustomDisplay()
        {
            DestroyImmediate(_containerItemDisplay, true);
        }

        private GameObject GetParentNetworkIdentity(GameObject g)
        {
            NetworkObject networkIdentity = g.GetComponentInParent<NetworkObject>();

            return networkIdentity ? networkIdentity.gameObject : null;
        }
    }
}




