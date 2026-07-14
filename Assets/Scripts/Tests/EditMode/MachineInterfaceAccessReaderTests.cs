using NUnit.Framework;
using SS3D.UI.MachineInterface.Components;

namespace EditorTests
{
    public class MachineInterfaceAccessReaderTests
    {
        [Test]
        public void AtmosIdReaderRow_ShowsDeniedState()
        {
            var row = new AtmosIdReaderRow();
            row.SetAccessState(scanning: false, granted: false, denied: true);

            Assert.AreEqual("Access Denied", row.StatusText);
            Assert.AreEqual("ID not recognized by this unit", row.SubText);
            Assert.AreEqual("Read ID Card", row.ButtonText);
        }

        [Test]
        public void AtmosIdReaderRow_ShowsGrantedState()
        {
            var row = new AtmosIdReaderRow();
            row.SetGrantedSubline("Power unlocked for this session");
            row.SetAccessState(scanning: false, granted: true, denied: false);

            Assert.AreEqual("Access Confirmed", row.StatusText);
            Assert.AreEqual("Power unlocked for this session", row.SubText);
            Assert.AreEqual("Lock Terminal", row.ButtonText);
        }

        [Test]
        public void AtmosIdReaderRow_ShowsScanningState()
        {
            var row = new AtmosIdReaderRow();
            row.SetAccessState(scanning: true, granted: false, denied: false);

            Assert.AreEqual("Reading ID…", row.StatusText);
            Assert.AreEqual("Checking access list", row.SubText);
            Assert.AreEqual("Reading…", row.ButtonText);
        }

        [Test]
        public void AccessGatePanel_ShowsDeniedState()
        {
            var gate = new AccessGatePanel();
            gate.SetState(scanning: false, denied: true);

            Assert.AreEqual("Access Denied", gate.GateHeadline);
            Assert.AreEqual("Card is not on the engineering access list", gate.GateSubline);
            Assert.AreEqual("Swipe ID Card", gate.SwipeLabel);
        }

        [Test]
        public void AccessGatePanel_ShowsScanningState()
        {
            var gate = new AccessGatePanel();
            gate.SetState(scanning: true, denied: false);

            Assert.AreEqual("Reading ID…", gate.GateHeadline);
            Assert.AreEqual("Verifying engineering clearance", gate.GateSubline);
            Assert.AreEqual("Scanning…", gate.SwipeLabel);
        }
    }
}
