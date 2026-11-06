using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SS3D.Core;
using SS3D.Systems.Atmospherics;
using SS3D.Systems.Atmospherics.Pipes;
using SS3D.Systems.Entities;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.StructuralDamage;
using SS3D.Systems.Tile;
using UnityEngine;
using UnityEngine.TestTools;

namespace SS3D.Tests
{
    /// <summary>A real C4 set off in the middle of the test room, where its walls take too little force to break.</summary>
    [Category(TestCategories.Scenarios)]
    [Category(TestCategories.Explosives)]
    public class ExplosivesScenarios : ScenarioFixture
    {
        private const string C4 = "d5222e435d7aa2d45b959a73ebc874c9";
        private const float C4Yield = 180f;
        private const float C4Falloff = 45f;

        private static readonly Vector3 Epicenter = new(0f, 0f, -1f);

        /// <summary>
        /// One tile east of here a body has two metres to fly. With <see cref="OpenRunYield"/> it takes
        /// force 90 and the room's walls survive: a breach's wind would hold it down.
        /// </summary>
        private static readonly Vector3 OpenRunEpicenter = new(-1f, 0f, -1f);
        private const float OpenRunYield = 135f;

        private static TileSubSystem Tiles => SubSystems.Get<TileSubSystem>();

        // Guards: one C4 strips floors without venting an undamaged room, a second breaches the cracked hull, and plenums beside bare cells draw so space does not show under the neighbouring floor (explosives-destruction §3, TileUnderfloorVisibility) · Not cheaper because: needs the real charge, atmos, FishNet observers and renderer state on a live host together
        [UnityTest]
        public IEnumerator C4_CracksFreshHullWithoutVenting_ThenASecondBlastBreachesIt()
        {
            ChangesRoom = true;
            Teleport(Body, new Vector3(5f, Body.transform.position.y, 1f));
            var diagonalOnly = new Vector3(2f, 0f, 1f);
            var sideRoom = new Vector3(5f, 0f, -1f);
            TileCoord epicenter = Coord(Epicenter);

            Item charge = SubSystems.Get<ItemSubSystem>().SpawnItem(C4, Epicenter + Vector3.up * 0.2f, Quaternion.identity);
            yield return Hold(0.5f);
            Assert.IsFalse(IsDrawn(Plenum(diagonalOnly)), "A plenum under an intact room stays hidden");

            typeof(TimedCharge).GetMethod("Detonate", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(charge.GetComponent<TimedCharge>(), null);
            Assert.IsTrue(IsDrawn(Plenum(diagonalOnly)), "The surrounding plenum shows at once: remote clients have no timed observer pass to fall back on");
            yield return Hold(3f);

            Assert.IsFalse(Tiles.QueryService.TryGetOccupant(epicenter, TileLayer.Turf, Direction.North, out _), "The floor at the charge is gone");
            Assert.AreEqual(StructuralIntegrityStage.Cracked, Plenum(Epicenter).IntegrityStage, "One charge cracks the plating");
            Assert.LessOrEqual(ScenarioAtmos.Read().MaxPressureDeviationKpa, 0.5f, "Cracked plating holds the room's air");
            Assert.IsTrue(Tiles.QueryService.TryGetOccupant(Coord(diagonalOnly), TileLayer.Turf, Direction.North, out _), "The corner keeps its floor");
            Assert.IsTrue(IsDrawn(Plenum(diagonalOnly)), "A diagonal bare cell keeps the plenum under this floor drawn");
            Assert.IsFalse(IsDrawn(Plenum(sideRoom)), "The side room stays fully covered");

            SubSystems.Get<StructuralDamageSubSystem>().ResolveBlast(epicenter, C4Yield, C4Falloff);

            Assert.IsTrue(Tiles.QueryService.TryGetOccupancy(epicenter, out TileOccupancy occupancy) && !occupancy.HasPlenum, "The second blast opens the hull");
            AtmosSimulation simulation = SubSystems.Get<AtmosSubSystem>().Simulation;
            yield return WaitFor(() => AtmosAreaSampler.TrySampleTile(Coord(diagonalOnly), simulation, out AtmosAreaSample corner)
                && corner.AveragePressureKpa < AtmosConstants.StandardPressure - 5f, "the room vents through the hole", 10f);
        }

        // Guards: a blast throws a floored owner straight away from the epicenter through the physics authority · Not cheaper because: FishNet TargetRpc delivery and bone physics need a live body
        [UnityTest]
        public IEnumerator CloseBlast_KnocksDownAndThrowsOwnerAway()
        {
            ChangesRoom = true;
            EntityInvincibility.SetServer(Body.Owner.ClientId, true);
            try
            {
                Ragdoll ragdoll = Body.GetComponent<Ragdoll>();
                Vector3 start = OpenRunEpicenter + Vector3.right;
                Teleport(Body, new Vector3(start.x, Body.transform.position.y, start.z));
                yield return Hold(0.5f);

                SubSystems.Get<StructuralDamageSubSystem>().ResolveBlast(Coord(OpenRunEpicenter), OpenRunYield, C4Falloff);
                Assert.AreEqual(BodyPresentationState.Collapsed, ragdoll.Presentation, "Force 90 floors the body");
                yield return Hold(0.6f);

                Assert.That(ragdoll.WindSamplePosition.x, Is.GreaterThan(start.x + 0.5f), "The body flies away from the charge");
                Assert.That(Mathf.Abs(ragdoll.WindSamplePosition.z - start.z), Is.LessThan(0.5f), "In a straight line");
                yield return WaitFor(() => ragdoll.Presentation == BodyPresentationState.Locomotion, "the body gets up afterwards", 8f);
            }
            finally
            {
                EntityInvincibility.ClearServer(Body.Owner.ClientId);
            }
        }

        // Guards: bones collide discretely, so BlastPush.MaxSpeed must not step them through a wall · Not cheaper because: needs real wall colliders and ragdoll physics
        [UnityTest]
        public IEnumerator FullSpeedLaunch_StopsAtAWall()
        {
            int x = (int)Epicenter.x;
            while (!(Tiles.QueryService.TryGetOccupancy(Coord(new Vector3(x, 0f, Epicenter.z)), out TileOccupancy tile) && tile.BlockedEdges != 0))
                Assert.Less(++x, 20, "The room has a wall to the east");

            Ragdoll ragdoll = Body.GetComponent<Ragdoll>();
            Teleport(Body, new Vector3(x - 2f, Body.transform.position.y, Epicenter.z));
            yield return Hold(0.5f);

            ragdoll.ServerApplyBlast(Vector3.right * BlastPush.MaxSpeed, knockdown: true);

            // A body collapses away from the wall after it hits, so where it ends up proves nothing.
            float furthest = float.MinValue;
            float start = Time.time;
            while (Time.time - start < 1f)
            {
                furthest = Mathf.Max(furthest, ragdoll.WindSamplePosition.x);
                yield return null;
            }

            Assert.That(furthest, Is.InRange(x - 0.75f, x + 0.5f), "The body reaches the wall and never passes it");
        }

        // Guards: force too low to floor anyone still shoves an upright owner, which is CharacterController motion and not bone physics · Not cheaper because: the controller path needs a live owned body
        [UnityTest]
        public IEnumerator DistantBlast_ShovesUprightOwner_WithoutKnockdown()
        {
            ChangesRoom = true;
            EntityInvincibility.SetServer(Body.Owner.ClientId, true);
            try
            {
                Ragdoll ragdoll = Body.GetComponent<Ragdoll>();
                Vector3 start = Epicenter + Vector3.right;
                Teleport(Body, new Vector3(start.x, Body.transform.position.y, start.z));
                yield return Hold(0.5f);
                start = ragdoll.WindSamplePosition;

                SubSystems.Get<StructuralDamageSubSystem>().ResolveBlast(Coord(Epicenter), 80f, 20f);
                yield return Hold(0.5f);

                Assert.AreEqual(BodyPresentationState.Locomotion, ragdoll.Presentation, "Force 60 leaves the body standing");
                Assert.That(ragdoll.WindSamplePosition.x, Is.GreaterThan(start.x + 0.3f), "The shove moves it away from the charge");
            }
            finally
            {
                EntityInvincibility.ClearServer(Body.Owner.ClientId);
            }
        }

        // Guards: nobody owns a combat dummy, so the server must run its knockdown and launch itself · Not cheaper because: the unowned authority fallback needs live bones
        [UnityTest]
        public IEnumerator CloseBlast_ThrowsUnownedBody()
        {
            ChangesRoom = true;
            Teleport(Body, new Vector3(5f, Body.transform.position.y, 1f));
            Vector3 start = OpenRunEpicenter + Vector3.right;
            Entity dummy = SubSystems.Get<EntitySubSystem>().ServerSpawnCombatDummy(start, Quaternion.identity);
            yield return Hold(0.5f);
            Ragdoll ragdoll = dummy.GetComponent<Ragdoll>();
            start = ragdoll.WindSamplePosition;

            SubSystems.Get<StructuralDamageSubSystem>().ResolveBlast(Coord(OpenRunEpicenter), OpenRunYield, C4Falloff);
            yield return Hold(0.6f);

            Assert.IsTrue(ragdoll.IsKnockedDown, "The dummy goes down");
            Assert.That(ragdoll.WindSamplePosition.x, Is.GreaterThan(start.x + 0.5f), "The server launched its bones");
        }

        private static TileCoord Coord(Vector3 position) => Tiles.QueryService.WorldToTile(position, Tiles.CurrentMap.MapId);

        private static PlacedTileObject Plenum(Vector3 position)
        {
            Assert.IsTrue(Tiles.QueryService.TryGetOccupant(Coord(position), TileLayer.Plenum, Direction.North, out ITileOccupant occupant),
                $"A plenum exists at {position}");
            return (PlacedTileObject)occupant;
        }

        private static bool IsDrawn(PlacedTileObject tile) => tile.GetComponentsInChildren<Renderer>(true)
            .Any(renderer => renderer.enabled && !renderer.forceRenderingOff);
    }
}
