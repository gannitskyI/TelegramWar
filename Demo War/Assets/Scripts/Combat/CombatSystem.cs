using System.Collections;
using UnityEngine;

public class CombatSystem : IInitializable
{
    public int InitializationOrder => 30;

    public IEnumerator Initialize()
    { 
        yield return null;
    }

    public void Cleanup() { }
}