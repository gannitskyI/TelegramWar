using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;

public class AddressableManager : IInitializable
{
    public int InitializationOrder => -100;

    private readonly Dictionary<string, AsyncOperationHandle> loadedAssets = new Dictionary<string, AsyncOperationHandle>();
    private readonly Dictionary<string, AsyncOperationHandle<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance>> loadedScenes = new Dictionary<string, AsyncOperationHandle<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance>>();

    public IEnumerator Initialize()
    {
        var initHandle = Addressables.InitializeAsync();
        yield return initHandle;
    }

    public IEnumerator PreloadCriticalAssets()
    {
        yield return LoadAssetAsync<GameObject>("PlayerPrefab");
        yield return LoadAssetAsync<GameObject>("GameUI");
    }

    public async Task<T> LoadAssetAsync<T>(string key) where T : UnityEngine.Object
    {
        if (string.IsNullOrEmpty(key)) return null;
        if (loadedAssets.TryGetValue(key, out var existingHandle) && existingHandle.IsValid() && existingHandle.Status == AsyncOperationStatus.Succeeded)
            return existingHandle.Result as T;

        loadedAssets.Remove(key);
        var handle = Addressables.LoadAssetAsync<T>(key);

        await handle.Task;
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            loadedAssets[key] = handle;
            return handle.Result;
        }
        if (handle.IsValid())
            Addressables.Release(handle);
        return null;
    }

    public async Task<GameObject> InstantiateAsync(string key, Vector3 position = default, Quaternion rotation = default)
    {
        if (string.IsNullOrEmpty(key)) return null;
        var handle = Addressables.InstantiateAsync(key, position, rotation);
        var timeoutTask = Task.Delay(5000);
        var completedTask = await Task.WhenAny(handle.Task, timeoutTask);

        if (completedTask == timeoutTask)
        {
            if (handle.IsValid())
                Addressables.Release(handle);
            return null;
        }

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            var instanceId = handle.Result.GetInstanceID().ToString();
            loadedAssets[instanceId] = handle;
            return handle.Result;
        }

        if (handle.IsValid())
            Addressables.Release(handle);
        return null;
    }

    public AsyncOperationHandle<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance> LoadSceneAsync(string sceneKey)
    {
        if (string.IsNullOrEmpty(sceneKey)) return default;
        if (loadedScenes.TryGetValue(sceneKey, out var existingHandle) && existingHandle.IsValid())
            return existingHandle;
        loadedScenes.Remove(sceneKey);
        var handle = Addressables.LoadSceneAsync(sceneKey, LoadSceneMode.Single);
        loadedScenes[sceneKey] = handle;
        return handle;
    }

    public void ReleaseAsset(GameObject instance)
    {
        if (instance == null) return;
        var instanceId = instance.GetInstanceID().ToString();
        if (loadedAssets.TryGetValue(instanceId, out var handle) && handle.IsValid())
        {
            Addressables.ReleaseInstance(handle);
            loadedAssets.Remove(instanceId);
        }
    }

    public void ReleaseAsset(string key)
    {
        if (loadedAssets.TryGetValue(key, out var handle) && handle.IsValid())
        {
            Addressables.Release(handle);
            loadedAssets.Remove(key);
        }
    }

    public void ReleaseScene(string sceneKey)
    {
        if (loadedScenes.TryGetValue(sceneKey, out var sceneHandle) && sceneHandle.IsValid())
        {
            Addressables.UnloadSceneAsync(sceneHandle);
            loadedScenes.Remove(sceneKey);
        }
    }

    public void Cleanup()
    {
        foreach (var kvp in loadedAssets)
        {
            if (kvp.Value.IsValid())
            {
                try
                {
                    if (kvp.Value.Result is GameObject)
                        Addressables.ReleaseInstance(kvp.Value);
                    else
                        Addressables.Release(kvp.Value);
                }
                catch { }
            }
        }
        foreach (var kvp in loadedScenes)
        {
            if (kvp.Value.IsValid())
            {
                try
                {
                    Addressables.UnloadSceneAsync(kvp.Value);
                }
                catch { }
            }
        }
        loadedAssets.Clear();
        loadedScenes.Clear();
    }
}
