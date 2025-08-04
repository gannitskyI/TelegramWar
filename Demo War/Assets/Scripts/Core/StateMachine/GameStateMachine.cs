using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

public class GameStateMachine
{
    private GameState currentState;
    private bool isTransitioning;
    private MonoBehaviour coroutineRunner;

    public void Update()
    {
        if (currentState != null && !isTransitioning)
        {
            currentState.Update();
        }
    }

    public GameStateMachine()
    {
        coroutineRunner = CoroutineRunner.Instance;
    }

    public async void ChangeState(GameState newState)
    {
        if (isTransitioning) return;
        isTransitioning = true;
        if (currentState != null)
        {
            await RunCoroutineAsync(currentState.Exit());
        }
        currentState = newState;
        if (currentState != null)
        {
            await RunCoroutineAsync(currentState.Enter());
        }
        isTransitioning = false;
    }

    private async Task RunCoroutineAsync(IEnumerator coroutine)
    {
        var tcs = new TaskCompletionSource<bool>();
        coroutineRunner.StartCoroutine(RunCoroutineWithCallback(coroutine, tcs));
        await tcs.Task;
    }

    private IEnumerator RunCoroutineWithCallback(IEnumerator coroutine, TaskCompletionSource<bool> tcs)
    {
        yield return coroutineRunner.StartCoroutine(coroutine);
        tcs.SetResult(true);
    }
}
