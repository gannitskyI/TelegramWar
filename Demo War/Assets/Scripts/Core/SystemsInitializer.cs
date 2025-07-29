using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SystemsInitializer
{
    private List<IInitializable> systems = new List<IInitializable>();
    private List<IUpdatable> updatableSystems = new List<IUpdatable>();
    private List<IFixedUpdatable> fixedUpdatableSystems = new List<IFixedUpdatable>();
    private Coroutine updateCoroutine;

    public IEnumerator InitializeAllSystems()
    { 
        // Создаем системы в правильном порядке
        var inputSystem = new InputSystem();
        var uiSystem = new UISystem();           // Добавляем UI систему
        var spawnSystem = new SpawnSystem();
        var combatSystem = new CombatSystem();
        var scoreSystem = new ScoreSystem();
        var upgradeSystem = new UpgradeSystem();  // Добавляем систему улучшений

        // Добавляем системы в список
        systems.Add(inputSystem);
        systems.Add(uiSystem);                  // UI система инициализируется после Input
        systems.Add(spawnSystem);
        systems.Add(combatSystem);
        systems.Add(scoreSystem);
        systems.Add(upgradeSystem);             // Система улучшений после Score

        // Сортируем по порядку инициализации
        systems.Sort((a, b) => a.InitializationOrder.CompareTo(b.InitializationOrder));

       
        foreach (var system in systems)
        { 
            yield return system.Initialize();

            // Регистрируем updatable системы
            if (system is IUpdatable updatable)
            {
                updatableSystems.Add(updatable);
               
            }

            if (system is IFixedUpdatable fixedUpdatable)
            {
                fixedUpdatableSystems.Add(fixedUpdatable);
                
            }

            // Регистрируем системы в ServiceLocator
            ServiceLocator.Register(system.GetType(), system);
            
        }

        // Запускаем Update loop
        updateCoroutine = CoroutineRunner.StartRoutine(UpdateLoop());
        
    }

    private IEnumerator UpdateLoop()
    {
        // Ждем пока GameStateMachine будет зарегистрирована
        GameStateMachine stateMachine = null;

        while (stateMachine == null)
        {
            if (ServiceLocator.TryGet<GameStateMachine>(out stateMachine))
            { 
                break;
            }

            // Ждем один кадр и пробуем снова
            yield return null;
        }

        while (true)
        {
            float deltaTime = Time.deltaTime;

            // Update всех updatable систем
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

            // Update текущего состояния State Machine
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

    /// <summary>
    /// Добавить систему во время runtime (например, для плагинов)
    /// </summary>
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
        {
            updatableSystems.Add(updatable);
        }

        if (system is IFixedUpdatable fixedUpdatable)
        {
            fixedUpdatableSystems.Add(fixedUpdatable);
        }

        ServiceLocator.Register(system.GetType(), system);
 
    }

    /// <summary>
    /// Удалить систему во время runtime
    /// </summary>
    public void RemoveSystem<T>() where T : IInitializable
    {
        var systemType = typeof(T);
        var system = systems.Find(s => s.GetType() == systemType);

        if (system != null)
        {
            Debug.Log($"Removing system: {systemType.Name}");

            systems.Remove(system);

            if (system is IUpdatable updatable)
            {
                updatableSystems.Remove(updatable);
            }

            if (system is IFixedUpdatable fixedUpdatable)
            {
                fixedUpdatableSystems.Remove(fixedUpdatable);
            }

            system.Cleanup();

            Debug.Log($"System removed: {systemType.Name}");
        }
        else
        {
            Debug.LogWarning($"System not found for removal: {systemType.Name}");
        }
    }
 
    public void Cleanup()
    {
       
        // Останавливаем Update loop
        if (updateCoroutine != null)
        {
            CoroutineRunner.StopRoutine(updateCoroutine);
            updateCoroutine = null;
        }

        // Очищаем все системы
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

        // Очищаем списки
        systems.Clear();
        updatableSystems.Clear();
        fixedUpdatableSystems.Clear();
 
    }
}