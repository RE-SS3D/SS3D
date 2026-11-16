using NUnit.Framework;
using SS3D.UI.MainHud;
using SS3D.UI.MainHud.Components;
using UnityEngine.UIElements;

namespace EditorTests
{
    /// <summary>The worn-equipment doll starts collapsed behind its toggle (main-hud.md §4: no permanent chrome).</summary>
    public sealed class EquipmentGridToggleTests
    {
        [Test]
        public void StartsCollapsed()
        {
            EquipmentGrid grid = new(default(MainHudIconSet));

            Assert.IsFalse(grid.Expanded);
            Assert.AreEqual(DisplayStyle.None, Panel(grid).style.display.value);
        }

        [Test]
        public void SetExpanded_ShowsAndHidesTheDoll()
        {
            EquipmentGrid grid = new(default(MainHudIconSet));

            grid.SetExpanded(true);
            Assert.AreEqual(DisplayStyle.Flex, Panel(grid).style.display.value);

            grid.SetExpanded(false);
            Assert.AreEqual(DisplayStyle.None, Panel(grid).style.display.value);
        }

        private static VisualElement Panel(EquipmentGrid grid) => grid.Q(className: "equipment-grid__panel");
    }
}
