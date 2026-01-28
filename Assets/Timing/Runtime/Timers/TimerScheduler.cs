using System;
using System.Collections.Generic;

namespace Timing.Timers
{
    public sealed class TimerScheduler
    {
        private readonly Dictionary<int, TimerEntry> _byId = new();

        // One heap per domain
        private readonly IndexedMinHeap<TimerEntry> _realHeap;
        private readonly IndexedMinHeap<TimerEntry> _appHeap;
        private readonly IndexedMinHeap<TimerEntry> _gameHeap;

        // group/tag indexes for bulk ops
        private readonly Dictionary<string, HashSet<int>> _byGroup = new(StringComparer.Ordinal);
        private readonly Dictionary<string, HashSet<int>> _byTag = new(StringComparer.Ordinal);

        private int _nextId = 1;

        private readonly ITimeDomain _real;
        private readonly ITimeDomain _app;
        private readonly ITimeDomain _game;
        private readonly TimerCallbackRegistry _registry;

        public TimerScheduler(ITimeDomain real, ITimeDomain app, ITimeDomain gameplay, TimerCallbackRegistry registry)
        {
            _real = real; _app = app; _game = gameplay;
            _registry = registry;

            _realHeap = MakeHeap();
            _appHeap = MakeHeap();
            _gameHeap = MakeHeap();
        }

        private static IndexedMinHeap<TimerEntry> MakeHeap() =>
            new IndexedMinHeap<TimerEntry>(
                getId: t => t.id,
                getDueMs: t => t.dueMs,
                setDueMs: (t, v) => t.dueMs = v
            );

        public TimerHandle After(long delayMs, string callbackId, TimerDomain domain, string group = null, params string[] tags)
        {
            var now = Now(domain);
            return AddTimer(domain, now + delayMs, 0, callbackId, group, tags);
        }

        public TimerHandle Every(long intervalMs, string callbackId, TimerDomain domain, string group = null, params string[] tags)
        {
            var now = Now(domain);
            return AddTimer(domain, now + intervalMs, intervalMs, callbackId, group, tags);
        }

        public TimerHandle AtUnixMs(long unixMs, string callbackId, string group = null, params string[] tags)
        {
            // Absolute-time timers use RealTime domain
            return AddTimer(TimerDomain.RealTime, unixMs, 0, callbackId, group, tags);
        }

        public void TickDomain(TimerDomain domain, int maxExecPerTick = 50)
        {
            var heap = GetHeap(domain);
            var now = Now(domain);

            int executed = 0;

            while (executed < maxExecPerTick && heap.Count > 0 && heap.PeekDueMs() <= now)
            {
                var t = heap.Pop();
                if (t == null) break;

                // Skip invalid states
                if (t.canceled)
                {
                    RemoveFully(t);
                    continue;
                }
                if (t.paused)
                {
                    // paused timers are not kept in heap in this implementation,
                    // but if they appear due to restore edge cases, just ignore.
                    continue;
                }

                // Execute
                if (_registry.TryResolve(t.callbackId, out var cb))
                    cb?.Invoke();

                // callback may cancel it
                if (t.canceled)
                {
                    RemoveFully(t);
                    continue;
                }

                executed++;

                if (t.repeating)
                {
                    // Catch-up: compute next due strictly in the future
                    // nextDue = due + k*interval, where k = floor((now - due)/interval)+1
                    long due = t.dueMs;
                    long interval = t.intervalMs;

                    if (interval <= 0)
                    {
                        t.canceled = true;
                        RemoveFully(t);
                        continue;
                    }

                    long k = ((now - due) / interval) + 1;
                    long nextDue = due + (k * interval);

                    t.dueMs = nextDue;
                    heap.Push(t);
                }
                else
                {
                    t.canceled = true;
                    RemoveFully(t);
                }
            }
        }

        public void Cancel(TimerHandle h)
        {
            if (!h.IsValid) return;
            if (!_byId.TryGetValue(h.Id, out var t)) return;
            t.canceled = true;

            // remove from heap immediately
            GetHeap(t.domain).Remove(t.id);
            RemoveFully(t);
        }

        public void Pause(TimerHandle h)
        {
            if (!h.IsValid) return;
            if (!_byId.TryGetValue(h.Id, out var t)) return;
            if (t.canceled || t.paused) return;

            var now = Now(t.domain);
            t.remainingMsWhenPaused = Math.Max(0, t.dueMs - now);
            t.paused = true;

            // remove from heap while paused
            GetHeap(t.domain).Remove(t.id);
        }

        public void Resume(TimerHandle h)
        {
            if (!h.IsValid) return;
            if (!_byId.TryGetValue(h.Id, out var t)) return;
            if (t.canceled || !t.paused) return;

            var now = Now(t.domain);
            t.dueMs = now + t.remainingMsWhenPaused;
            t.remainingMsWhenPaused = 0;
            t.paused = false;

            GetHeap(t.domain).Push(t);
        }

        public void CancelGroup(string group)
        {
            if (string.IsNullOrEmpty(group)) return;
            if (!_byGroup.TryGetValue(group, out var ids)) return;

            // copy to avoid modification during iteration
            var tmp = new List<int>(ids);
            foreach (var id in tmp) Cancel(new TimerHandle(id));
        }

        public void PauseGroup(string group)
        {
            if (string.IsNullOrEmpty(group)) return;
            if (!_byGroup.TryGetValue(group, out var ids)) return;

            var tmp = new List<int>(ids);
            foreach (var id in tmp) Pause(new TimerHandle(id));
        }

        public void CancelTag(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return;
            if (!_byTag.TryGetValue(tag, out var ids)) return;

            var tmp = new List<int>(ids);
            foreach (var id in tmp) Cancel(new TimerHandle(id));
        }

        public void PauseTag(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return;
            if (!_byTag.TryGetValue(tag, out var ids)) return;

            var tmp = new List<int>(ids);
            foreach (var id in tmp) Pause(new TimerHandle(id));
        }

        // ---------------- Persistence hooks ----------------
        internal IEnumerable<TimerEntry> GetAllTimers() => _byId.Values;

        internal void RestoreTimer(TimerEntry entry)
        {
            _byId[entry.id] = entry;
            _nextId = Math.Max(_nextId, entry.id + 1);

            IndexGroupTags(entry);

            if (!entry.canceled && !entry.paused)
                GetHeap(entry.domain).Push(entry);
        }

        // ---------------- Internals ----------------
        private TimerHandle AddTimer(TimerDomain domain, long dueMs, long intervalMs, string callbackId, string group, params string[] tags)
        {
            var id = _nextId++;

            var entry = new TimerEntry
            {
                id = id,
                domain = domain,
                dueMs = dueMs,
                intervalMs = intervalMs,
                callbackId = callbackId,
                group = group,
                tags = tags != null && tags.Length > 0 ? new List<string>(tags) : null
            };

            _byId[id] = entry;
            IndexGroupTags(entry);
            GetHeap(domain).Push(entry);

            return new TimerHandle(id);
        }

        private void IndexGroupTags(TimerEntry t)
        {
            if (!string.IsNullOrEmpty(t.group))
            {
                if (!_byGroup.TryGetValue(t.group, out var set))
                    _byGroup[t.group] = set = new HashSet<int>();
                set.Add(t.id);
            }

            if (t.tags != null)
            {
                for (int i = 0; i < t.tags.Count; i++)
                {
                    var tag = t.tags[i];
                    if (string.IsNullOrEmpty(tag)) continue;

                    if (!_byTag.TryGetValue(tag, out var set))
                        _byTag[tag] = set = new HashSet<int>();
                    set.Add(t.id);
                }
            }
        }

        private void DeindexGroupTags(TimerEntry t)
        {
            if (!string.IsNullOrEmpty(t.group) && _byGroup.TryGetValue(t.group, out var gset))
            {
                gset.Remove(t.id);
                if (gset.Count == 0) _byGroup.Remove(t.group);
            }

            if (t.tags != null)
            {
                for (int i = 0; i < t.tags.Count; i++)
                {
                    var tag = t.tags[i];
                    if (string.IsNullOrEmpty(tag)) continue;

                    if (_byTag.TryGetValue(tag, out var tset))
                    {
                        tset.Remove(t.id);
                        if (tset.Count == 0) _byTag.Remove(tag);
                    }
                }
            }
        }

        private void RemoveFully(TimerEntry t)
        {
            // Ensure it is not in heap
            GetHeap(t.domain).Remove(t.id);

            // Remove from indexes + id map
            DeindexGroupTags(t);
            _byId.Remove(t.id);
        }

        private IndexedMinHeap<TimerEntry> GetHeap(TimerDomain d) =>
            d == TimerDomain.RealTime ? _realHeap :
            d == TimerDomain.AppTime ? _appHeap :
            _gameHeap;

        private long Now(TimerDomain d) =>
            d == TimerDomain.RealTime ? _real.NowMs :
            d == TimerDomain.AppTime ? _app.NowMs :
            _game.NowMs;
    }
}
