using System;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public void UpdateTimer(float time)
    {
        // TODO: Implement UI update
        Debug.Log($"Time remaining: {time:F1}");
    }

    internal void ShowLevelUpScreen()
    {
        throw new NotImplementedException();
    }
}