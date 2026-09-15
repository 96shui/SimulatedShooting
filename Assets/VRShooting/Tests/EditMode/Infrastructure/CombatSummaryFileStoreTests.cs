using System;
using System.IO;
using NUnit.Framework;
using VRShooting.Application.Combat;
using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.Tests.EditMode
{
    // BDD17/21 return to main menu: latest summary persistence, no history and recoverable corruption.
    public sealed class CombatSummaryFileStoreTests
    {
        string directory;
        [SetUp] public void Setup() => directory = Path.Combine(Path.GetTempPath(), "vr-combat-store-" + Guid.NewGuid().ToString("N"));
        [TearDown] public void Cleanup() { if (Directory.Exists(directory)) Directory.Delete(directory, true); if (File.Exists(directory)) File.Delete(directory); }
        CombatSummaryDto Summary(string id, TrainingMode mode = TrainingMode.Trench) => new CombatSummaryDto
        {
            SessionId = id, Mode = mode, MapId = mode == TrainingMode.Trench ? "trench-a" : "urban-a", Victory = true,
            ElapsedSeconds = 12.5f, RemainingAmmo = 137, CompletedAtUtc = new DateTime(2026, 9, 14, 0, 0, 0, DateTimeKind.Utc)
        };
        [Test] public void RoundTripReplacesOnlySameModeAndSurvivesNewInstance()
        {
            var store = new CombatSummaryFileStore(directory);
            Assert.That(store.GetLatest(TrainingMode.Trench).ErrorCode, Is.EqualTo(ErrorCode.NotFound));
            Assert.That(store.SaveLatest(Summary("one")).Success); Assert.That(store.SaveLatest(Summary("two")).Success);
            Assert.That(store.SaveLatest(Summary("urban", TrainingMode.Urban)).Success);
            var loaded = new CombatSummaryFileStore(directory).GetLatest(TrainingMode.Trench);
            Assert.That(loaded.Success); Assert.That(loaded.Data.Equals(Summary("two")));
            Assert.That(store.GetLatest(TrainingMode.Urban).Data.SessionId, Is.EqualTo("urban"));
            Assert.That(Directory.GetFiles(directory).Length, Is.EqualTo(2));
        }
        [Test] public void InvalidSummaryDoesNotOverwritePreviousResult()
        {
            var store = new CombatSummaryFileStore(directory); store.SaveLatest(Summary("good"));
            Assert.That(store.SaveLatest(default).ErrorCode, Is.EqualTo(ErrorCode.InvalidInput));
            Assert.That(store.GetLatest(TrainingMode.Trench).Data.SessionId, Is.EqualTo("good"));
            Assert.That(store.GetLatest(TrainingMode.Zeroing100m).ErrorCode, Is.EqualTo(ErrorCode.InvalidInput));
        }
        [Test] public void UnwritablePathAndCorruptionReportPersistenceFailed()
        {
            File.WriteAllText(directory, "path occupied by file");
            var store = new CombatSummaryFileStore(directory);
            Assert.That(store.SaveLatest(Summary("one")).ErrorCode, Is.EqualTo(ErrorCode.PersistenceFailed));
            File.Delete(directory); Assert.That(store.SaveLatest(Summary("one")).Success);
            File.WriteAllText(Path.Combine(directory, "trench-latest.json"), "{ damaged");
            Assert.That(store.GetLatest(TrainingMode.Trench).ErrorCode, Is.EqualTo(ErrorCode.PersistenceFailed));
        }
        [Test] public void LockedDestinationPreservesOldFileAndRemovesTemporaryFile()
        {
            var store = new CombatSummaryFileStore(directory); store.SaveLatest(Summary("one"));
            var target = Path.Combine(directory, "trench-latest.json"); var old = File.ReadAllText(target);
            using (File.Open(target, FileMode.Open, FileAccess.Read, FileShare.Read))
                Assert.That(store.SaveLatest(Summary("two")).ErrorCode, Is.EqualTo(ErrorCode.PersistenceFailed));
            Assert.That(File.ReadAllText(target), Is.EqualTo(old)); Assert.That(Directory.GetFiles(directory).Length, Is.EqualTo(1));
        }
    }
}
