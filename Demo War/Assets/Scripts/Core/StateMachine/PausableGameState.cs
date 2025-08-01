using System.Collections;
using UnityEngine;

public abstract class PausableGameState : GameState
{
    protected bool isPaused = false;
    protected bool wasInitialized = false;

    public virtual void Pause()
    {
        if (isPaused) return;

        isPaused = true;
        OnPause();
        Debug.Log($"{GetType().Name}: Game paused");
    }

    public virtual void Resume()
    {
        if (!isPaused) return;

        isPaused = false;
        OnResume();
        Debug.Log($"{GetType().Name}: Game resumed");
    }

    protected virtual void OnPause()
    {
        if (ServiceLocator.TryGet<SpawnSystem>(out var spawnSystem))
        {
            spawnSystem.StopSpawning();
        }

        if (ServiceLocator.TryGet<InputReader>(out var inputReader))
        {
            inputReader.DisableAllInput();
        }

        Time.timeScale = 0f;
    }

    protected virtual void OnResume()
    {
        if (ServiceLocator.TryGet<SpawnSystem>(out var spawnSystem))
        {
            spawnSystem.StartSpawning();
        }

        if (ServiceLocator.TryGet<InputReader>(out var inputReader))
        {
            inputReader.EnableGameplayInput();
        }

        Time.timeScale = 1f;
    }

    public override void Update()
    {
        if (!isPaused)
        {
            OnUpdate();
        }
    }

    protected virtual void OnUpdate() { }

    public bool IsPaused() => isPaused;
    public bool IsInitialized() => wasInitialized;
}