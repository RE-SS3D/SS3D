using NUnit.Framework;
using SS3D.Systems.Audio;

namespace EditorTests
{
    /// <summary>Menu music plays on from the main menu into the lobby and fades on spawn or observe (lobby effort Decision 15).</summary>
    public class MenuMusicDemandTests
    {
        [Test]
        public void JoinHandsTheMusicOverToTheLobby()
        {
            MenuMusicDemand demand = new();
            demand.SetMainMenu(true);
            demand.HandOverToLobby(0f);
            demand.SetLobby(null);
            Assert.IsTrue(demand.Wanted(5f), "keeps playing through the session load");
            demand.SetLobby(true);
            Assert.IsTrue(demand.Wanted(60f), "the lobby holds it from here");
        }

        [Test]
        public void SpawningOrObservingFadesIt()
        {
            MenuMusicDemand demand = new();
            demand.SetMainMenu(true);
            demand.HandOverToLobby(0f);
            demand.SetLobby(false);
            Assert.IsFalse(demand.Wanted(0.1f), "a body or a ghost before the lobby reported ends the handover");

            demand.SetLobby(true);
            demand.SetLobby(false);
            Assert.IsFalse(demand.Wanted(1f));
        }

        [Test]
        public void HandoverEndsWhenTheLobbyNeverReports()
        {
            MenuMusicDemand demand = new();
            demand.SetMainMenu(true);
            demand.HandOverToLobby(0f);
            Assert.IsFalse(demand.Wanted(MenuMusicDemand.HandoverSeconds));
        }

        [Test]
        public void HostingFromASilentScreenHandsNothingOver()
        {
            MenuMusicDemand demand = new();
            demand.SetMainMenu(false);
            demand.HandOverToLobby(0f);
            Assert.IsFalse(demand.Wanted(0.1f), "the hosting screen had already faded it");
            demand.SetLobby(true);
            Assert.IsTrue(demand.Wanted(1f), "the lobby brings it back");
        }

        [Test]
        public void LeavingTheSessionDropsTheLobbyClaim()
        {
            MenuMusicDemand demand = new();
            demand.SetLobby(true);
            demand.SetLobby(null);
            Assert.IsFalse(demand.Wanted(0f));
        }
    }
}
