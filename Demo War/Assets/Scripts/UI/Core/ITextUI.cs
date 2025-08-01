using UnityEngine;

public interface ITextUI : IUIController
{
    void SetText(string textId, string text);
    string GetText(string textId);
}