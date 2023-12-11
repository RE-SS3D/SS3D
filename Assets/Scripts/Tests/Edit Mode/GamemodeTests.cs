using System.Collections.Generic;
using NUnit.Framework;
using SS3D.Systems.Gamemodes;
using SS3D.Systems.GameModes.Modes;
using SS3D.Tests;
using UnityEditor;
using UnityEngine;

namespace EditorTests
{
    public class GamemodeTests : EditModeTest
    {
        #region Class variables
        /// <summary>
        /// Toggles whether any test-specific Debug.Log's are displayed in the console.
        /// </summary>
        private bool SHOW_DEBUG = false;
        #endregion

        #region Test set up
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
        }
        #endregion

        #region Tests

        /// <summary>
        /// Test to confirm that the GamemodeObjective.Succeed() method correctly sets the state.
        /// </summary>
        [Test]
        public void SucceedingAnObjectiveShouldSetStatusToSuccess()
        {

            // ARRANGE
            GamemodeObjective sut = ScriptableObject.CreateInstance<GamemodeObjective>();
            Assert.IsFalse(sut.Succeeded);

            // ACT
            sut.Succeed();

            // ASSERT
            Assert.IsTrue(sut.Succeeded);
        }

        /// <summary>
        /// Test to confirm that the GamemodeObjective.Fail() method correctly sets the state.
        /// </summary>
        [Test]
        public void FailingAnObjectiveShouldSetStatusToFailed()
        {

            // ARRANGE
            GamemodeObjective sut = ScriptableObject.CreateInstance<GamemodeObjective>();
            Assert.IsFalse(sut.Failed);

            // ACT
            sut.Fail();

            // ASSERT
            Assert.IsTrue(sut.Failed);
        }

        /// <summary>
        /// Test to confirm that the GamemodeObjective.Succeed() method does not change the
        /// status if the current status is not InProgress.
        /// </summary>
        [Test]
        [TestCase(ObjectiveStatus.Cancelled)]
        [TestCase(ObjectiveStatus.Failed)]

        public void ObjectiveDoesNotSucceedUnlessItIsFirstInProgress(ObjectiveStatus startingStatus)
        {

            // ARRANGE
            if (startingStatus == ObjectiveStatus.InProgress || startingStatus == ObjectiveStatus.Success) return;
            GamemodeObjective sut = ScriptableObject.CreateInstance<GamemodeObjective>();
            sut.SetStatus(startingStatus);

            // ACT
            sut.Succeed();

            // ASSERT
            Assert.IsTrue(sut.Status == startingStatus);
        }

        /// <summary>
        /// Test to confirm that the GamemodeObjective.Fail() method does not change the
        /// status if the current status is not InProgress.
        /// </summary>
        [Test]
        [TestCase(ObjectiveStatus.Cancelled)]
        [TestCase(ObjectiveStatus.Success)]
        public void ObjectiveDoesNotFailUnlessItIsFirstInProgress(ObjectiveStatus startingStatus)
        {

            // ARRANGE
            if (startingStatus == ObjectiveStatus.InProgress || startingStatus == ObjectiveStatus.Failed) return;
            GamemodeObjective sut = ScriptableObject.CreateInstance<GamemodeObjective>();
            sut.SetStatus(startingStatus);

            // ACT
            sut.Fail();

            // ASSERT
            Assert.IsTrue(sut.Status == startingStatus);
        }

        /// <summary>
        /// Test to confirm that the GamemodeObjective.InitializeObjective() method changes the current status
        /// to InProgress.
        /// </summary>
        [Test]
        [TestCase(ObjectiveStatus.Failed)]
        [TestCase(ObjectiveStatus.Cancelled)]
        [TestCase(ObjectiveStatus.Success)]
        public void InitializeObjectiveSetsStatusToInProgress(ObjectiveStatus startingStatus)
        {

            // ARRANGE
            if (startingStatus == ObjectiveStatus.InProgress) return;
            GamemodeObjective sut = ScriptableObject.CreateInstance<GamemodeObjective>();
            sut.SetStatus(startingStatus);

            // ACT
            sut.InitializeObjective();

            // ASSERT
            Assert.IsTrue(sut.InProgress);
        }

        /// <summary>
        /// Test to confirm that every player in the game gets at least one objective when the Gamemode is initialized.
        /// </summary>
        /// <param name="sut">Gamemode (acquired from TestCaseSource) as the System Under Test</param>
        [Test]
        [TestCaseSource(nameof(AllGamemodes))]
        public void InitializeGamemodeCreatesObjectivesForEachPlayer(Gamemode sut)
        {

            // ARRANGE
            const int NUMBER_OF_PLAYERS_TO_TEST = 32;
            List<string> Ckeys = SampleCkeys(NUMBER_OF_PLAYERS_TO_TEST);

            // ACT
            sut.InitializeGamemode(Ckeys);

            // ASSERT
            foreach (string key in Ckeys)
            {
                // Every player should have at least one objective
                Assert.IsTrue(sut.GetPlayerObjectives(key).Count > 0);
                if (SHOW_DEBUG) Debug.Log($"{key} has {sut.GetPlayerObjectives(key).Count} objectives");
            }
        }

        /// <summary>
        /// Test to confirm that all objectives are no longer In Progress when the Gamemode is finalized.
        /// </summary>
        [Test]
        public void FinalizeGamemodeChangesAllObjectivesFromInProgress()
        {

            // ARRANGE
            const int NUMBER_OF_PLAYERS_TO_TEST = 32;
            List<string> Ckeys = SampleCkeys(NUMBER_OF_PLAYERS_TO_TEST);
            Gamemode sut = CreateGamemodeWithSingleObjective(CollaborationType.Individual);
            sut.InitializeGamemode(Ckeys);
            Assert.IsTrue(sut.RoundObjectives.Count > 0);

            // ACT
            sut.FinalizeGamemode();

            // ASSERT
            Assert.IsTrue(sut.RoundObjectives.Count > 0);
            foreach(GamemodeObjective objective in sut.RoundObjectives)
            {
                Assert.IsFalse(objective.InProgress);
            }
        }

        /// <summary>
        /// Test to confirm that all objectives are no longer In Progress when the Gamemode is finalized.
        /// </summary>
        [Test]
        public void FinalizeGamemodeDoesNotChangeStatusOfCompletedObjectives()
        {

            // ARRANGE
            const int NUMBER_OF_PLAYERS_TO_TEST = 32;
            List<string> Ckeys = SampleCkeys(NUMBER_OF_PLAYERS_TO_TEST);
            Gamemode sut = CreateGamemodeWithSingleObjective(CollaborationType.Individual);
            sut.InitializeGamemode(Ckeys);
            Assert.IsTrue(sut.RoundObjectives.Count > 0);

            // ACT
            sut.RoundObjectives[0].Succeed();
            sut.RoundObjectives[1].Cancel();
            sut.FinalizeGamemode();

            // ASSERT
            Assert.IsTrue(sut.RoundObjectives[0].Succeeded);
            Assert.IsTrue(sut.RoundObjectives[1].Cancelled);
            for (int i = 2; i < sut.RoundObjectives.Count; i++)
            {
                Assert.IsTrue(sut.RoundObjectives[i].Failed);
            }
        }

        /// <summary>
        /// Test to confirm that when one player succeeds in a multi-participant cooperative objective, all other players
        /// involved in that objective also succeed.
        /// </summary>
        [Test]
        public void CooperativeObjectivesSucceedWhenOnePlayerCompletesThem()
        {
            // ARRANGE
            const int NUMBER_OF_PLAYERS_TO_TEST = 32;
            const int PLAYERS_PER_OBJECTIVE = 8;
            List<string> Ckeys = SampleCkeys(NUMBER_OF_PLAYERS_TO_TEST);
            Gamemode sut = CreateGamemodeWithSingleObjective(CollaborationType.Cooperative, PLAYERS_PER_OBJECTIVE, PLAYERS_PER_OBJECTIVE);
            sut.InitializeGamemode(Ckeys);

            // ACT
            sut.RoundObjectives[0].Succeed();

            // ASSERT
            int countOfSucceededObjectives = 0;
            for (int i = 0; i < sut.RoundObjectives.Count; i++)
            {
                if (sut.RoundObjectives[i].Succeeded) countOfSucceededObjectives++;
            }
            Assert.IsTrue(countOfSucceededObjectives == PLAYERS_PER_OBJECTIVE);
        }

        /// <summary>
        /// Test to confirm that when one player succeeds in a multi-participant cooperative objective, all other players
        /// involved in that objective also succeed.
        /// </summary>
        [Test]
        public void CompetitiveObjectivesFailWhenOnePlayerCompletesThem()
        {
            // ARRANGE
            const int NUMBER_OF_PLAYERS_TO_TEST = 32;
            const int PLAYERS_PER_OBJECTIVE = 8;
            List<string> Ckeys = SampleCkeys(NUMBER_OF_PLAYERS_TO_TEST);
            Gamemode sut = CreateGamemodeWithSingleObjective(CollaborationType.Competitive, PLAYERS_PER_OBJECTIVE, PLAYERS_PER_OBJECTIVE);
            sut.InitializeGamemode(Ckeys);

            // ACT
            sut.RoundObjectives[0].Succeed();

            // ASSERT
            int countOfSucceededObjectives = 0;
            int countOfFailedObjectives = 0;
            for (int i = 0; i < sut.RoundObjectives.Count; i++)
            {
                if (sut.RoundObjectives[i].Succeeded) countOfSucceededObjectives++;
                if (sut.RoundObjectives[i].Failed) countOfFailedObjectives++;
            }

            Assert.IsTrue(countOfSucceededObjectives == 1);
            Assert.IsTrue(countOfFailedObjectives == PLAYERS_PER_OBJECTIVE - 1);
        }

        /// <summary>
        /// Test to confirm that all objectives are cleared when the Gamemode is reset.
        /// </summary>
        [Test]
        public void ResetGamemodeClearsAllObjectives()
        {
            // ARRANGE
            const int NUMBER_OF_PLAYERS_TO_TEST = 32;
            List<string> Ckeys = SampleCkeys(NUMBER_OF_PLAYERS_TO_TEST);
            Gamemode sut = CreateGamemodeWithSingleObjective(CollaborationType.Individual);
            sut.InitializeGamemode(Ckeys);
            Assert.IsTrue(sut.RoundObjectives.Count > 0);

            // ACT
            sut.ResetGamemode();

            // ASSERT
            Assert.IsTrue(sut.RoundObjectives.Count == 0);
        }

        /// <summary>
        /// Test to confirm that late joining players also get objectives.
        /// </summary>
        [Test]
        public void LateJoiningPlayersGetObjectives()
        {
            // ARRANGE
            const int NUMBER_OF_PLAYERS_TO_TEST = 4;
            const string lateJoinPlayerCkey = "lateJoinPlayer";
            List<string> Ckeys = SampleCkeys(NUMBER_OF_PLAYERS_TO_TEST);
            Gamemode sut = CreateGamemodeWithSingleObjective(CollaborationType.Individual);
            sut.InitializeGamemode(Ckeys);

            // ACT
            sut.CreateLateJoinObjective(lateJoinPlayerCkey);

            // ASSERT
            Assert.IsTrue(sut.RoundObjectives[sut.RoundObjectives.Count - 1].AssigneeCkey.Equals(lateJoinPlayerCkey));
            Assert.IsTrue(sut.RoundObjectives[sut.RoundObjectives.Count - 1].InProgress);
        }

        /// <summary>
        /// Test to confirm that individual objectives all initialize without error.
        /// </summary>
        /// <param name="sut">Gamemode (acquired from TestCaseSource) as the System Under Test</param>
        [Test]
        [TestCaseSource(nameof(AllObjectives))]
        public void GamemodeObjectiveInitializesWithoutError(GamemodeObjective sut)
        {

            // ARRANGE

            // ACT
            sut.InitializeObjective();

            // ASSERT
            Assert.IsTrue(sut.InProgress);
        }
        #endregion

        #region Helper functions

        /// <summary>
        /// Returns all gamemodes saved within the relevant directory, for use as TestCaseSource.
        /// </summary>
        /// <returns>Copies of all gamemodes</returns>
        private static List<Gamemode> AllGamemodes()
        {
            const string FOLDER_PATH = "Assets/Content/Data/Gamemode";
            List<Gamemode> allGamemodes = new();
            string[] assetNames = AssetDatabase.FindAssets($"t:{typeof(Gamemode)}", new[] { FOLDER_PATH });
            foreach(string SOName in assetNames)
            {
                var SOpath = AssetDatabase.GUIDToAssetPath(SOName);
                var gamemode = AssetDatabase.LoadAssetAtPath<Gamemode>(SOpath);
                allGamemodes.Add(ScriptableObject.Instantiate(gamemode));
            }
            return allGamemodes;
        }

        /// <summary>
        /// Returns all gamemode objectives saved within the relevant directory, for use as TestCaseSource.
        /// </summary>
        /// <returns>Copies of all gamemode objectives</returns>
        private static List<GamemodeObjective> AllObjectives()
        {
            const string FOLDER_PATH = "Assets/Content/Data/Gamemode";
            List<GamemodeObjective> allGamemodeObjectives = new();
            string[] assetNames = AssetDatabase.FindAssets($"t:{typeof(GamemodeObjective)}", new[] { FOLDER_PATH });
            foreach (string SOName in assetNames)
            {
                var SOpath = AssetDatabase.GUIDToAssetPath(SOName);
                var gamemodeObjective = AssetDatabase.LoadAssetAtPath<GamemodeObjective>(SOpath);
                allGamemodeObjectives.Add(ScriptableObject.Instantiate(gamemodeObjective));
            }
            return allGamemodeObjectives;
        }

        /// <summary>
        /// Returns a sample list of spawned player Ckeys.
        /// </summary>
        /// <param name="numberOfPlayers">The number of Ckeys to generate</param>
        /// <returns>A list containing unique Ckeys</returns>
        private List<string> SampleCkeys(int numberOfPlayers)
        {
            // Keep number of players within reasonable bounds
            const int MIN_PLAYERS = 1;
            const int MAX_PLAYERS = 300;

            // Validation of player numbers
            if (numberOfPlayers < MIN_PLAYERS)
            {
                numberOfPlayers = MIN_PLAYERS;
                Debug.LogWarning($"numberOfPlayers set at the minimum number of {MIN_PLAYERS}");
            }

            if (numberOfPlayers > MAX_PLAYERS)
            {
                numberOfPlayers = MAX_PLAYERS;
                Debug.LogWarning($"numberOfPlayers set at the maximum number of {MAX_PLAYERS}");
            }

            // Generate and return the list
            List<string> ckeys = new List<string>();
            for (int i = 0; i < numberOfPlayers; i++)
            {
                ckeys.Add($"player_{i}");
            }
            return ckeys;
        }

        private Gamemode CreateGamemodeWithSingleObjective(CollaborationType collaborationType, int maxAssigneesPerObjective = 1, int minAssigneesPerObjective = 1)
        {
            Gamemode gamemode = new Gamemode();

            GamemodeObjective gamemodeObjective = ObjectiveFactory.Create("Sample Objective", collaborationType, Alignment.Any, minAssigneesPerObjective, maxAssigneesPerObjective);

            GamemodeObjectiveCollectionEntry collectionEntry = new GamemodeObjectiveCollectionEntry();
            collectionEntry.GamemodeObjective = gamemodeObjective;
            collectionEntry.AssignmentProbability = 100f;
            collectionEntry.RemainingAssignments = 1000;

            GamemodeObjectiveCollection collection = new GamemodeObjectiveCollection();
            collection.Entries = new List<GamemodeObjectiveCollectionEntry>();
            collection.Entries.Add(collectionEntry);

            gamemode.PossibleObjectives = collection;

            return gamemode;
        }
        #endregion
    }
}
