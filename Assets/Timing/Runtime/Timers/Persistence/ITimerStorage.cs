using System.Linq;
using UnityEngine;

namespace Timing.Timers.Persistence
{
    public interface ITimerStorage
    {
        void Save(string key, string json);
        bool TryLoad(string key, out string json);
    }

    public sealed class PlayerPrefsTimerStorage : ITimerStorage
    {
        public void Save(string key, string json) => PlayerPrefs.SetString(key, json);
        public bool TryLoad(string key, out string json)
        {
            if (!PlayerPrefs.HasKey(key)) { json = null; return false; }
            json = PlayerPrefs.GetString(key);
            return true;
        }
    }

    public sealed class TimerPersistence
    {
        private const string Key = "timing.timers.state.v1";
        private readonly ITimerStorage _storage;

        public TimerPersistence(ITimerStorage storage) => _storage = storage;

        public void Save(TimerScheduler scheduler)
        {
            var list = scheduler.GetAllTimers().Select(t => new TimerEntryDto
            {
                id = t.id,
                domain = t.domain,
                dueMs = t.dueMs,
                intervalMs = t.intervalMs,
                paused = t.paused,
                remainingMsWhenPaused = t.remainingMsWhenPaused,
                callbackId = t.callbackId,
                group = t.group,
                tags = t.tags != null ? t.tags.ToArray() : null
            }).ToArray();

            var dto = new TimerStateDto { timers = list };
            _storage.Save(Key, JsonUtility.ToJson(dto));
        }

        public void LoadInto(TimerScheduler scheduler)
        {
            if (!_storage.TryLoad(Key, out var json)) return;

            TimerStateDto dto;
            try { dto = JsonUtility.FromJson<TimerStateDto>(json); }
            catch { return; }

            if (dto.timers == null) return;

            foreach (var t in dto.timers)
            {
                var entry = new TimerEntry
                {
                    id = t.id,
                    domain = t.domain,
                    dueMs = t.dueMs,
                    intervalMs = t.intervalMs,
                    paused = t.paused,
                    remainingMsWhenPaused = t.remainingMsWhenPaused,
                    callbackId = t.callbackId,
                    group = t.group,
                    tags = t.tags != null ? new System.Collections.Generic.List<string>(t.tags) : null
                };
                scheduler.RestoreTimer(entry);
            }

            // After load, execute overdue timers:
            scheduler.TickDomain(TimerDomain.RealTime);
            scheduler.TickDomain(TimerDomain.AppTime);
            scheduler.TickDomain(TimerDomain.GameplayTime);
        }
    }
}
