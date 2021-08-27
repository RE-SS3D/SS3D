// GENERATED AUTOMATICALLY FROM 'Assets/Engine/Input/InputActions.inputactions'

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace SS3D.Engine.Input
{
    public class @InputActions : IInputActionCollection, IDisposable
    {
        public InputActionAsset asset { get; }
        public @InputActions()
        {
            asset = InputActionAsset.FromJson(@"{
    ""name"": ""InputActions"",
    ""maps"": [
        {
            ""name"": ""UI"",
            ""id"": ""fc1b2a61-4efe-48b1-8fa0-0f76e9661897"",
            ""actions"": [
                {
                    ""name"": ""Toggle Chat Down"",
                    ""type"": ""Button"",
                    ""id"": ""4a079100-8c33-4e2c-bef0-076d8fdd4d15"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""Toggle Internal Clothing Down"",
                    ""type"": ""Button"",
                    ""id"": ""44ee3766-a3af-4ff5-bf58-00aa7ba8b024"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""Focus Chat Down"",
                    ""type"": ""Button"",
                    ""id"": ""62667858-3588-4bd6-8df2-f5d534112593"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""Press""
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""9424b36a-2666-4b5d-8f74-a36ca51306cb"",
                    ""path"": ""<Keyboard>/c"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Mouse+Keyboard"",
                    ""action"": ""Toggle Chat Down"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""dfe61014-5888-4c14-8813-945b32097b16"",
                    ""path"": ""<Keyboard>/i"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Mouse+Keyboard"",
                    ""action"": ""Toggle Internal Clothing Down"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""d0b89dfa-d863-46f2-a6fa-7e631addfdd0"",
                    ""path"": ""<Keyboard>/t"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Mouse+Keyboard"",
                    ""action"": ""Focus Chat Down"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        },
        {
            ""name"": ""Pointer"",
            ""id"": ""ee0f925f-9fe4-461f-a24e-ac37fe217016"",
            ""actions"": [
                {
                    ""name"": ""Position"",
                    ""type"": ""Value"",
                    ""id"": ""facdede9-4e7a-4d84-9c04-0ec63b615915"",
                    ""expectedControlType"": ""Vector2"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Secondary Action Down"",
                    ""type"": ""Button"",
                    ""id"": ""a805661f-bd81-4620-977b-7329ceefd3ba"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""Primary Action Down"",
                    ""type"": ""Button"",
                    ""id"": ""16234339-2e74-42b8-85a2-3c59dd67c22a"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""Primary Action Held"",
                    ""type"": ""Button"",
                    ""id"": ""919ded4e-000a-4312-9883-999f834700fe"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""Hold""
                },
                {
                    ""name"": ""Secondary Action Held"",
                    ""type"": ""Button"",
                    ""id"": ""4d58af78-a2bd-4159-810f-b45c884509b7"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""Hold""
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""9d580a37-193d-428f-b525-e608d58ba5bb"",
                    ""path"": ""<Mouse>/position"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Mouse+Keyboard"",
                    ""action"": ""Position"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""444c76e5-8751-46c0-bf77-a80c1bf8bcfb"",
                    ""path"": ""<Mouse>/rightButton"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Mouse+Keyboard"",
                    ""action"": ""Secondary Action Down"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""ddf0c49f-e216-4650-a7c3-ea461b67f1e9"",
                    ""path"": ""<Mouse>/leftButton"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Mouse+Keyboard"",
                    ""action"": ""Primary Action Down"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""92d7f15a-3371-4eb1-bdb5-1e66d816efe6"",
                    ""path"": ""<Mouse>/leftButton"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Mouse+Keyboard"",
                    ""action"": ""Primary Action Held"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""07362855-98e4-4cf5-905d-485cfcd684be"",
                    ""path"": ""<Mouse>/rightButton"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Mouse+Keyboard"",
                    ""action"": ""Secondary Action Held"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        },
        {
            ""name"": ""Player"",
            ""id"": ""3f732837-a54c-43a2-a414-ee7ed49c49de"",
            ""actions"": [
                {
                    ""name"": ""Examine Held"",
                    ""type"": ""Button"",
                    ""id"": ""1423de7d-f8bb-4d74-8d60-e30590f68083"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""Hold""
                },
                {
                    ""name"": ""Combat Mode Down"",
                    ""type"": ""Button"",
                    ""id"": ""273501af-efa6-4b59-b3aa-fb34069d0d86"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""Swap Hand Down"",
                    ""type"": ""Button"",
                    ""id"": ""04903fa0-8d32-4877-ba2a-a2c6908df5d9"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""Drop Item Down"",
                    ""type"": ""Button"",
                    ""id"": ""82764e0f-b059-4975-bfc9-7f76583eb694"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""Activate Down"",
                    ""type"": ""Button"",
                    ""id"": ""ab79c055-66fb-407e-9722-dd38b5631dc2"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""Press""
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""4b6ff1f7-5b4e-4c31-a453-d8478c2ab812"",
                    ""path"": ""<Keyboard>/leftShift"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Mouse+Keyboard"",
                    ""action"": ""Examine Held"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""fac25978-e468-4a6b-9c54-7baedff35138"",
                    ""path"": ""<Keyboard>/f"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Mouse+Keyboard"",
                    ""action"": ""Combat Mode Down"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""caa7255a-7c3e-45e3-a62b-842c00d00efd"",
                    ""path"": ""<Keyboard>/x"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Mouse+Keyboard"",
                    ""action"": ""Swap Hand Down"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""8a583482-4112-4ec0-8705-1e02aed98de4"",
                    ""path"": ""<Keyboard>/q"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Mouse+Keyboard"",
                    ""action"": ""Drop Item Down"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""5453032b-a781-4c3f-bddf-0a7b5b893b3f"",
                    ""path"": ""<Keyboard>/z"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Mouse+Keyboard"",
                    ""action"": ""Activate Down"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        },
        {
            ""name"": ""Misc"",
            ""id"": ""1926618d-2845-4407-b640-13f106a037de"",
            ""actions"": [
                {
                    ""name"": ""Left Alternate Held"",
                    ""type"": ""Button"",
                    ""id"": ""4ee50e36-9872-41d1-8d44-0594d7dd6500"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""Hold""
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""246a3776-61b6-4e4d-8f3c-2e610098e10c"",
                    ""path"": ""<Keyboard>/leftAlt"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Mouse+Keyboard"",
                    ""action"": ""Left Alternate Held"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        }
    ],
    ""controlSchemes"": [
        {
            ""name"": ""Mouse+Keyboard"",
            ""bindingGroup"": ""Mouse+Keyboard"",
            ""devices"": [
                {
                    ""devicePath"": ""<Keyboard>"",
                    ""isOptional"": false,
                    ""isOR"": false
                },
                {
                    ""devicePath"": ""<Mouse>"",
                    ""isOptional"": false,
                    ""isOR"": false
                }
            ]
        }
    ]
}");
            // UI
            m_UI = asset.FindActionMap("UI", throwIfNotFound: true);
            m_UI_ToggleChatDown = m_UI.FindAction("Toggle Chat Down", throwIfNotFound: true);
            m_UI_ToggleInternalClothingDown = m_UI.FindAction("Toggle Internal Clothing Down", throwIfNotFound: true);
            m_UI_FocusChatDown = m_UI.FindAction("Focus Chat Down", throwIfNotFound: true);
            // Pointer
            m_Pointer = asset.FindActionMap("Pointer", throwIfNotFound: true);
            m_Pointer_Position = m_Pointer.FindAction("Position", throwIfNotFound: true);
            m_Pointer_SecondaryActionDown = m_Pointer.FindAction("Secondary Action Down", throwIfNotFound: true);
            m_Pointer_PrimaryActionDown = m_Pointer.FindAction("Primary Action Down", throwIfNotFound: true);
            m_Pointer_PrimaryActionHeld = m_Pointer.FindAction("Primary Action Held", throwIfNotFound: true);
            m_Pointer_SecondaryActionHeld = m_Pointer.FindAction("Secondary Action Held", throwIfNotFound: true);
            // Player
            m_Player = asset.FindActionMap("Player", throwIfNotFound: true);
            m_Player_ExamineHeld = m_Player.FindAction("Examine Held", throwIfNotFound: true);
            m_Player_CombatModeDown = m_Player.FindAction("Combat Mode Down", throwIfNotFound: true);
            m_Player_SwapHandDown = m_Player.FindAction("Swap Hand Down", throwIfNotFound: true);
            m_Player_DropItemDown = m_Player.FindAction("Drop Item Down", throwIfNotFound: true);
            m_Player_ActivateDown = m_Player.FindAction("Activate Down", throwIfNotFound: true);
            // Misc
            m_Misc = asset.FindActionMap("Misc", throwIfNotFound: true);
            m_Misc_LeftAlternateHeld = m_Misc.FindAction("Left Alternate Held", throwIfNotFound: true);
        }

        public void Dispose()
        {
            UnityEngine.Object.Destroy(asset);
        }

        public InputBinding? bindingMask
        {
            get => asset.bindingMask;
            set => asset.bindingMask = value;
        }

        public ReadOnlyArray<InputDevice>? devices
        {
            get => asset.devices;
            set => asset.devices = value;
        }

        public ReadOnlyArray<InputControlScheme> controlSchemes => asset.controlSchemes;

        public bool Contains(InputAction action)
        {
            return asset.Contains(action);
        }

        public IEnumerator<InputAction> GetEnumerator()
        {
            return asset.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Enable()
        {
            asset.Enable();
        }

        public void Disable()
        {
            asset.Disable();
        }

        // UI
        private readonly InputActionMap m_UI;
        private IUIActions m_UIActionsCallbackInterface;
        private readonly InputAction m_UI_ToggleChatDown;
        private readonly InputAction m_UI_ToggleInternalClothingDown;
        private readonly InputAction m_UI_FocusChatDown;
        public struct UIActions
        {
            private @InputActions m_Wrapper;
            public UIActions(@InputActions wrapper) { m_Wrapper = wrapper; }
            public InputAction @ToggleChatDown => m_Wrapper.m_UI_ToggleChatDown;
            public InputAction @ToggleInternalClothingDown => m_Wrapper.m_UI_ToggleInternalClothingDown;
            public InputAction @FocusChatDown => m_Wrapper.m_UI_FocusChatDown;
            public InputActionMap Get() { return m_Wrapper.m_UI; }
            public void Enable() { Get().Enable(); }
            public void Disable() { Get().Disable(); }
            public bool enabled => Get().enabled;
            public static implicit operator InputActionMap(UIActions set) { return set.Get(); }
            public void SetCallbacks(IUIActions instance)
            {
                if (m_Wrapper.m_UIActionsCallbackInterface != null)
                {
                    @ToggleChatDown.started -= m_Wrapper.m_UIActionsCallbackInterface.OnToggleChatDown;
                    @ToggleChatDown.performed -= m_Wrapper.m_UIActionsCallbackInterface.OnToggleChatDown;
                    @ToggleChatDown.canceled -= m_Wrapper.m_UIActionsCallbackInterface.OnToggleChatDown;
                    @ToggleInternalClothingDown.started -= m_Wrapper.m_UIActionsCallbackInterface.OnToggleInternalClothingDown;
                    @ToggleInternalClothingDown.performed -= m_Wrapper.m_UIActionsCallbackInterface.OnToggleInternalClothingDown;
                    @ToggleInternalClothingDown.canceled -= m_Wrapper.m_UIActionsCallbackInterface.OnToggleInternalClothingDown;
                    @FocusChatDown.started -= m_Wrapper.m_UIActionsCallbackInterface.OnFocusChatDown;
                    @FocusChatDown.performed -= m_Wrapper.m_UIActionsCallbackInterface.OnFocusChatDown;
                    @FocusChatDown.canceled -= m_Wrapper.m_UIActionsCallbackInterface.OnFocusChatDown;
                }
                m_Wrapper.m_UIActionsCallbackInterface = instance;
                if (instance != null)
                {
                    @ToggleChatDown.started += instance.OnToggleChatDown;
                    @ToggleChatDown.performed += instance.OnToggleChatDown;
                    @ToggleChatDown.canceled += instance.OnToggleChatDown;
                    @ToggleInternalClothingDown.started += instance.OnToggleInternalClothingDown;
                    @ToggleInternalClothingDown.performed += instance.OnToggleInternalClothingDown;
                    @ToggleInternalClothingDown.canceled += instance.OnToggleInternalClothingDown;
                    @FocusChatDown.started += instance.OnFocusChatDown;
                    @FocusChatDown.performed += instance.OnFocusChatDown;
                    @FocusChatDown.canceled += instance.OnFocusChatDown;
                }
            }
        }
        public UIActions @UI => new UIActions(this);

        // Pointer
        private readonly InputActionMap m_Pointer;
        private IPointerActions m_PointerActionsCallbackInterface;
        private readonly InputAction m_Pointer_Position;
        private readonly InputAction m_Pointer_SecondaryActionDown;
        private readonly InputAction m_Pointer_PrimaryActionDown;
        private readonly InputAction m_Pointer_PrimaryActionHeld;
        private readonly InputAction m_Pointer_SecondaryActionHeld;
        public struct PointerActions
        {
            private @InputActions m_Wrapper;
            public PointerActions(@InputActions wrapper) { m_Wrapper = wrapper; }
            public InputAction @Position => m_Wrapper.m_Pointer_Position;
            public InputAction @SecondaryActionDown => m_Wrapper.m_Pointer_SecondaryActionDown;
            public InputAction @PrimaryActionDown => m_Wrapper.m_Pointer_PrimaryActionDown;
            public InputAction @PrimaryActionHeld => m_Wrapper.m_Pointer_PrimaryActionHeld;
            public InputAction @SecondaryActionHeld => m_Wrapper.m_Pointer_SecondaryActionHeld;
            public InputActionMap Get() { return m_Wrapper.m_Pointer; }
            public void Enable() { Get().Enable(); }
            public void Disable() { Get().Disable(); }
            public bool enabled => Get().enabled;
            public static implicit operator InputActionMap(PointerActions set) { return set.Get(); }
            public void SetCallbacks(IPointerActions instance)
            {
                if (m_Wrapper.m_PointerActionsCallbackInterface != null)
                {
                    @Position.started -= m_Wrapper.m_PointerActionsCallbackInterface.OnPosition;
                    @Position.performed -= m_Wrapper.m_PointerActionsCallbackInterface.OnPosition;
                    @Position.canceled -= m_Wrapper.m_PointerActionsCallbackInterface.OnPosition;
                    @SecondaryActionDown.started -= m_Wrapper.m_PointerActionsCallbackInterface.OnSecondaryActionDown;
                    @SecondaryActionDown.performed -= m_Wrapper.m_PointerActionsCallbackInterface.OnSecondaryActionDown;
                    @SecondaryActionDown.canceled -= m_Wrapper.m_PointerActionsCallbackInterface.OnSecondaryActionDown;
                    @PrimaryActionDown.started -= m_Wrapper.m_PointerActionsCallbackInterface.OnPrimaryActionDown;
                    @PrimaryActionDown.performed -= m_Wrapper.m_PointerActionsCallbackInterface.OnPrimaryActionDown;
                    @PrimaryActionDown.canceled -= m_Wrapper.m_PointerActionsCallbackInterface.OnPrimaryActionDown;
                    @PrimaryActionHeld.started -= m_Wrapper.m_PointerActionsCallbackInterface.OnPrimaryActionHeld;
                    @PrimaryActionHeld.performed -= m_Wrapper.m_PointerActionsCallbackInterface.OnPrimaryActionHeld;
                    @PrimaryActionHeld.canceled -= m_Wrapper.m_PointerActionsCallbackInterface.OnPrimaryActionHeld;
                    @SecondaryActionHeld.started -= m_Wrapper.m_PointerActionsCallbackInterface.OnSecondaryActionHeld;
                    @SecondaryActionHeld.performed -= m_Wrapper.m_PointerActionsCallbackInterface.OnSecondaryActionHeld;
                    @SecondaryActionHeld.canceled -= m_Wrapper.m_PointerActionsCallbackInterface.OnSecondaryActionHeld;
                }
                m_Wrapper.m_PointerActionsCallbackInterface = instance;
                if (instance != null)
                {
                    @Position.started += instance.OnPosition;
                    @Position.performed += instance.OnPosition;
                    @Position.canceled += instance.OnPosition;
                    @SecondaryActionDown.started += instance.OnSecondaryActionDown;
                    @SecondaryActionDown.performed += instance.OnSecondaryActionDown;
                    @SecondaryActionDown.canceled += instance.OnSecondaryActionDown;
                    @PrimaryActionDown.started += instance.OnPrimaryActionDown;
                    @PrimaryActionDown.performed += instance.OnPrimaryActionDown;
                    @PrimaryActionDown.canceled += instance.OnPrimaryActionDown;
                    @PrimaryActionHeld.started += instance.OnPrimaryActionHeld;
                    @PrimaryActionHeld.performed += instance.OnPrimaryActionHeld;
                    @PrimaryActionHeld.canceled += instance.OnPrimaryActionHeld;
                    @SecondaryActionHeld.started += instance.OnSecondaryActionHeld;
                    @SecondaryActionHeld.performed += instance.OnSecondaryActionHeld;
                    @SecondaryActionHeld.canceled += instance.OnSecondaryActionHeld;
                }
            }
        }
        public PointerActions @Pointer => new PointerActions(this);

        // Player
        private readonly InputActionMap m_Player;
        private IPlayerActions m_PlayerActionsCallbackInterface;
        private readonly InputAction m_Player_ExamineHeld;
        private readonly InputAction m_Player_CombatModeDown;
        private readonly InputAction m_Player_SwapHandDown;
        private readonly InputAction m_Player_DropItemDown;
        private readonly InputAction m_Player_ActivateDown;
        public struct PlayerActions
        {
            private @InputActions m_Wrapper;
            public PlayerActions(@InputActions wrapper) { m_Wrapper = wrapper; }
            public InputAction @ExamineHeld => m_Wrapper.m_Player_ExamineHeld;
            public InputAction @CombatModeDown => m_Wrapper.m_Player_CombatModeDown;
            public InputAction @SwapHandDown => m_Wrapper.m_Player_SwapHandDown;
            public InputAction @DropItemDown => m_Wrapper.m_Player_DropItemDown;
            public InputAction @ActivateDown => m_Wrapper.m_Player_ActivateDown;
            public InputActionMap Get() { return m_Wrapper.m_Player; }
            public void Enable() { Get().Enable(); }
            public void Disable() { Get().Disable(); }
            public bool enabled => Get().enabled;
            public static implicit operator InputActionMap(PlayerActions set) { return set.Get(); }
            public void SetCallbacks(IPlayerActions instance)
            {
                if (m_Wrapper.m_PlayerActionsCallbackInterface != null)
                {
                    @ExamineHeld.started -= m_Wrapper.m_PlayerActionsCallbackInterface.OnExamineHeld;
                    @ExamineHeld.performed -= m_Wrapper.m_PlayerActionsCallbackInterface.OnExamineHeld;
                    @ExamineHeld.canceled -= m_Wrapper.m_PlayerActionsCallbackInterface.OnExamineHeld;
                    @CombatModeDown.started -= m_Wrapper.m_PlayerActionsCallbackInterface.OnCombatModeDown;
                    @CombatModeDown.performed -= m_Wrapper.m_PlayerActionsCallbackInterface.OnCombatModeDown;
                    @CombatModeDown.canceled -= m_Wrapper.m_PlayerActionsCallbackInterface.OnCombatModeDown;
                    @SwapHandDown.started -= m_Wrapper.m_PlayerActionsCallbackInterface.OnSwapHandDown;
                    @SwapHandDown.performed -= m_Wrapper.m_PlayerActionsCallbackInterface.OnSwapHandDown;
                    @SwapHandDown.canceled -= m_Wrapper.m_PlayerActionsCallbackInterface.OnSwapHandDown;
                    @DropItemDown.started -= m_Wrapper.m_PlayerActionsCallbackInterface.OnDropItemDown;
                    @DropItemDown.performed -= m_Wrapper.m_PlayerActionsCallbackInterface.OnDropItemDown;
                    @DropItemDown.canceled -= m_Wrapper.m_PlayerActionsCallbackInterface.OnDropItemDown;
                    @ActivateDown.started -= m_Wrapper.m_PlayerActionsCallbackInterface.OnActivateDown;
                    @ActivateDown.performed -= m_Wrapper.m_PlayerActionsCallbackInterface.OnActivateDown;
                    @ActivateDown.canceled -= m_Wrapper.m_PlayerActionsCallbackInterface.OnActivateDown;
                }
                m_Wrapper.m_PlayerActionsCallbackInterface = instance;
                if (instance != null)
                {
                    @ExamineHeld.started += instance.OnExamineHeld;
                    @ExamineHeld.performed += instance.OnExamineHeld;
                    @ExamineHeld.canceled += instance.OnExamineHeld;
                    @CombatModeDown.started += instance.OnCombatModeDown;
                    @CombatModeDown.performed += instance.OnCombatModeDown;
                    @CombatModeDown.canceled += instance.OnCombatModeDown;
                    @SwapHandDown.started += instance.OnSwapHandDown;
                    @SwapHandDown.performed += instance.OnSwapHandDown;
                    @SwapHandDown.canceled += instance.OnSwapHandDown;
                    @DropItemDown.started += instance.OnDropItemDown;
                    @DropItemDown.performed += instance.OnDropItemDown;
                    @DropItemDown.canceled += instance.OnDropItemDown;
                    @ActivateDown.started += instance.OnActivateDown;
                    @ActivateDown.performed += instance.OnActivateDown;
                    @ActivateDown.canceled += instance.OnActivateDown;
                }
            }
        }
        public PlayerActions @Player => new PlayerActions(this);

        // Misc
        private readonly InputActionMap m_Misc;
        private IMiscActions m_MiscActionsCallbackInterface;
        private readonly InputAction m_Misc_LeftAlternateHeld;
        public struct MiscActions
        {
            private @InputActions m_Wrapper;
            public MiscActions(@InputActions wrapper) { m_Wrapper = wrapper; }
            public InputAction @LeftAlternateHeld => m_Wrapper.m_Misc_LeftAlternateHeld;
            public InputActionMap Get() { return m_Wrapper.m_Misc; }
            public void Enable() { Get().Enable(); }
            public void Disable() { Get().Disable(); }
            public bool enabled => Get().enabled;
            public static implicit operator InputActionMap(MiscActions set) { return set.Get(); }
            public void SetCallbacks(IMiscActions instance)
            {
                if (m_Wrapper.m_MiscActionsCallbackInterface != null)
                {
                    @LeftAlternateHeld.started -= m_Wrapper.m_MiscActionsCallbackInterface.OnLeftAlternateHeld;
                    @LeftAlternateHeld.performed -= m_Wrapper.m_MiscActionsCallbackInterface.OnLeftAlternateHeld;
                    @LeftAlternateHeld.canceled -= m_Wrapper.m_MiscActionsCallbackInterface.OnLeftAlternateHeld;
                }
                m_Wrapper.m_MiscActionsCallbackInterface = instance;
                if (instance != null)
                {
                    @LeftAlternateHeld.started += instance.OnLeftAlternateHeld;
                    @LeftAlternateHeld.performed += instance.OnLeftAlternateHeld;
                    @LeftAlternateHeld.canceled += instance.OnLeftAlternateHeld;
                }
            }
        }
        public MiscActions @Misc => new MiscActions(this);
        private int m_MouseKeyboardSchemeIndex = -1;
        public InputControlScheme MouseKeyboardScheme
        {
            get
            {
                if (m_MouseKeyboardSchemeIndex == -1) m_MouseKeyboardSchemeIndex = asset.FindControlSchemeIndex("Mouse+Keyboard");
                return asset.controlSchemes[m_MouseKeyboardSchemeIndex];
            }
        }
        public interface IUIActions
        {
            void OnToggleChatDown(InputAction.CallbackContext context);
            void OnToggleInternalClothingDown(InputAction.CallbackContext context);
            void OnFocusChatDown(InputAction.CallbackContext context);
        }
        public interface IPointerActions
        {
            void OnPosition(InputAction.CallbackContext context);
            void OnSecondaryActionDown(InputAction.CallbackContext context);
            void OnPrimaryActionDown(InputAction.CallbackContext context);
            void OnPrimaryActionHeld(InputAction.CallbackContext context);
            void OnSecondaryActionHeld(InputAction.CallbackContext context);
        }
        public interface IPlayerActions
        {
            void OnExamineHeld(InputAction.CallbackContext context);
            void OnCombatModeDown(InputAction.CallbackContext context);
            void OnSwapHandDown(InputAction.CallbackContext context);
            void OnDropItemDown(InputAction.CallbackContext context);
            void OnActivateDown(InputAction.CallbackContext context);
        }
        public interface IMiscActions
        {
            void OnLeftAlternateHeld(InputAction.CallbackContext context);
        }
    }
}
