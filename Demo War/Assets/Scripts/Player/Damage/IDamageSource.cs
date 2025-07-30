using UnityEngine;

public interface IDamageSource
{
    float GetDamage();
    DamageTeam GetTeam();
    string GetSourceName();
    GameObject GetSourceObject();
    Vector3 GetSourcePosition();
}