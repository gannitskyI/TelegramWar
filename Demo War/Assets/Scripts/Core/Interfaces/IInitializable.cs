using System.Collections;
public interface IInitializable
{
    int InitializationOrder { get; }
    IEnumerator Initialize();
    void Cleanup();
}

public interface IUpdatable
{
    void OnUpdate(float deltaTime);
}

public interface IFixedUpdatable
{
    void OnFixedUpdate(float fixedDeltaTime);
}