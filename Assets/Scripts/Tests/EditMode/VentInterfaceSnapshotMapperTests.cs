using NUnit.Framework;
using SS3D.UI.MachineInterface;

namespace EditorTests
{
    public class VentInterfaceSnapshotMapperTests
    {
        [Test]
        public void IdleVent_ShowsTheRoomItStandsIn_NotMockupPressures()
        {
            VentInterfaceViewModel model = VentInterfaceSnapshotMapper.ToViewModel(new VentInterfaceSnapshot
            {
                Scenario = (byte)VentScenario.Idle,
                Powered = true,
                TargetPressureKpa = 101,
                RoomPressureKpa = 0.7f,
                LinePressureKpa = 258f,
            });

            Assert.AreEqual("0.7 kPa", model.ExternalPressureText);
            Assert.AreEqual("258.0 kPa", model.InternalPressureText);
            Assert.AreEqual(StatusTone.Warning, model.ExternalTone);
            Assert.AreNotEqual("Pressure equalized.", model.StatusSubline);
        }
    }
}
