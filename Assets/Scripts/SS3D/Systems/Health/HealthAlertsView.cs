using SS3D.Attributes;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.UI.Settings;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Health
{
    [RequiredLayer("UI")]
    public class HealthAlertsView : View
    {
        [SerializeField] private GameObject _bleedingChip;
        [SerializeField] private GameObject _criticalChip;
        [SerializeField] private GameObject _cardiacArrestChip;

        private HumanHealthController _controller;

        protected override void OnStart()
        {
            base.OnStart();
            var uiVisibilityView = ViewLocator.Get<GlobalUiVisibilityControllerView>().First();
            uiVisibilityView.RegisterToggle(GameObject);
            Refresh();
        }

        public void AssignViewToPlayer(HumanHealthController controller)
        {
            _controller = controller;
            Refresh();
        }

        public void UnassignViewFromPlayer(HumanHealthController controller)
        {
            if (_controller == controller)
            {
                _controller = null;
                Refresh();
            }
        }

        public void Refresh()
        {
            if (_bleedingChip != null)
            {
                bool bleeding = _controller != null && _controller.Snapshot.IsBleeding;
                _bleedingChip.SetActive(bleeding);
            }

            if (_criticalChip != null)
            {
                bool critical = _controller != null && _controller.Snapshot.State == HealthState.Critical;
                _criticalChip.SetActive(critical);
            }

            if (_cardiacArrestChip != null)
            {
                bool cardiacArrest = _controller != null && _controller.Snapshot.IsCardiacArrest;
                _cardiacArrestChip.SetActive(cardiacArrest);
            }
        }
    }
}
