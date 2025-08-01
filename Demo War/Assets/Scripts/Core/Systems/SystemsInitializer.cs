using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class SystemsInitializer
{
    private readonly SystemFactory systemFactory = new SystemFactory();
    private readonly List<IInitializable> systems = new List<IInitializable>();
    private readonly List<IUpdatable> updatableSystems = new List<IUpdatable>();
    private readonly List<IFixedUpdatable> fixedUpdatableSystems = new List<IFixedUpdatable>();
    private Coroutine updateCoroutine;

    public IEnumerator InitializeAllSystems()
    {
        Debug.Log("SystemsInitializer: Starting initialization...");

        if (systems.Count > 0)
        {
            Debug.Log("SystemsInitializer: Clearing old systems...");
            Cleanup();
            systems.Clear();
            updatableSystems.Clear();
            fixedUpdatableSystems.Clear();
        }

        systems.Add(systemFactory.Create<InputSystem>());
        systems.Add(systemFactory.Create<UISystem>());
        systems.Add(systemFactory.Create<SpawnSystem>());
        systems.Add(systemFactory.Create<CombatSystem>());
        systems.Add(systemFactory.Create<ScoreSystem>());
        systems.Add(systemFactory.Create<UpgradeSystem>());

        systems.Sort((a, b) => a.InitializationOrder.CompareTo(b.InitializationOrder));

        foreach (var system in systems)
        {
            Debug.Log($"SystemsInitializer: Initializing {system.GetType().Name}...");
            yield return system.Initialize();

            if (system is IUpdatable updatable)
                updatableSystems.Add(updatable);

            if (system is IFixedUpdatable fixedUpdatable)
                fixedUpdatableSystems.Add(fixedUpdatable);

            ServiceLocator.Register(system.GetType(), system);
            Debug.Log($"SystemsInitializer: {system.GetType().Name} initialized and registered");
        }

        yield return WaitForUpgradeSystemReady();

        yield return InitializeSpriteCache();

        updateCoroutine = CoroutineRunner.StartRoutine(UpdateLoop());
        Debug.Log($"SystemsInitializer: All {systems.Count} systems initialized successfully");
    }

    private IEnumerator WaitForUpgradeSystemReady()
    {
        var upgradeSystem = systems.OfType<UpgradeSystem>().FirstOrDefault();
        if (upgradeSystem != null)
        {
            Debug.Log("SystemsInitializer: Waiting for UpgradeSystem database to load...");

            var timeout = 0f;
            var maxTimeout = 10f;

            while (!upgradeSystem.IsDatabaseLoaded() && timeout < maxTimeout)
            {
                timeout += 0.1f;
                yield return new WaitForSeconds(0.1f);
            }

            if (timeout >= maxTimeout)
            {
                Debug.LogWarning("SystemsInitializer: UpgradeSystem database load timeout");
            }
            else
            {
                Debug.Log("SystemsInitializer: UpgradeSystem database loaded successfully");
            }
        }
    }

    private IEnumerator InitializeSpriteCache()
    {
        Debug.Log("SystemsInitializer: Initializing SpriteCache...");
        SpriteCache.Initialize();
        yield return null;
    }

    private IEnumerator UpdateLoop()
    {
        GameStateMachine stateMachine = null;
        while (stateMachine == null)
        {
            if (ServiceLocator.TryGet(out stateMachine))
                break;
            yield return null;
        }

        while (true)
        {
            float deltaTime = Time.deltaTime;

            foreach (var system in updatableSystems)
            {
                try
                {
                    system.OnUpdate(deltaTime);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error updating {system.GetType().Name}: {e.Message}");
                }
            }

            foreach (var system in fixedUpdatableSystems)
            {
                try
                {
                    system.OnFixedUpdate(deltaTime);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error in fixed update {system.GetType().Name}: {e.Message}");
                }
            }

            try
            {
                stateMachine?.Update();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error updating GameStateMachine: {e.Message}");
            }

            yield return null;
        }
    }

    public IEnumerator AddSystem(IInitializable system)
    {
        if (system == null)
        {
            Debug.LogError("Cannot add null system");
            yield break;
        }

        yield return system.Initialize();
        systems.Add(system);

        if (system is IUpdatable updatable)
            updatableSystems.Add(updatable);

        if (system is IFixedUpdatable fixedUpdatable)
            fixedUpdatableSystems.Add(fixedUpdatable);

        ServiceLocator.Register(system.GetType(), system);
    }

    public void RemoveSystem<T>() where T : IInitializable
    {
        var systemType = typeof(T);
        var system = systems.Find(s => s.GetType() == systemType);
        if (system == null)
        {
            Debug.LogWarning($"System {systemType.Name} not found for removal");
            return;
        }

        Debug.Log($"Removing system: {systemType.Name}");
        systems.Remove(system);

        if (system is IUpdatable updatable)
            updatableSystems.Remove(updatable);

        if (system is IFixedUpdatable fixedUpdatable)
            fixedUpdatableSystems.Remove(fixedUpdatable);

        system.Cleanup();
        Debug.Log($"System removed: {systemType.Name}");
    }

    public void Cleanup()
    {
        Debug.Log("SystemsInitializer: Starting cleanup...");

        if (updateCoroutine != null)
        {
            CoroutineRunner.StopRoutine(updateCoroutine);
            updateCoroutine = null;
        }

        foreach (var system in systems)
        {
            try
            {
                system.Cleanup();
                Debug.Log($"{system.GetType().Name} cleaned up");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error cleaning up {system.GetType().Name}: {e.Message}");
            }
        }

        systems.Clear();
        updatableSystems.Clear();
        fixedUpdatableSystems.Clear();

        SpriteCache.Cleanup();

        Debug.Log("SystemsInitializer: Cleanup complete");
    }

    public void RestartSystems()
    {
        Debug.Log("SystemsInitializer: Restarting all systems...");
        CoroutineRunner.StartRoutine(RestartSystemsCoroutine());
    }

    private IEnumerator RestartSystemsCoroutine()
    {
        Cleanup();
        yield return new WaitForSeconds(0.1f);

        AddressableUpgradeLoader.Instance.ClearCache();

        yield return InitializeAllSystems();
        Debug.Log("SystemsInitializer: Systems restarted successfully");
    }

    public bool AreAllSystemsInitialized()
    {
        if (systems.Count == 0) return false;

        foreach (var system in systems)
        {
            if (system is UpgradeSystem upgradeSystem && !upgradeSystem.IsDatabaseLoaded())
            {
                return false;
            }
        }

        return true;
    }

    public T GetSystem<T>() where T : class, IInitializable
    {
        return systems.OfType<T>().FirstOrDefault();
    }

    public string GetSystemsStatus()
    {
        var status = $"Systems Status ({systems.Count} total):\n";

        foreach (var system in systems.OrderBy(s => s.InitializationOrder))
        {
            var systemName = system.GetType().Name;
            var isReady = "Ready";

            if (system is UpgradeSystem upgradeSystem && !upgradeSystem.IsDatabaseLoaded())
            {
                isReady = "Database Loading...";
            }

            status += $"- {systemName}: {isReady}\n";
        }

        status += $"Updatable Systems: {updatableSystems.Count}\n";
        status += $"Fixed Updatable Systems: {fixedUpdatableSystems.Count}\n";
        status += $"Update Loop Active: {updateCoroutine != null}";

        return status;
    }
}

public class SystemFactory
{
    public T Create<T>() where T : IInitializable, new()
    {
        return new T();
    }
}