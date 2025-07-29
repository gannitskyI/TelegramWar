using UnityEngine;

public interface IAnimatedUI : IUIController
{
    void PlayShowAnimation();
    void PlayHideAnimation();
    bool IsAnimating();
}