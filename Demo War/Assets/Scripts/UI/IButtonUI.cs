using UnityEngine;

public interface IButtonUI : IUIController
{
    void SetButtonInteractable(string buttonId, bool interactable);
    void RegisterButtonCallback(string buttonId, System.Action callback);
}