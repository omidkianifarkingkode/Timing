using System;
using System.Diagnostics;
using UnityEngine;

namespace Timing.Clock
{
    public sealed class GameClock
    {
        private const string StorageKey = "timing.gameclock.snapshot.v1";

        private readonly ITimeStorage _storage;
        private GameClockSnapshot _snap;
        private bool _hasSnapshot;

        // Non-blocking signals
        public bool SuspectedTampering { get; private set; }
        public int TamperCount => _snap.tamperCount;

        public GameClock(ITimeStorage storage)
        {
            _storage = storage;
            Load();
            if (!_hasSnapshot)
            {
                // bootstrap with device time, but once synced, device time is only diagnostic
                var deviceMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                ResetBaseline(deviceMs);
                Persist();
            }
        }

        public DateTimeOffset TrustedUtcNow
        {
            get
            {
                var nowMs = ComputeTrustedUnixMs();
                return DateTimeOffset.FromUnixTimeMilliseconds(nowMs);
            }
        }

        public long TrustedUnixMsNow() => ComputeTrustedUnixMs();

        /// <summary>Call when you successfully fetch server UTC.</summary>
        public void SyncWithServerUnixMs(long serverUnixMs)
        {
            var deviceMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            ResetBaseline(serverUnixMs);
            _snap.deviceUnixMsAtSync = deviceMs;

            // Optional: detect large device skew for analytics (non-blocking)
            CheckForClockAnomalies(deviceMs, serverUnixMs);

            Persist();
        }

        /// <summary>Call on app resume to detect device clock jumps (non-blocking).</summary>
        public void OnAppResume()
        {
            // compute trusted “now” using monotonic
            var trustedNow = ComputeTrustedUnixMs();

            // compare to device time to detect big jumps (diagnostic only)
            var deviceMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var delta = Math.Abs(deviceMs - trustedNow);

            // threshold: choose your own policy
            if (delta > 5 * 60 * 1000) // 5 minutes
                FlagTamper("Device clock differs from trusted time on resume.");

            // additional sanity: trusted time should never go backwards across sessions
            if (trustedNow < _snap.lastKnownTrustedUnixMs)
                FlagTamper("Trusted time went backwards (snapshot anomaly).");

            _snap.lastKnownTrustedUnixMs = Math.Max(_snap.lastKnownTrustedUnixMs, trustedNow);
            Persist();
        }

        private long ComputeTrustedUnixMs()
        {
            var monoNow = Stopwatch.GetTimestamp();
            var monoDeltaTicks = monoNow - _snap.monotonicTicksAtSync;

            // convert delta ticks to ms
            var ms = (monoDeltaTicks * 1000L) / _snap.stopwatchFrequency;
            var trusted = _snap.trustedUnixMsAtSync + ms;

            // monotonic safety: if ms < 0 something is very wrong
            if (ms < 0) FlagTamper("Monotonic delta < 0.");

            return trusted;
        }

        private void ResetBaseline(long trustedUnixMs)
        {
            _snap.trustedUnixMsAtSync = trustedUnixMs;
            _snap.monotonicTicksAtSync = Stopwatch.GetTimestamp();
            _snap.stopwatchFrequency = Stopwatch.Frequency;
            _snap.lastKnownTrustedUnixMs = trustedUnixMs;
            _hasSnapshot = true;
            SuspectedTampering = false;
        }

        private void CheckForClockAnomalies(long deviceUnixMs, long trustedUnixMs)
        {
            var skew = Math.Abs(deviceUnixMs - trustedUnixMs);
            if (skew > 10 * 60 * 1000) // 10 minutes
                FlagTamper("Large device/server time skew at sync.");
        }

        private void FlagTamper(string reason)
        {
            SuspectedTampering = true;
            _snap.tamperCount++;
            // hook analytics/log if you want
            // UnityEngine.Debug.LogWarning($"[GameClock] Tamper suspected: {reason}");
        }

        private void Persist()
        {
            var json = JsonUtility.ToJson(_snap);
            _storage.Save(StorageKey, json);
        }

        private void Load()
        {
            if (_storage.TryLoad(StorageKey, out var json))
            {
                try
                {
                    _snap = JsonUtility.FromJson<GameClockSnapshot>(json);
                    _hasSnapshot = _snap.stopwatchFrequency > 0;
                }
                catch
                {
                    _hasSnapshot = false;
                }
            }
        }
    }
}
