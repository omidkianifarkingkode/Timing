using System;
using UnityEngine;

namespace Timing.Tick
{
    public sealed class TickSystem : MonoBehaviour
    {
        public static TickSystem Instance { get; private set; }

        public bool Paused { get; private set; }
        public float TimeScale { get; private set; } = 1f;

        public event Action<float> OnAppTick;      // unscaled dt
        public event Action<float> OnGameplayTick; // scaled dt, pauses

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SetPaused(bool paused) => Paused = paused;
        public void SetTimeScale(float scale) => TimeScale = Mathf.Max(0f, scale);

        private void Update()
        {
            var appDt = Time.unscaledDeltaTime;
            OnAppTick?.Invoke(appDt);

            var gameplayDt = Paused ? 0f : appDt * TimeScale;
            OnGameplayTick?.Invoke(gameplayDt);
        }
    }
}
