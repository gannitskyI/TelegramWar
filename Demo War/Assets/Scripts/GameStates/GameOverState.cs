using System.Collections;
using UnityEngine;

public class GameOverState : GameState
{
    private const string GAME_OVER_UI_ID = "GameOver";
    private int finalScore;
    private GameOverUIController gameOverUIController;

    public GameOverState(int score)
    {
        finalScore = score;
    }

    public override IEnumerator Enter()
    {
        Debug.Log($"Game Over! Final score: {finalScore}");

        Time.timeScale = 0f;

        StopAllGameSystems();
        ShowGameOverUI();

        yield return null;
    }

    private void StopAllGameSystems()
    {
        if (ServiceLocator.TryGet<SpawnSystem>(out var spawnSystem))
        {
            spawnSystem.StopSpawning();
        }

        if (ServiceLocator.TryGet<InputReader>(out var inputReader))
        {
            inputReader.DisableAllInput();
        }

        ClearExistingEnemies();
    }

    private void ClearExistingEnemies()
    {
        var enemies = Object.FindObjectsOfType<EnemyBehaviour>();
        foreach (var enemy in enemies)
        {
            if (enemy != null)
            {
                Object.Destroy(enemy.gameObject);
            }
        }

        var projectiles = Object.FindObjectsOfType<EnemyProjectile>();
        foreach (var projectile in projectiles)
        {
            if (projectile != null)
            {
                Object.Destroy(projectile.gameObject);
            }
        }

        var experienceParticles = Object.FindObjectsOfType<ExperienceParticle>();
        foreach (var particle in experienceParticles)
        {
            if (particle != null)
            {
                Object.Destroy(particle.gameObject);
            }
        }

        if (enemies.Length > 0 || projectiles.Length > 0 || experienceParticles.Length > 0)
        {
            Debug.Log($"Cleared {enemies.Length} enemies, {projectiles.Length} projectiles, {experienceParticles.Length} exp particles");
        }
    }

    private void ShowGameOverUI()
    {
        var uiSystem = ServiceLocator.Get<UISystem>();
        if (uiSystem == null)
        {
            Debug.LogError("UISystem not found! Cannot show game over screen.");
            return;
        }

        if (uiSystem.IsUIActive("GameUI"))
        {
            uiSystem.HideUI("GameUI");
        }

        gameOverUIController = new GameOverUIController(finalScore);
        uiSystem.RegisterUIController(GAME_OVER_UI_ID, gameOverUIController);
        uiSystem.ShowUI(GAME_OVER_UI_ID);

        Debug.Log("Game Over UI displayed");
    }

    public override void Update()
    {
    }

    public override IEnumerator Exit()
    {
        Debug.Log("Exiting Game Over state");

        var uiSystem = ServiceLocator.Get<UISystem>();
        if (uiSystem != null)
        {
            uiSystem.HideUI(GAME_OVER_UI_ID);
            uiSystem.UnregisterUIController(GAME_OVER_UI_ID);
        }

        gameOverUIController = null;

        Time.timeScale = 1f;

        yield return null;
    }
}