using System;
using System.Collections;
using System.Collections.Generic;
using SS3D.Core.Behaviours;
using SS3D.UI.Buttons;
using UnityEngine;
using UnityEngine.Events;

public class GenericTabControllerView : SpessBehaviour
{
    [SerializeField] private List<GenericTabView> _tabs;

    protected override void OnStart()
    {
        base.OnStart();

        SetupTabs();
        HandleTabButtonPressed(_tabs[0]);
    }

    private void SetupTabs()
    {
        foreach (GenericTabView tab in _tabs)
        {
            tab.Button.onClick.AddListener(() => HandleTabButtonPressed(tab));
        }
    }

    private void HandleTabButtonPressed(GenericTabView tab)
    {
        foreach (GenericTabView tabView in _tabs)
        {
            tab.SetActive(tabView == tab);
        }
    }
}
