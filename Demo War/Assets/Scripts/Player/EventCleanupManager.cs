using System;
using System.Collections.Generic;
using UnityEngine;

public interface IEventPublisher
{
    void RegisterCleanup(Action cleanupAction);
}

public class EventCleanupManager : MonoBehaviour
{
    private static EventCleanupManager instance;
    public static EventCleanupManager Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject("[EventCleanupManager]");
                instance = go.AddComponent<EventCleanupManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    private readonly Dictionary<object, List<Action>> registeredCleanups = new Dictionary<object, List<Action>>();

    public void RegisterEventCleanup(object owner, Action cleanupAction)
    {
        if (owner == null || cleanupAction == null) return;

        if (!registeredCleanups.ContainsKey(owner))
        {
            registeredCleanups[owner] = new List<Action>();
        }

        registeredCleanups[owner].Add(cleanupAction);
    }

    public void CleanupEvents(object owner)
    {
        if (owner == null) return;

        if (registeredCleanups.TryGetValue(owner, out var cleanupActions))
        {
            foreach (var cleanup in cleanupActions)
            {
                try
                {
                    cleanup?.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error during event cleanup for {owner.GetType().Name}: {e.Message}");
                }
            }

            registeredCleanups.Remove(owner);
        }
    }

    public void CleanupAll()
    {
        foreach (var kvp in registeredCleanups)
        {
            foreach (var cleanup in kvp.Value)
            {
                try
                {
                    cleanup?.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error during cleanup: {e.Message}");
                }
            }
        }

        registeredCleanups.Clear();
    }

    private void OnDestroy()
    {
        CleanupAll();
    }
}

public static class EventCleanupExtensions
{
    public static void RegisterEventCleanup(this MonoBehaviour component, Action cleanupAction)
    {
        EventCleanupManager.Instance.RegisterEventCleanup(component, cleanupAction);
    }

    public static void CleanupEvents(this MonoBehaviour component)
    {
        EventCleanupManager.Instance.CleanupEvents(component);
    }
}