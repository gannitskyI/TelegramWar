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
        Debug.Log("SystemsInitializer: Starting initialization...");

        // Очищаем старые системы если есть
        if (systems.Count > 0)
        {
            Debug.Log("SystemsInitializer: Clearing old systems...");
            Cleanup();
            systems.Clear();
            updatableSystems.Clear();
            fixedUpdatableSystems.Clear();
        }

        // Создаем новые системы
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

        updateCoroutine = CoroutineRunner.StartRoutine(UpdateLoop());
        Debug.Log($"SystemsInitializer: All {systems.Count} systems initialized successfully");
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
                    Debug.LogError($"Ошибка обновления {system.GetType().Name}: {e.Message}");
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
                    Debug.LogError($"Ошибка фиксированного обновления {system.GetType().Name}: {e.Message}");
                }
            }

            try
            {
                stateMachine?.Update();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Ошибка обновления GameStateMachine: {e.Message}");
            }

            yield return null;
        }
    }

    public IEnumerator AddSystem(IInitializable system)
    {
        if (system == null)
        {
            Debug.LogError("Нельзя добавить null систему");
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
            Debug.LogWarning($"Система {systemType.Name} не найдена для удаления");
            return;
        }

        Debug.Log($"Удаление системы: {systemType.Name}");
        systems.Remove(system);

        if (system is IUpdatable updatable)
            updatableSystems.Remove(updatable);

        if (system is IFixedUpdatable fixedUpdatable)
            fixedUpdatableSystems.Remove(fixedUpdatable);

        system.Cleanup();
        Debug.Log($"Система удалена: {systemType.Name}");
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
                Debug.Log($"{system.GetType().Name} очищена");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Ошибка очистки {system.GetType().Name}: {e.Message}");
            }
        }

        systems.Clear();
        updatableSystems.Clear();
        fixedUpdatableSystems.Clear();
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
        yield return new WaitForSeconds(0.1f); // Небольшая пауза
        yield return InitializeAllSystems();
        Debug.Log("SystemsInitializer: Systems restarted successfully");
    }
}

public class SystemFactory
{
    public T Create<T>() where T : IInitializable, new()
    {
        return new T();
    }
}