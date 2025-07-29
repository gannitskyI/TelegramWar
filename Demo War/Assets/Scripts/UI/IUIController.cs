using UnityEngine;

public interface IUIController
{
    void Show();
    void Hide();
    void Update(float deltaTime);
    void Cleanup();
    bool IsVisible();
    void SetUIParent(Transform parentTransform); // Опциональный метод
}