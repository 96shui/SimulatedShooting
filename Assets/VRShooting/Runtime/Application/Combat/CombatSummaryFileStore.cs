using System;
using System.Globalization;
using System.IO;
using System.Security;
using UnityEngine;
using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.Application.Combat
{
    /// <summary>One latest summary per P3 mode. Same-directory atomic replacement preserves the previous result on failure.</summary>
    public sealed class CombatSummaryFileStore : ICombatSummaryStore
    {
        readonly string directory;
        readonly object gate = new object();
        public CombatSummaryFileStore(string directory) { this.directory = Path.GetFullPath(directory ?? throw new ArgumentNullException(nameof(directory))); }
        public ServiceResult<Unit> SaveLatest(CombatSummaryDto summary)
        {
            if (!Valid(summary)) return ServiceResult<Unit>.Fail(ErrorCode.InvalidInput);
            lock (gate)
            {
                string temporary = null;
                try
                {
                    Directory.CreateDirectory(directory);
                    var target = FileName(summary.Mode);
                    temporary = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
                    var record = new Record { version = 1, sessionId = summary.SessionId, mode = (int)summary.Mode, mapId = summary.MapId,
                        victory = summary.Victory, elapsedSeconds = summary.ElapsedSeconds, remainingAmmo = summary.RemainingAmmo,
                        completedAtUtc = summary.CompletedAtUtc.ToString("O", CultureInfo.InvariantCulture) };
                    using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    {
                        using (var writer = new StreamWriter(stream, new System.Text.UTF8Encoding(false), 1024, true)) { writer.Write(JsonUtility.ToJson(record)); writer.Flush(); }
                        stream.Flush(true);
                    }
                    if (File.Exists(target)) File.Replace(temporary, target, null);
                    else File.Move(temporary, target);
                    return ServiceResult<Unit>.Ok(Unit.Value);
                }
                catch (Exception ex) when (StorageFailure(ex)) { return ServiceResult<Unit>.Fail(ErrorCode.PersistenceFailed, "Unable to save latest combat result"); }
                finally { if (temporary != null) try { if (File.Exists(temporary)) File.Delete(temporary); } catch (Exception ex) when (StorageFailure(ex)) { /* A failed cleanup must not hide the save result. */ } }
            }
        }
        public ServiceResult<CombatSummaryDto> GetLatest(TrainingMode mode)
        {
            if (!P3(mode)) return ServiceResult<CombatSummaryDto>.Fail(ErrorCode.InvalidInput);
            lock (gate)
            {
                try
                {
                    var record = JsonUtility.FromJson<Record>(File.ReadAllText(FileName(mode)));
                    if (record == null || record.version != 1 || record.mode != (int)mode ||
                        !DateTime.TryParseExact(record.completedAtUtc, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var time))
                        return ServiceResult<CombatSummaryDto>.Fail(ErrorCode.PersistenceFailed, "Invalid combat summary format");
                    var result = new CombatSummaryDto { SessionId = record.sessionId, Mode = mode, MapId = record.mapId, Victory = record.victory,
                        ElapsedSeconds = record.elapsedSeconds, RemainingAmmo = record.remainingAmmo, CompletedAtUtc = time };
                    return Valid(result) ? ServiceResult<CombatSummaryDto>.Ok(result) : ServiceResult<CombatSummaryDto>.Fail(ErrorCode.PersistenceFailed);
                }
                catch (FileNotFoundException) { return ServiceResult<CombatSummaryDto>.Fail(ErrorCode.NotFound); }
                catch (DirectoryNotFoundException) { return ServiceResult<CombatSummaryDto>.Fail(ErrorCode.NotFound); }
                catch (Exception ex) when (StorageFailure(ex)) { return ServiceResult<CombatSummaryDto>.Fail(ErrorCode.PersistenceFailed, "Unable to read latest combat result"); }
            }
        }
        string FileName(TrainingMode mode) => Path.Combine(directory, mode == TrainingMode.Trench ? "trench-latest.json" : "urban-latest.json");
        static bool P3(TrainingMode mode) => mode == TrainingMode.Trench || mode == TrainingMode.Urban;
        static bool Valid(CombatSummaryDto value) => P3(value.Mode) && !string.IsNullOrWhiteSpace(value.SessionId)
            && value.MapId == (value.Mode == TrainingMode.Trench ? P3ContractIds.TrenchMap : P3ContractIds.UrbanMap)
            && !float.IsNaN(value.ElapsedSeconds) && !float.IsInfinity(value.ElapsedSeconds) && value.ElapsedSeconds >= 0 && value.RemainingAmmo >= 0
            && value.CompletedAtUtc.Kind == DateTimeKind.Utc && value.CompletedAtUtc != default;
        static bool StorageFailure(Exception ex) => ex is IOException || ex is UnauthorizedAccessException || ex is SecurityException || ex is ArgumentException || ex is NotSupportedException;
        [Serializable] sealed class Record
        {
            public int version, mode, remainingAmmo;
            public string sessionId, mapId, completedAtUtc;
            public bool victory;
            public float elapsedSeconds;
        }
    }
}
