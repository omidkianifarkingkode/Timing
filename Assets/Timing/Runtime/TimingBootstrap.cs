using UnityEngine;
using Timing.Clock;
using Timing.Tick;
using Timing.Timers;
using Timing.Timers.Persistence;

namespace Timing
{
    public sealed class TimingBootstrap : MonoBehaviour
    {
        private GameClock _clock;
        private AccumulatingDomain _app;
        private AccumulatingDomain _gameplay;

        private TimerScheduler _scheduler;
        private TimerCallbackRegistry _registry;

        private TimerPersistence _persistence;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

            // Clock
            _clock = new GameClock(new PlayerPrefsTimeStorage());

            // Tick system (ensure exists)
            if (TickSystem.Instance == null)
                new GameObject("TickSystem").AddComponent<TickSystem>();

            // Domains
            var real = new RealTimeDomain(_clock);
            _app = new AccumulatingDomain(TimerDomain.AppTime);
            _gameplay = new AccumulatingDomain(TimerDomain.GameplayTime);

            // Registry + scheduler
            _registry = new TimerCallbackRegistry();
            _scheduler = new TimerScheduler(real, _app, _gameplay, _registry);

            Timer.Initialize(_scheduler, _registry);

            // Persistence
            _persistence = new TimerPersistence(new PlayerPrefsTimerStorage());
            _persistence.LoadInto(_scheduler);

            // Connect ticks
            TickSystem.Instance.OnAppTick += OnAppTick;
            TickSystem.Instance.OnGameplayTick += OnGameplayTick;

            // On resume tamper check
            Application.focusChanged += focused =>
            {
                if (focused) _clock.OnAppResume();
            };
        }

        private void OnApplicationPause(bool pause)
        {
            if (!pause) _clock.OnAppResume();
            if (pause) _persistence.Save(_scheduler);
        }

        private void OnAppTick(float dt)
        {
            _app.Advance(dt);
            _scheduler.TickDomain(TimerDomain.AppTime);
            _scheduler.TickDomain(TimerDomain.RealTime); // RealTime can be ticked here too
        }

        private void OnGameplayTick(float dt)
        {
            _gameplay.Advance(dt);
            _scheduler.TickDomain(TimerDomain.GameplayTime);
        }
    }
}