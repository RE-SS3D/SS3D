// GENERATED AUTOMATICALLY FROM 'Assets/Engine/Input/Inputs.inputactions'

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace SS3D.Engine.Input
{
    public class @Inputs : IInputActionCollection, IDisposable
    {
        public InputActionAsset asset { get; }
        public @Inputs()
        {
            asset = InputActionAsset.FromJson(@"{
    ""name"": ""Inputs"",
    ""maps"": [
        {
            ""name"": ""ui"",
            ""id"": ""fc1b2a61-4efe-48b1-8fa0-0f76e9661897"",
            ""actions"": [
                {
                    ""name"": ""toggleChatDown"",
                    ""type"": ""Button"",
                    ""id"": ""4a079100-8c33-4e2c-bef0-076d8fdd4d15"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""toggleInternalClothingDown"",
                    ""type"": ""Button"",
                    ""id"": ""44ee3766-a3af-4ff5-bf58-00aa7ba8b024"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""focusChatDown"",
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
                    ""action"": ""toggleChatDown"",
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
                    ""action"": ""toggleInternalClothingDown"",
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
                    ""action"": ""focusChatDown"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        },
        {
            ""name"": ""pointer"",
            ""id"": ""ee0f925f-9fe4-461f-a24e-ac37fe217016"",
            ""actions"": [
                {
                    ""name"": ""position"",
                    ""type"": ""Value"",
                    ""id"": ""facdede9-4e7a-4d84-9c04-0ec63b615915"",
                    ""expectedControlType"": ""Vector2"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""secondaryActionDown"",
                    ""type"": ""Button"",
                    ""id"": ""a805661f-bd81-4620-977b-7329ceefd3ba"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""primaryActionDown"",
                    ""type"": ""Button"",
                    ""id"": ""16234339-2e74-42b8-85a2-3c59dd67c22a"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""primaryActionHeld"",
                    ""type"": ""Button"",
                    ""id"": ""919ded4e-000a-4312-9883-999f834700fe"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""Hold""
                },
                {
                    ""name"": ""secondaryActionHeld"",
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
                    ""action"": ""position"",
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
                    ""action"": ""secondaryActionDown"",
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
                    ""action"": ""primaryActionDown"",
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
                    ""action"": ""primaryActionHeld"",
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
                    ""action"": ""secondaryActionHeld"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        },
        {
            ""name"": ""player"",
            ""id"": ""3f732837-a54c-43a2-a414-ee7ed49c49de"",
            ""actions"": [
                {
                    ""name"": ""examineHold"",
                    ""type"": ""Button"",
                    ""id"": ""1423de7d-f8bb-4d74-8d60-e30590f68083"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""Hold""
                },
                {
                    ""name"": ""combatModeDown"",
                    ""type"": ""Button"",
                    ""id"": ""273501af-efa6-4b59-b3aa-fb34069d0d86"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""swapHandDown"",
                    ""type"": ""Button"",
                    ""id"": ""04903fa0-8d32-4877-ba2a-a2c6908df5d9"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""dropItemDown"",
                    ""type"": ""Button"",
                    ""id"": ""82764e0f-b059-4975-bfc9-7f76583eb694"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""activateDown"",
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
                    ""action"": ""examineHold"",
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
                    ""action"": ""combatModeDown"",
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
                    ""action"": ""swapHandDown"",
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
                    ""action"": ""dropItemDown"",
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
                    ""action"": ""activateDown"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        },
        {
            ""name"": ""misc"",
            ""id"": ""1926618d-2845-4407-b640-13f106a037de"",
            ""actions"": [
                {
                    ""name"": ""leftAlternateHeld"",
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
                    ""action"": ""leftAlternateHeld"",
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
            // ui
            m_ui = asset.FindActionMap("ui", throwIfNotFound: true);
            m_ui_toggleChatDown = m_ui.FindAction("toggleChatDown", throwIfNotFound: true);
            m_ui_toggleInternalClothingDown = m_ui.FindAction("toggleInternalClothingDown", throwIfNotFound: true);
            m_ui_focusChatDown = m_ui.FindAction("focusChatDown", throwIfNotFound: true);
            // pointer
            m_pointer = asset.FindActionMap("pointer", throwIfNotFound: true);
            m_pointer_position = m_pointer.FindAction("position", throwIfNotFound: true);
            m_pointer_secondaryActionDown = m_pointer.FindAction("secondaryActionDown", throwIfNotFound: true);
            m_pointer_primaryActionDown = m_pointer.FindAction("primaryActionDown", throwIfNotFound: true);
            m_pointer_primaryActionHeld = m_pointer.FindAction("primaryActionHeld", throwIfNotFound: true);
            m_pointer_secondaryActionHeld = m_pointer.FindAction("secondaryActionHeld", throwIfNotFound: true);
            // player
            m_player = asset.FindActionMap("player", throwIfNotFound: true);
            m_player_examineHold = m_player.FindAction("examineHold", throwIfNotFound: true);
            m_player_combatModeDown = m_player.FindAction("combatModeDown", throwIfNotFound: true);
            m_player_swapHandDown = m_player.FindAction("swapHandDown", throwIfNotFound: true);
            m_player_dropItemDown = m_player.FindAction("dropItemDown", throwIfNotFound: true);
            m_player_activateDown = m_player.FindAction("activateDown", throwIfNotFound: true);
            // misc
            m_misc = asset.FindActionMap("misc", throwIfNotFound: true);
            m_misc_leftAlternateHeld = m_misc.FindAction("leftAlternateHeld", throwIfNotFound: true);
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

        // ui
        private readonly InputActionMap m_ui;
        private IUiActions m_UiActionsCallbackInterface;
        private readonly InputAction m_ui_toggleChatDown;
        private readonly InputAction m_ui_toggleInternalClothingDown;
        private readonly InputAction m_ui_focusChatDown;
        public struct UiActions
        {
            private @Inputs m_Wrapper;
            public UiActions(@Inputs wrapper) { m_Wrapper = wrapper; }
            public InputAction @toggleChatDown => m_Wrapper.m_ui_toggleChatDown;
            public InputAction @toggleInternalClothingDown => m_Wrapper.m_ui_toggleInternalClothingDown;
            public InputAction @focusChatDown => m_Wrapper.m_ui_focusChatDown;
            public InputActionMap Get() { return m_Wrapper.m_ui; }
            public void Enable() { Get().Enable(); }
            public void Disable() { Get().Disable(); }
            public bool enabled => Get().enabled;
            public static implicit operator InputActionMap(UiActions set) { return set.Get(); }
            public void SetCallbacks(IUiActions instance)
            {
                if (m_Wrapper.m_UiActionsCallbackInterface != null)
                {
                    @toggleChatDown.started -= m_Wrapper.m_UiActionsCallbackInterface.OnToggleChatDown;
                    @toggleChatDown.performed -= m_Wrapper.m_UiActionsCallbackInterface.OnToggleChatDown;
                    @toggleChatDown.canceled -= m_Wrapper.m_UiActionsCallbackInterface.OnToggleChatDown;
                    @toggleInternalClothingDown.started -= m_Wrapper.m_UiActionsCallbackInterface.OnToggleInternalClothingDown;
                    @toggleInternalClothingDown.performed -= m_Wrapper.m_UiActionsCallbackInterface.OnToggleInternalClothingDown;
                    @toggleInternalClothingDown.canceled -= m_Wrapper.m_UiActionsCallbackInterface.OnToggleInternalClothingDown;
                    @focusChatDown.started -= m_Wrapper.m_UiActionsCallbackInterface.OnFocusChatDown;
                    @focusChatDown.performed -= m_Wrapper.m_UiActionsCallbackInterface.OnFocusChatDown;
                    @focusChatDown.canceled -= m_Wrapper.m_UiActionsCallbackInterface.OnFocusChatDown;
                }
                m_Wrapper.m_UiActionsCallbackInterface = instance;
                if (instance != null)
                {
                    @toggleChatDown.started += instance.OnToggleChatDown;
                    @toggleChatDown.performed += instance.OnToggleChatDown;
                    @toggleChatDown.canceled += instance.OnToggleChatDown;
                    @toggleInternalClothingDown.started += instance.OnToggleInternalClothingDown;
                    @toggleInternalClothingDown.performed += instance.OnToggleInternalClothingDown;
                    @toggleInternalClothingDown.canceled += instance.OnToggleInternalClothingDown;
                    @focusChatDown.started += instance.OnFocusChatDown;
                    @focusChatDown.performed += instance.OnFocusChatDown;
                    @focusChatDown.canceled += instance.OnFocusChatDown;
                }
            }
        }
        public UiActions @ui => new UiActions(this);

        // pointer
        private readonly InputActionMap m_pointer;
        private IPointerActions m_PointerActionsCallbackInterface;
        private readonly InputAction m_pointer_position;
        private readonly InputAction m_pointer_secondaryActionDown;
        private readonly InputAction m_pointer_primaryActionDown;
        private readonly InputAction m_pointer_primaryActionHeld;
        private readonly InputAction m_pointer_secondaryActionHeld;
        public struct PointerActions
        {
            private @Inputs m_Wrapper;
            public PointerActions(@Inputs wrapper) { m_Wrapper = wrapper; }
            public InputAction @position => m_Wrapper.m_pointer_position;
            public InputAction @secondaryActionDown => m_Wrapper.m_pointer_secondaryActionDown;
            public InputAction @primaryActionDown => m_Wrapper.m_pointer_primaryActionDown;
            public InputAction @primaryActionHeld => m_Wrapper.m_pointer_primaryActionHeld;
            public InputAction @secondaryActionHeld => m_Wrapper.m_pointer_secondaryActionHeld;
            public InputActionMap Get() { return m_Wrapper.m_pointer; }
            public void Enable() { Get().Enable(); }
            public void Disable() { Get().Disable(); }
            public bool enabled => Get().enabled;
            public static implicit operator InputActionMap(PointerActions set) { return set.Get(); }
            public void SetCallbacks(IPointerActions instance)
            {
                if (m_Wrapper.m_PointerActionsCallbackInterface != null)
                {
                    @position.started -= m_Wrapper.m_PointerActionsCallbackInterface.OnPosition;
                    @position.performed -= m_Wrapper.m_PointerActionsCallbackInterface.OnPosition;
                    @position.canceled -= m_Wrapper.m_PointerActionsCallbackInterface.OnPosition;
                    @secondaryActionDown.started -= m_Wrapper.m_PointerActionsCallbackInterface.OnSecondaryActionDown;
                    @secondaryActionDown.performed -= m_Wrapper.m_PointerActionsCallbackInterface.OnSecondaryActionDown;
                    @secondaryActionDown.canceled -= m_Wrapper.m_PointerActionsCallbackInterface.OnSecondaryActionDown;
                    @primaryActionDown.started -= m_Wrapper.m_PointerActionsCallbackInterface.OnPrimaryActionDown;
                    @primaryActionDown.performed -= m_Wrapper.m_PointerActionsCallbackInterface.OnPrimaryActionDown;
                    @primaryActionDown.canceled -= m_Wrapper.m_PointerActionsCallbackInterface.OnPrimaryActionDown;
                    @primaryActionHeld.started -= m_Wrapper.m_PointerActionsCallbackInterface.OnPrimaryActionHeld;
                    @primaryActionHeld.performed -= m_Wrapper.m_PointerActionsCallbackInterface.OnPrimaryActionHeld;
                    @primaryActionHeld.canceled -= m_Wrapper.m_PointerActionsCallbackInterface.OnPrimaryActionHeld;
                    @secondaryActionHeld.started -= m_Wrapper.m_PointerActionsCallbackInterface.OnSecondaryActionHeld;
                    @secondaryActionHeld.performed -= m_Wrapper.m_PointerActionsCallbackInterface.OnSecondaryActionHeld;
                    @secondaryActionHeld.canceled -= m_Wrapper.m_PointerActionsCallbackInterface.OnSecondaryActionHeld;
                }
                m_Wrapper.m_PointerActionsCallbackInterface = instance;
                if (instance != null)
                {
                    @position.started += instance.OnPosition;
                    @position.performed += instance.OnPosition;
                    @position.canceled += instance.OnPosition;
                    @secondaryActionDown.started += instance.OnSecondaryActionDown;
                    @secondaryActionDown.performed += instance.OnSecondaryActionDown;
                    @secondaryActionDown.canceled += instance.OnSecondaryActionDown;
                    @primaryActionDown.started += instance.OnPrimaryActionDown;
                    @primaryActionDown.performed += instance.OnPrimaryActionDown;
                    @primaryActionDown.canceled += instance.OnPrimaryActionDown;
                    @primaryActionHeld.started += instance.OnPrimaryActionHeld;
                    @primaryActionHeld.performed += instance.OnPrimaryActionHeld;
                    @primaryActionHeld.canceled += instance.OnPrimaryActionHeld;
                    @secondaryActionHeld.started += instance.OnSecondaryActionHeld;
                    @secondaryActionHeld.performed += instance.OnSecondaryActionHeld;
                    @secondaryActionHeld.canceled += instance.OnSecondaryActionHeld;
                }
            }
        }
        public PointerActions @pointer => new PointerActions(this);

        // player
        private readonly InputActionMap m_player;
        private IPlayerActions m_PlayerActionsCallbackInterface;
        private readonly InputAction m_player_examineHold;
        private readonly InputAction m_player_combatModeDown;
        private readonly InputAction m_player_swapHandDown;
        private readonly InputAction m_player_dropItemDown;
        private readonly InputAction m_player_activateDown;
        public struct PlayerActions
        {
            private @Inputs m_Wrapper;
            public PlayerActions(@Inputs wrapper) { m_Wrapper = wrapper; }
            public InputAction @examineHold => m_Wrapper.m_player_examineHold;
            public InputAction @combatModeDown => m_Wrapper.m_player_combatModeDown;
            public InputAction @swapHandDown => m_Wrapper.m_player_swapHandDown;
            public InputAction @dropItemDown => m_Wrapper.m_player_dropItemDown;
            public InputAction @activateDown => m_Wrapper.m_player_activateDown;
            public InputActionMap Get() { return m_Wrapper.m_player; }
            public void Enable() { Get().Enable(); }
            public void Disable() { Get().Disable(); }
            public bool enabled => Get().enabled;
            public static implicit operator InputActionMap(PlayerActions set) { return set.Get(); }
            public void SetCallbacks(IPlayerActions instance)
            {
                if (m_Wrapper.m_PlayerActionsCallbackInterface != null)
                {
                    @examineHold.started -= m_Wrapper.m_PlayerActionsCallbackInterface.OnExamineHold;
                    @examineHold.performed -= m_Wrapper.m_PlayerActionsCallbackInterface.OnExamineHold;
                    @examineHold.canceled -= m_Wrapper.m_PlayerActionsCallbackInterface.OnExamineHold;
                    @combatModeDown.started -= m_Wrapper.m_PlayerActionsCallbackInterface.OnCombatModeDown;
                    @combatModeDown.performed -= m_Wrapper.m_PlayerActionsCallbackInterface.OnCombatModeDown;
                    @combatModeDown.canceled -= m_Wrapper.m_PlayerActionsCallbackInterface.OnCombatModeDown;
                    @swapHandDown.started -= m_Wrapper.m_PlayerActionsCallbackInterface.OnSwapHandDown;
                    @swapHandDown.performed -= m_Wrapper.m_PlayerActionsCallbackInterface.OnSwapHandDown;
                    @swapHandDown.canceled -= m_Wrapper.m_PlayerActionsCallbackInterface.OnSwapHandDown;
                    @dropItemDown.started -= m_Wrapper.m_PlayerActionsCallbackInterface.OnDropItemDown;
                    @dropItemDown.performed -= m_Wrapper.m_PlayerActionsCallbackInterface.OnDropItemDown;
                    @dropItemDown.canceled -= m_Wrapper.m_PlayerActionsCallbackInterface.OnDropItemDown;
                    @activateDown.started -= m_Wrapper.m_PlayerActionsCallbackInterface.OnActivateDown;
                    @activateDown.performed -= m_Wrapper.m_PlayerActionsCallbackInterface.OnActivateDown;
                    @activateDown.canceled -= m_Wrapper.m_PlayerActionsCallbackInterface.OnActivateDown;
                }
                m_Wrapper.m_PlayerActionsCallbackInterface = instance;
                if (instance != null)
                {
                    @examineHold.started += instance.OnExamineHold;
                    @examineHold.performed += instance.OnExamineHold;
                    @examineHold.canceled += instance.OnExamineHold;
                    @combatModeDown.started += instance.OnCombatModeDown;
                    @combatModeDown.performed += instance.OnCombatModeDown;
                    @combatModeDown.canceled += instance.OnCombatModeDown;
                    @swapHandDown.started += instance.OnSwapHandDown;
                    @swapHandDown.performed += instance.OnSwapHandDown;
                    @swapHandDown.canceled += instance.OnSwapHandDown;
                    @dropItemDown.started += instance.OnDropItemDown;
                    @dropItemDown.performed += instance.OnDropItemDown;
                    @dropItemDown.canceled += instance.OnDropItemDown;
                    @activateDown.started += instance.OnActivateDown;
                    @activateDown.performed += instance.OnActivateDown;
                    @activateDown.canceled += instance.OnActivateDown;
                }
            }
        }
        public PlayerActions @player => new PlayerActions(this);

        // misc
        private readonly InputActionMap m_misc;
        private IMiscActions m_MiscActionsCallbackInterface;
        private readonly InputAction m_misc_leftAlternateHeld;
        public struct MiscActions
        {
            private @Inputs m_Wrapper;
            public MiscActions(@Inputs wrapper) { m_Wrapper = wrapper; }
            public InputAction @leftAlternateHeld => m_Wrapper.m_misc_leftAlternateHeld;
            public InputActionMap Get() { return m_Wrapper.m_misc; }
            public void Enable() { Get().Enable(); }
            public void Disable() { Get().Disable(); }
            public bool enabled => Get().enabled;
            public static implicit operator InputActionMap(MiscActions set) { return set.Get(); }
            public void SetCallbacks(IMiscActions instance)
            {
                if (m_Wrapper.m_MiscActionsCallbackInterface != null)
                {
                    @leftAlternateHeld.started -= m_Wrapper.m_MiscActionsCallbackInterface.OnLeftAlternateHeld;
                    @leftAlternateHeld.performed -= m_Wrapper.m_MiscActionsCallbackInterface.OnLeftAlternateHeld;
                    @leftAlternateHeld.canceled -= m_Wrapper.m_MiscActionsCallbackInterface.OnLeftAlternateHeld;
                }
                m_Wrapper.m_MiscActionsCallbackInterface = instance;
                if (instance != null)
                {
                    @leftAlternateHeld.started += instance.OnLeftAlternateHeld;
                    @leftAlternateHeld.performed += instance.OnLeftAlternateHeld;
                    @leftAlternateHeld.canceled += instance.OnLeftAlternateHeld;
                }
            }
        }
        public MiscActions @misc => new MiscActions(this);
        private int m_MouseKeyboardSchemeIndex = -1;
        public InputControlScheme MouseKeyboardScheme
        {
            get
            {
                if (m_MouseKeyboardSchemeIndex == -1) m_MouseKeyboardSchemeIndex = asset.FindControlSchemeIndex("Mouse+Keyboard");
                return asset.controlSchemes[m_MouseKeyboardSchemeIndex];
            }
        }
        public interface IUiActions
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
            void OnExamineHold(InputAction.CallbackContext context);
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
