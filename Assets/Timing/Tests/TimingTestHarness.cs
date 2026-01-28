using System;
using UnityEngine;
using Timing.Timers;
using Timing.Utils;
using Timing.Tick;
using Timing.Clock;

public sealed class TimingTestHarness : MonoBehaviour
{
    private TimerHandle _appOnce;
    private TimerHandle _gameplayRepeating;
    private TimerHandle _realAbsolute;

    private void Start()
    {
        Debug.Log("=== Timing Test Harness START ===");
        
        // 1) AppTime one-shot: should fire in ~3 seconds (even if gameplay paused)
        _appOnce = Timer.After(3.Seconds(), OnAppOnce, TimerDomain.AppTime, group: "Tests", "App");

        // 2) Gameplay repeating: should tick every 1s, affected by pause and timescale
        _gameplayRepeating = Timer.Every(1.Seconds(), OnGameplayTick, TimerDomain.GameplayTime, group: "Tests", "Gameplay", "UI");

        // 3) RealTime absolute: schedule for 10 seconds from trusted now
        var endUtc = DateTimeOffset.UtcNow.AddSeconds(10); // for test; in LiveOps use server-synced event time
        _realAbsolute = Timer.At(endUtc, OnRealAbsolute, group: "Tests", "RealTime");

        Debug.Log("Scheduled: AppTime After(3s), Gameplay Every(1s), RealTime At(+10s).");
        Debug.Log("Hotkeys: P=toggle pause, [=timescale 0.5, ]=timescale 2.0, C=cancel group Tests, T=pause tag UI, R=resume gameplay timer");
    }

    private void Update()
    {
        // Only for input in this test harness (timers themselves are NOT using Update)
        if (Input.GetKeyDown(KeyCode.P))
        {
            var ts = TickSystem.Instance;
            ts.SetPaused(!ts.Paused);
            Debug.Log($"TickSystem paused = {ts.Paused}");
        }

        if (Input.GetKeyDown(KeyCode.LeftBracket))
        {
            TickSystem.Instance.SetTimeScale(0.5f);
            Debug.Log("TimeScale = 0.5");
        }

        if (Input.GetKeyDown(KeyCode.RightBracket))
        {
            TickSystem.Instance.SetTimeScale(2.0f);
            Debug.Log("TimeScale = 2.0");
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            Timer.Scheduler.CancelGroup("Tests");
            Debug.Log("CancelGroup(Tests)");
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            Timer.Scheduler.PauseTag("UI");
            Debug.Log("PauseTag(UI)");
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            Timer.Scheduler.Resume(_gameplayRepeating);
            Debug.Log("Resume gameplay repeating timer handle");
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            // Manual pause/resume for the AppTime timer
            Timer.Scheduler.Pause(_appOnce);
            Debug.Log("Paused AppTime one-shot");
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            Timer.Scheduler.Resume(_appOnce);
            Debug.Log("Resumed AppTime one-shot");
        }
    }

    private void OnAppOnce()
    {
        Debug.Log($"[FIRE] AppTime one-shot fired at frame={Time.frameCount}");
    }

    private int _gameplayCount = 0;
    private void OnGameplayTick()
    {
        _gameplayCount++;
        Debug.Log($"[FIRE] Gameplay repeating #{_gameplayCount} at frame={Time.frameCount}");
    }

    private void OnRealAbsolute()
    {
        Debug.Log($"[FIRE] RealTime absolute fired at frame={Time.frameCount}");
    }

    private void OnApplicationQuit()
    {
        // optional: you can force-save here too if you want
        Debug.Log("=== Timing Test Harness QUIT ===");
    }
}
