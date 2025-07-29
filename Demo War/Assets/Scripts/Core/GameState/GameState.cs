using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

public abstract class GameState
{
    public virtual IEnumerator Enter() { yield break; }
    public virtual IEnumerator Exit() { yield break; }
    public virtual void Update() { }
    public virtual void FixedUpdate() { }
}