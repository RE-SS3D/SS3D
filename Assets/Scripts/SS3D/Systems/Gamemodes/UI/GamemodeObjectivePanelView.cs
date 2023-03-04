using System;
using System.Collections.Generic;
using Coimbra;
using Coimbra.Services.Events;
using Coimbra.Services.PlayerLoopEvents;
using SS3D.Systems.Gamemodes;
using SS3D.Systems.GameModes.Events;
using SS3D.Core;
using SS3D.Systems.Rounds;
using SS3D.Systems.Rounds.Events;
using SS3D.Utils;
using UnityEngine;
using UnityEngine.InputSystem;
using Actor = SS3D.Core.Behaviours.Actor;

namespace SS3D.Systems.Gamemodes.UI
{
    public class GamemodeObjectivePanelView : Actor
    {
        [SerializeField] private UiFade _fade;

        [SerializeField] private GamemodeObjectiveItemView _itemViewPrefab;
        [SerializeField] private GameObject _content;
        private Controls.OtherActions _controls;

        private Dictionary<int, GamemodeObjectiveItemView> _gamemodeObjectiveItems;

        protected override void OnEnabled()
        {
            base.OnEnabled();

            _gamemodeObjectiveItems = new Dictionary<int, GamemodeObjectiveItemView>();

            AddHandle(RoundStateUpdated.AddListener(HandleRoundStateUpdated));
            AddHandle(UpdateEvent.AddListener(HandleUpdate));
        }

        protected override void OnStart()
        {
            base.OnStart();

            _fade.SetFade(false);
            _controls = Subsystems.Get<InputSubsystem>().Inputs.Other;
            _controls.Fade.performed += HandleFadePerformed;
            _controls.Fade.canceled += HandleFadeCanceled;
        }

        protected override void OnDestroyed()
        {
            base.OnDestroyed();
            
            _controls.Fade.performed -= HandleFadePerformed;
            _controls.Fade.canceled -= HandleFadeCanceled;
        }

        private void HandleFadePerformed(InputAction.CallbackContext context)
        {
            _fade.SetFade(true);
        }

        private void HandleFadeCanceled(InputAction.CallbackContext context)
        {
            _fade.SetFade(false);
        }

        private void HandleRoundStateUpdated(ref EventContext context, in RoundStateUpdated e)
        {
            RoundState roundState = e.RoundState;

            if (roundState == RoundState.Stopped)
            {
                ClearObjectivesList();
            }
        }

        private void HandleUpdate(ref EventContext context, in UpdateEvent updateEvent)
        {
            ProcessInput();
        }

        private void ProcessInput()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                _fade.SetFade(true);
            }
            if (Input.GetKeyUp(KeyCode.Tab))
            {
                _fade.SetFade(false);
            }
        }

        public void ProcessObjectiveUpdated(GamemodeObjective objective)
        {
            bool hasValue = _gamemodeObjectiveItems.TryGetValue(objective.Id, out GamemodeObjectiveItemView view);

            if (hasValue)
            {
                view.UpdateObjective(objective);
            }

            else
            {
                CreateItemView(objective);
            }
        }

        private void CreateItemView(GamemodeObjective objective)
        {
            GamemodeObjectiveItemView itemView = Instantiate(_itemViewPrefab, _content.transform);
            itemView.SetActive(true);

            _gamemodeObjectiveItems.Add(objective.Id, itemView);
            itemView.UpdateObjective(objective);
        }

        private void ClearObjectivesList()
        {
            foreach (KeyValuePair<int,GamemodeObjectiveItemView> view in _gamemodeObjectiveItems)
            {
                view.Value.GameObject.Destroy();
            }

            _gamemodeObjectiveItems.Clear();
        }
    }
}