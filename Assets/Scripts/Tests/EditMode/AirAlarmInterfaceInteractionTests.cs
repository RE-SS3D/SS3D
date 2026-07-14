using NUnit.Framework;
using SS3D.Systems.Atmospherics.Pipes;
using SS3D.UI.MachineInterface;

namespace EditorTests
{
    public class AirAlarmInterfaceInteractionTests
    {
        [Test]
        public void ApplyPresetMode_RequiresAccessGranted()
        {
            AirAlarmInterfaceViewModel model = AirAlarmInterfaceViewModel.CreateNormal();
            model.AccessGranted = false;

            AirAlarmInterfaceInteractionLogic.ApplyAction(
                model,
                MachineInterfaceControlIds.Atmos.PresetMode,
                (int)AirAlarmPresetMode.Panic);

            Assert.AreEqual(AirAlarmPresetMode.Filtering, model.ActiveMode);
        }

        [Test]
        public void ApplyPresetMode_UpdatesModeWhenAccessGranted()
        {
            AirAlarmInterfaceViewModel model = AirAlarmInterfaceViewModel.CreateNormal();
            model.AccessGranted = true;

            AirAlarmInterfaceInteractionLogic.ApplyAction(
                model,
                MachineInterfaceControlIds.Atmos.PresetMode,
                (int)AirAlarmPresetMode.Panic);

            Assert.AreEqual(AirAlarmPresetMode.Panic, model.ActiveMode);
        }
    }
}
