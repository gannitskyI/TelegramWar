using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SystemsInitializer
{
    private readonly SystemFactory systemFactory = new SystemFactory();
    private readonly List<IInitializable> systems = new List<IInitializable>();
    private readonly List<IUpdatable> updatableSystems = new List<IUpdatable>();
    private readonly List<IFixedUpdatable> fixedUpdatableSystems = new List<IFixedUpdatable>();
    private Coroutine updateCoroutine;

    public IEnumerator InitializeAllSystems()
    {
        Cleanup();
        systems.Clear();
        updatableSystems.Clear();
        fixedUpdatableSystems.Clear();

        systems.Add(systemFactory.Create<InputSystem>());
        systems.Add(systemFactory.Create<UISystem>());
        systems.Add(systemFactory.Create<SpawnSystem>());
        systems.Add(systemFactory.Create<CombatSystem>());
        systems.Add(systemFactory.Create<ScoreSystem>());
        systems.Add(systemFactory.Create<UpgradeSystem>());

        systems.Sort((a, b) => a.InitializationOrder.CompareTo(b.InitializationOrder));

        for (int i = 0; i < systems.Count; i++)
        {
            yield return systems[i].Initialize();

            if (systems[i] is IUpdatable updatable)
                updatableSystems.Add(updatable);

            if (systems[i] is IFixedUpdatable fixedUpdatable)
                fixedUpdatableSystems.Add(fixedUpdatable);

            ServiceLocator.Register(systems[i].GetType(), systems[i]);
        }

        yield return WaitForUpgradeSystemReady();
        yield return InitializeSpriteCache();

        updateCoroutine = CoroutineRunner.StartRoutine(UpdateLoop());
    }

    private IEnumerator WaitForUpgradeSystemReady()
    {
        UpgradeSystem upgradeSystem = null;
        for (int i = 0; i < systems.Count; i++)
        {
            if (systems[i] is UpgradeSystem us)
            {
                upgradeSystem = us;
                break;
            }
        }
        if (upgradeSystem != null)
        {
            float timeout = 0f;
            const float maxTimeout = 10f;
            while (!upgradeSystem.IsDatabaseLoaded() && timeout < maxTimeout)
            {
                timeout += 0.1f;
                yield return new WaitForSeconds(0.1f);
            }
        }
    }

    private IEnumerator InitializeSpriteCache()
    {
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

            for (int i = 0; i < updatableSystems.Count; i++)
            {
                updatableSystems[i].OnUpdate(deltaTime);
            }
            for (int i = 0; i < fixedUpdatableSystems.Count; i++)
            {
                fixedUpdatableSystems[i].OnFixedUpdate(deltaTime);
            }
            stateMachine?.Update();
            yield return null;
        }
    }

    public IEnumerator AddSystem(IInitializable system)
    {
        if (system == null)
            yield break;

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
        IInitializable foundSystem = null;
        for (int i = 0; i < systems.Count; i++)
        {
            if (systems[i].GetType() == systemType)
            {
                foundSystem = systems[i];
                break;
            }
        }
        if (foundSystem == null) return;
        systems.Remove(foundSystem);

        if (foundSystem is IUpdatable updatable)
            updatableSystems.Remove(updatable);

        if (foundSystem is IFixedUpdatable fixedUpdatable)
            fixedUpdatableSystems.Remove(fixedUpdatable);

        foundSystem.Cleanup();
    }

    public void Cleanup()
    {
        if (updateCoroutine != null)
        {
            CoroutineRunner.StopRoutine(updateCoroutine);
            updateCoroutine = null;
        }
        for (int i = 0; i < systems.Count; i++)
        {
            systems[i].Cleanup();
        }
        systems.Clear();
        updatableSystems.Clear();
        fixedUpdatableSystems.Clear();
        SpriteCache.Cleanup();
    }

    public void RestartSystems()
    {
        CoroutineRunner.StartRoutine(RestartSystemsCoroutine());
    }

    private IEnumerator RestartSystemsCoroutine()
    {
        Cleanup();
        yield return new WaitForSeconds(0.1f);
        AddressableUpgradeLoader.Instance.ClearCache();
        yield return InitializeAllSystems();
    }

    public bool AreAllSystemsInitialized()
    {
        if (systems.Count == 0) return false;
        for (int i = 0; i < systems.Count; i++)
        {
            if (systems[i] is UpgradeSystem upgradeSystem && !upgradeSystem.IsDatabaseLoaded())
                return false;
        }
        return true;
    }

    public T GetSystem<T>() where T : class, IInitializable
    {
        for (int i = 0; i < systems.Count; i++)
        {
            if (systems[i] is T match)
                return match;
        }
        return null;
    }

    public string GetSystemsStatus()
    {
        var status = $"Systems Status ({systems.Count} total):\n";
        for (int i = 0; i < systems.Count; i++)
        {
            var system = systems[i];
            var systemName = system.GetType().Name;
            var isReady = "Ready";
            if (system is UpgradeSystem upgradeSystem && !upgradeSystem.IsDatabaseLoaded())
                isReady = "Database Loading...";
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
