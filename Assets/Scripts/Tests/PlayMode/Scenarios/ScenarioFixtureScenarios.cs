using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SS3D.Tests
{
    // Guards: a new round restores the station template's air, so breathing or a scenario's gas never leaks into the next round or scenario (round start/stop atmos reset) · Not cheaper because: needs the atmos simulation torn down and reseeded by a real round stop and start
    [Category(TestCategories.Scenarios)]
    [Category(TestCategories.Atmos)]
    public class ScenarioFixtureScenarios : ScenarioFixture
    {
        [UnityTest]
        public IEnumerator ReloadedRoom_HasStationAirAgain()
        {
            yield return Console.Expect("atmos gas carbondioxide 20", "Added 20.0 mol");
            float start = Time.time;
            while (ScenarioAtmos.Read().MaxCarbonDioxide <= 0.01f)
            {
                Assert.Less(Time.time - start, 5f, "The added CO₂ shows up in the room's air");
                yield return null;
            }

            yield return ReloadRoom();
        }
    }
}
