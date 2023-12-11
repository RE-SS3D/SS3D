using System.Collections.Generic;
using System.Linq;
using SS3D.Core.Behaviours;
using SS3D.UI.Buttons;
using UnityEngine;

namespace SS3D.UI
{
    public class GenericTabControllerView : Actor
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
            if (tab == null)
            {
                return;
            }

            foreach (GenericTabView tabView in _tabs.Where(tabView => tabView != null))
            {
                tabView.SetTabActive(tab == tabView);
            }
        }
    }
}
