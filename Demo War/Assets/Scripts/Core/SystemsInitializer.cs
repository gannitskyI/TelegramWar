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
        // ������� ������� � ���������� �������
        var inputSystem = new InputSystem();
        var uiSystem = new UISystem();           // ��������� UI �������
        var spawnSystem = new SpawnSystem();
        var combatSystem = new CombatSystem();
        var scoreSystem = new ScoreSystem();
        var upgradeSystem = new UpgradeSystem();  // ��������� ������� ���������

        // ��������� ������� � ������
        systems.Add(inputSystem);
        systems.Add(uiSystem);                  // UI ������� ���������������� ����� Input
        systems.Add(spawnSystem);
        systems.Add(combatSystem);
        systems.Add(scoreSystem);
        systems.Add(upgradeSystem);             // ������� ��������� ����� Score

        // ��������� �� ������� �������������
        systems.Sort((a, b) => a.InitializationOrder.CompareTo(b.InitializationOrder));

       
        foreach (var system in systems)
        { 
            yield return system.Initialize();

            // ������������ updatable �������
            if (system is IUpdatable updatable)
            {
                updatableSystems.Add(updatable);
               
            }

            if (system is IFixedUpdatable fixedUpdatable)
            {
                fixedUpdatableSystems.Add(fixedUpdatable);
                
            }

            // ������������ ������� � ServiceLocator
            ServiceLocator.Register(system.GetType(), system);
            
        }

        // ��������� Update loop
        updateCoroutine = CoroutineRunner.StartRoutine(UpdateLoop());
        
    }

    private IEnumerator UpdateLoop()
    {
        // ���� ���� GameStateMachine ����� ����������������
        GameStateMachine stateMachine = null;

        while (stateMachine == null)
        {
            if (ServiceLocator.TryGet<GameStateMachine>(out stateMachine))
            { 
                break;
            }

            // ���� ���� ���� � ������� �����
            yield return null;
        }

        while (true)
        {
            float deltaTime = Time.deltaTime;

            // Update ���� updatable ������
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

            // Update �������� ��������� State Machine
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
    /// �������� ������� �� ����� runtime (��������, ��� ��������)
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
    /// ������� ������� �� ����� runtime
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
       
        // ������������� Update loop
        if (updateCoroutine != null)
        {
            CoroutineRunner.StopRoutine(updateCoroutine);
            updateCoroutine = null;
        }

        // ������� ��� �������
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

        // ������� ������
        systems.Clear();
        updatableSystems.Clear();
        fixedUpdatableSystems.Clear();
 
    }
}