using System.Collections;
using UnityEngine;

public class GameOverState : GameState
{
    private int finalScore;
    private GameObject gameOverUI;

    public GameOverState(int score)
    {
        finalScore = score;
    }

    public override IEnumerator Enter()
    {
        Debug.Log($"Game Over! Final score: {finalScore}");

        // Показываем UI game over
        var addressableManager = ServiceLocator.Get<AddressableManager>();
        if (addressableManager != null)
        {
            var uiTask = addressableManager.InstantiateAsync("GameOverUI");
            yield return new WaitUntil(() => uiTask.IsCompleted);
            gameOverUI = uiTask.Result;
        }

        // Отключаем игровой инпут
        if (ServiceLocator.TryGet<InputReader>(out var inputReader))
        {
            inputReader.DisableAllInput();
        }

        yield return null;
    }

    public void RestartGame()
    {
        var stateMachine = ServiceLocator.Get<GameStateMachine>();
        stateMachine?.ChangeState(new GameplayState());
    }

    public void ReturnToMenu()
    {
        var stateMachine = ServiceLocator.Get<GameStateMachine>();
        stateMachine?.ChangeState(new MainMenuState());
    }

    public override IEnumerator Exit()
    {
        if (gameOverUI != null)
        {
            Object.Destroy(gameOverUI);
        }

        yield return null;
    }
}