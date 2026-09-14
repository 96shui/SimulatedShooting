using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using VRShooting.Application;
using VRShooting.Common;
using VRShooting.Contracts;
using VRShooting.P3.TestSupport;

namespace VRShooting.Tests.EditMode.Infrastructure
{
    // BDD 22: 默认数据/共享契约/配置拒绝/固定种子/错误/最近摘要。
    public sealed class Screen22_P3ContractTests
    {
        [Test]
        public void Screen22_DefaultDtosAreEmptyAndCollectionsAreDefensiveCopies()
        {
            Assert.That(default(TrenchMapDto).MapId, Is.Empty);
            Assert.That(default(TrenchMapDto).SearchNodes, Is.Empty);
            Assert.That(default(UrbanMapDto).Floors, Is.Empty);
            Assert.That(default(UrbanSessionDto).Floors, Is.Empty);
            Assert.That(default(UrbanResultDto).FloorMaps, Is.Empty);
            Assert.That(default(CombatVisualSnapshotDto).Entities, Is.Empty);
            Assert.That(default(TrenchResultDto).Victory, Is.False);
            var source = new List<RoomDto> { new RoomDto { RoomId = "room-1" } };
            var floor = new FloorDto { Rooms = source };
            source.Clear();
            Assert.That(floor.Rooms.Single().RoomId, Is.EqualTo("room-1"));
            Assert.Throws<NotSupportedException>(() => ((IList<RoomDto>)floor.Rooms).Clear());
        }

        [Test]
        public void Screen22_SharedContractAssemblyHasNoUiXrOrRuntimeDependency()
        {
            var assembly = typeof(ITrenchService).Assembly;
            Assert.That(assembly.GetName().Name, Is.EqualTo("VRShooting.Contracts"));
            Assert.That(typeof(AmmoDto).Assembly, Is.SameAs(assembly));
            Assert.That(typeof(RandomSeed).Assembly, Is.SameAs(assembly));
            Assert.That(typeof(HudDto).Assembly, Is.SameAs(assembly));
            Assert.That(typeof(SquadStatusDto).Assembly, Is.SameAs(assembly));
            var forbidden = new[] { "VRShooting.Runtime", "SimulatedShooting.Runtime", "Unity.ugui", "Unity.TextMeshPro", "Unity.XR.Interaction.Toolkit" };
            Assert.That(assembly.GetReferencedAssemblies().Select(x => x.Name).Intersect(forbidden), Is.Empty);
            Assert.That(typeof(FakeTrenchService).Assembly.GetReferencedAssemblies().Select(x => x.Name), Does.Not.Contain("VRShooting.Runtime"));
        }

        [Test]
        public void Screen22_ExistingEnumValuesAndP3DefaultsAreCompatible()
        {
            Assert.That((int)UIEventId.Common_Retry, Is.EqualTo(19));
            Assert.That((int)UIEventId.Trench_SelectMap, Is.EqualTo(20));
            Assert.That((int)TrainingPostureMode.ProneFixed, Is.Zero);
            Assert.That((int)WeaponFireMode.InitialTwoThenAutomatic, Is.EqualTo(1));
            var config = CombatConfigDto.Default;
            Assert.That(config.Validate().Success, Is.True);
            Assert.That(config.FireMode, Is.EqualTo(WeaponFireMode.SingleShot));
            Assert.That(config.InitialAmmo.CurrentMagazine, Is.EqualTo(30));
            Assert.That(config.InitialAmmo.ReserveAmmo, Is.EqualTo(120));
            Assert.That(config.PlayerHealth, Is.EqualTo(100));
            Assert.That(config.TargetRefreshHz, Is.EqualTo(72));
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(0f)]
        [TestCase(-1f)]
        public void Screen22_InvalidConfigurationReturnsInvalidInput(float value)
        {
            var config = CombatConfigDto.Default;
            config = config.WithPlayerHealth(value);
            Assert.That(config.Validate().ErrorCode, Is.EqualTo(ErrorCode.InvalidInput));
        }

        [Test]
        public void Screen22_MapsRejectMissingDuplicateAndInvalidSpawnConfiguration()
        {
            Assert.That(P3Fixtures.TrenchDefinition.Validate().Success, Is.True);
            Assert.That(P3Fixtures.UrbanDefinition.Validate().Success, Is.True);
            Assert.That(default(CombatSceneDefinitionDto).Validate().ErrorCode, Is.EqualTo(ErrorCode.InvalidInput));
            var original = P3Fixtures.TrenchDefinition;
            var duplicate = original.WithSpawnPoints(new[] { original.SpawnPoints[0], original.SpawnPoints[0] });
            Assert.That(duplicate.Validate().ErrorCode, Is.EqualTo(ErrorCode.InvalidInput));
            var insufficient = original.WithSpawnPoints(new[] { original.SpawnPoints[0] });
            Assert.That(insufficient.Validate().ErrorCode, Is.EqualTo(ErrorCode.ResourceUnavailable));
            var points = original.SpawnPoints.ToArray();
            points[0] = new SceneSpawnPointDto { PointId = points[0].PointId, EstimateAreaId = points[0].EstimateAreaId, Group = EncounterGroup.Trench, WorldPosition = Vector3.one * 100, Navigable = false };
            Assert.That(original.WithSpawnPoints(points).Validate().ErrorCode, Is.EqualTo(ErrorCode.ResourceUnavailable));
        }

        [Test]
        public void Screen22_ClockRandomAndWorldInputsAreReplayable()
        {
            var a = new FakeCombatRandom(RandomSeed.Fixed(17));
            var b = new FakeCombatRandom(RandomSeed.Fixed(17));
            var values = Enumerable.Range(0, 30).Select(_ => a.NextInt(3, 6)).ToArray();
            Assert.That(values, Is.EqualTo(Enumerable.Range(0, 30).Select(_ => b.NextInt(3, 6)).ToArray()));
            Assert.That(values.All(x => x >= 3 && x < 6), Is.True);
            Assert.Throws<ArgumentOutOfRangeException>(() => a.NextInt(2, 2));
            var clock = new FakeCombatClock();
            clock.Advance(0.5);
            Assert.That(clock.Now, Is.EqualTo(0.5));
            Assert.That(clock.Tick, Is.EqualTo(1));
            Assert.Throws<ArgumentOutOfRangeException>(() => clock.Advance(double.NaN));
            var world = new FakeCombatWorld();
            ICombatWorldInputPort input = world;
            ICombatNavigationPort navigation = world;
            Assert.That(input.Submit(new CombatInputDto { SessionId = "s", EventId = "hit-1", Kind = CombatInputKind.Hit, Tick = clock.Tick }).Success, Is.True);
            Assert.That(input.Submit(new CombatInputDto { SessionId = "s", EventId = "sense-1", Kind = CombatInputKind.Perception }).Success, Is.True);
            navigation.Move(new CombatNavigationRequestDto { SessionId = "s", RequestId = "nav-1", EntityId = "two" });
            Assert.That(world.Inputs.Select(x => x.EventId), Is.EqualTo(new[] { "hit-1", "sense-1" }));
            Assert.That(world.NavigationRequests.Single().EntityId, Is.EqualTo("two"));
        }

        [Test]
        public void Screen12_14_17_TrenchFakeHasExplicitFramesAndErrorBranches()
        {
            using (var fake = new FakeTrenchService())
            {
                ITrenchService service = fake;
                Assert.That(service.GetMaps().Data.Count, Is.EqualTo(1));
                Assert.That(service.SelectMap("trench-b").ErrorCode, Is.EqualTo(ErrorCode.NotFound));
                Assert.That(service.GetSession("absent").ErrorCode, Is.EqualTo(ErrorCode.NotFound));
                fake.Bind(P3Fixtures.TrenchSession("s", 1));
                var notifications = 0;
                service.SessionChanged += _ => notifications++;
                fake.Publish(P3Fixtures.TrenchSession("s", 2));
                fake.Publish(P3Fixtures.TrenchSession("s", 2));
                fake.Publish(P3Fixtures.TrenchSession("s", 1));
                fake.Publish(P3Fixtures.TrenchSession("old", 100));
                Assert.That(notifications, Is.EqualTo(1));
                Assert.That(service.GetSession("s").Data.Revision, Is.EqualTo(2));
                fake.NextError = ErrorCode.Busy;
                Assert.That(service.MarkSearchNode("s", "node").ErrorCode, Is.EqualTo(ErrorCode.Busy));
                Assert.That(service.CompleteIfReady("s").ErrorCode, Is.EqualTo(ErrorCode.InvalidState));
                Assert.That(service.MarkSearchNode("s", "node").Data.SearchProgress01, Is.Zero, "Fake must not calculate search rules.");
                fake.SetResult(new TrenchResultDto { SessionId = "s", Revision = 3, Victory = true });
                Assert.That(service.GetResult("s").Data.Victory, Is.True);
                Assert.That(service.Cancel("s").Success, Is.True);
                Assert.That(service.Cancel("s").Success, Is.True);
                Assert.That(service.GetSession("s").ErrorCode, Is.EqualTo(ErrorCode.InvalidState));
                fake.Bind(P3Fixtures.TrenchSession("new", 1));
                Assert.That(fake.Publish(P3Fixtures.TrenchSession("s", 500)), Is.False);
            }
        }

        [Test]
        public void Screen18_21_UrbanFakeExposesRoomsResultsAndLaterRejection()
        {
            using (var fake = new FakeUrbanService())
            {
                IUrbanService service = fake;
                fake.Bind(P3Fixtures.UrbanSession("u", 1));
                Assert.That(service.GetMaps().Data.Single().Floors.Count, Is.EqualTo(3));
                Assert.That(service.SelectMap("urban-b").ErrorCode, Is.EqualTo(ErrorCode.NotFound));
                Assert.That(service.ObserveRoom("u", "urban-a.room-001").ErrorCode, Is.EqualTo(ErrorCode.InvalidState));
                fake.NextError = ErrorCode.InvalidState;
                Assert.That(service.EnterBuilding("u", "urban-a.entrance").Success, Is.False);
                Assert.That(service.GetSession("u").Data.Phase, Is.EqualTo(UrbanPhase.Street));
                fake.Publish(P3Fixtures.UrbanSession("u", 2, UrbanPhase.Building));
                Assert.That(service.GetSession("u").Data.CurrentFloorId, Is.EqualTo("urban-a.floor-1"));
                Assert.That(service.MarkRoomSearched("u", "urban-a.room-001").Data.RoomsSearched, Is.Zero);
                fake.SetResult(new UrbanResultDto { SessionId = "u", Revision = 3, Victory = false, RoomsSearched = 1 });
                Assert.That(service.FailByPlayerDeath("u").Data.RoomsSearched, Is.EqualTo(1));
                Assert.That(service.Cancel("old").ErrorCode, Is.EqualTo(ErrorCode.NotFound));
            }
        }

        [TestCase(ErrorCode.Busy)]
        [TestCase(ErrorCode.NotFound)]
        [TestCase(ErrorCode.InvalidState)]
        [TestCase(ErrorCode.ResourceUnavailable)]
        [TestCase(ErrorCode.InvalidInput)]
        public void Screen22_InjectedCommandErrorsDoNotPublishOrMutate(ErrorCode error)
        {
            using (var fake = new FakeUrbanService())
            {
                fake.Bind(P3Fixtures.UrbanSession("u", 1));
                var changes = 0;
                fake.SessionChanged += _ => changes++;
                fake.NextError = error;
                Assert.That(fake.OpenRoomDoor("u", "room").ErrorCode, Is.EqualTo(error));
                Assert.That(fake.GetSession("u").Data.Revision, Is.EqualTo(1));
                Assert.That(changes, Is.Zero);
                fake.Maps = null;
                Assert.That(fake.GetMaps().Data, Is.Empty);
                Assert.That(fake.SelectMap("urban-a").ErrorCode, Is.EqualTo(ErrorCode.NotFound));
                fake.SetResult(new UrbanResultDto { SessionId = "u", Revision = 2, Victory = false });
                Assert.That(fake.SetResult(new UrbanResultDto { SessionId = "u", Revision = 3, Victory = true }), Is.False);
                Assert.That(fake.GetResult("u").Data.Victory, Is.False);
            }
        }

        [Test]
        public void Screen14_15_SquadAndPlayerFixturesHaveVersionedEventsAndNoCommands()
        {
            using (var fake = new FakeCombatStateService())
            {
                ICombatStateService state = fake;
                ISquadCommandService commands = fake;
                fake.Players.Bind(new CombatPlayerSnapshotDto { SessionId = "s", Revision = 1, Player = PlayerStatusDto.Default });
                fake.Squads.Bind(new CombatSquadSnapshotDto { SessionId = "s", Revision = 1, Squad = P3Fixtures.Squad("s") });
                Assert.That(state.GetPlayer("s").Data.Player.Health, Is.EqualTo(100));
                Assert.That(commands.GetSquadStatus("s").Data.Members.Count, Is.EqualTo(3));
                Assert.That(commands.GetAvailableCommands("s").Data, Is.Empty);
                Assert.That(commands.Issue(new SquadCommandRequest { SessionId = "s" }).ErrorCode, Is.EqualTo(ErrorCode.InvalidState));
                Assert.That(commands.OnReloadStarted("s").Success, Is.False);
                var changes = 0;
                state.PlayerChanged += _ => changes++;
                fake.Players.Publish(new CombatPlayerSnapshotDto { SessionId = "s", Revision = 2, Player = new PlayerStatusDto { Health = 90, IsAlive = true } });
                fake.Players.Publish(new CombatPlayerSnapshotDto { SessionId = "old", Revision = 500 });
                Assert.That(changes, Is.EqualTo(1));
                fake.Dispose();
                Assert.That(fake.Players.SubscriberCount, Is.Zero);
            }
        }

        [Test]
        public void Screen12_EmptyCatalogCannotStartAndEstimateIdsMatchSceneData()
        {
            Assert.That(P3Fixtures.TrenchMap.EnemyEstimateAreas.Select(x => x.AreaId),
                Is.EquivalentTo(P3Fixtures.TrenchDefinition.SpawnPoints.Select(x => x.EstimateAreaId)));
            using (var fake = new FakeTrenchService())
            {
                fake.Bind(P3Fixtures.TrenchSession("s", 1));
                fake.Maps = Array.Empty<TrenchMapDto>();
                Assert.That(fake.SelectMap("trench-a").ErrorCode, Is.EqualTo(ErrorCode.NotFound));
                Assert.That(fake.StartSession("trench-a", P3ContractIds.TrainingWeapon, RandomSeed.Fixed(1)).ErrorCode, Is.EqualTo(ErrorCode.NotFound));
            }
        }

        [TestCase("StandingSpeed", -1f)]
        [TestCase("EnemyRange", float.NaN)]
        [TestCase("EnemyAttackInterval", 0f)]
        [TestCase("SnapTurnDegrees", 181f)]
        [TestCase("CpuBudgetMilliseconds", 20f)]
        public void Screen22_RejectsInvalidMovementAttackAndPerformanceBudgets(string field, float value)
        {
            object boxed = CombatConfigDto.Default;
            typeof(CombatConfigDto).GetProperty(field).SetValue(boxed, value);
            Assert.That(((CombatConfigDto)boxed).Validate().ErrorCode, Is.EqualTo(ErrorCode.InvalidInput));
        }

        [Test]
        public void Screen17_21_SummaryFailureDoesNotOverwriteAndCanRetry()
        {
            var store = new FakeCombatSummaryStore();
            ICombatSummaryStore port = store;
            var previous = new CombatSummaryDto { SessionId = "first", MapId = "trench-a", Mode = TrainingMode.Trench };
            Assert.That(port.SaveLatest(previous).Success, Is.True);
            store.NextError = ErrorCode.PersistenceFailed;
            var next = new CombatSummaryDto { SessionId = "second", MapId = "trench-a", Mode = TrainingMode.Trench };
            Assert.That(port.SaveLatest(next).ErrorCode, Is.EqualTo(ErrorCode.PersistenceFailed));
            Assert.That(port.GetLatest(TrainingMode.Trench).Data.SessionId, Is.EqualTo("first"));
            Assert.That(port.SaveLatest(next).Success, Is.True);
            Assert.That(port.GetLatest(TrainingMode.Trench).Data.SessionId, Is.EqualTo("second"));
            Assert.That(port.GetLatest(TrainingMode.Urban).ErrorCode, Is.EqualTo(ErrorCode.NotFound));
        }
    }
}
