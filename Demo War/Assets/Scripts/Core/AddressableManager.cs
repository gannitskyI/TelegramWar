using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

public class AddressableManager : IInitializable
{
    public int InitializationOrder => -100; // Загружается первым

    private Dictionary<string, AsyncOperationHandle> loadedAssets = new Dictionary<string, AsyncOperationHandle>();
    private Dictionary<string, AsyncOperationHandle<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance>> loadedScenes = new Dictionary<string, AsyncOperationHandle<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance>>();

    public IEnumerator Initialize()
    {
        var initHandle = Addressables.InitializeAsync();
        yield return initHandle;
       
    }

    public IEnumerator PreloadCriticalAssets()
    {
        // Предзагрузка критических ассетов
        yield return LoadAssetAsync<GameObject>("PlayerPrefab");
        yield return LoadAssetAsync<GameObject>("GameUI");
    }

    public async Task<T> LoadAssetAsync<T>(string key) where T : UnityEngine.Object
    {
        if (string.IsNullOrEmpty(key))
        {
            Debug.LogError("Asset key is null or empty!");
            return null;
        }

        if (loadedAssets.ContainsKey(key))
        {
            var existingHandle = loadedAssets[key];
            if (existingHandle.IsValid() && existingHandle.Status == AsyncOperationStatus.Succeeded)
            {
                return existingHandle.Result as T;
            }
            else
            {
                // Удаляем невалидный handle
                loadedAssets.Remove(key);
            }
        }

        try
        {
            var handle = Addressables.LoadAssetAsync<T>(key);
            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                loadedAssets[key] = handle;
                return handle.Result;
            }
            else
            {
                Debug.LogError($"Failed to load asset: {key}, Status: {handle.Status}");
                if (handle.IsValid())
                    Addressables.Release(handle);
                return null;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Exception loading asset {key}: {e.Message}");
            return null;
        }
    }

    public async Task<GameObject> InstantiateAsync(string key, Vector3 position = default, Quaternion rotation = default)
    {
        if (string.IsNullOrEmpty(key))
        {
            Debug.LogError("Asset key is null or empty!");
            return null;
        }

        try
        {
            var handle = Addressables.InstantiateAsync(key, position, rotation);

            // Добавляем таймаут
            var timeoutTask = Task.Delay(5000); // 5 секунд таймаут
            var completedTask = await Task.WhenAny(handle.Task, timeoutTask);

            if (completedTask == timeoutTask)
            {
                Debug.LogError($"Timeout loading asset: {key}");
                if (handle.IsValid())
                    Addressables.Release(handle);
                return null;
            }

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                var instanceId = handle.Result.GetInstanceID();
                loadedAssets[instanceId.ToString()] = handle;
                return handle.Result;
            }
            else
            {
                Debug.LogError($"Failed to instantiate: {key}, Status: {handle.Status}");
                if (handle.IsValid())
                    Addressables.Release(handle);
                return null;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Exception instantiating {key}: {e.Message}");
            return null;
        }
    }

    // Метод для загрузки сцены с отдельным словарем
    public AsyncOperationHandle<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance> LoadSceneAsync(string sceneKey)
    {
        if (string.IsNullOrEmpty(sceneKey))
        {
            Debug.LogError("Scene key is null or empty!");
            return default;
        }

        if (loadedScenes.ContainsKey(sceneKey))
        {
            var existingHandle = loadedScenes[sceneKey];
            if (existingHandle.IsValid())
            {
                return existingHandle;
            }
            else
            {
                loadedScenes.Remove(sceneKey);
            }
        }

        try
        {
            var handle = Addressables.LoadSceneAsync(sceneKey, LoadSceneMode.Single);
            loadedScenes[sceneKey] = handle;
            return handle;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Exception loading scene {sceneKey}: {e.Message}");
            return default;
        }
    }

    public void ReleaseAsset(GameObject instance)
    {
        if (instance == null) return;

        var instanceId = instance.GetInstanceID().ToString();
        if (loadedAssets.ContainsKey(instanceId))
        {
            Addressables.ReleaseInstance(instance);
            loadedAssets.Remove(instanceId);
        }
    }

    public void ReleaseAsset(string key)
    {
        if (loadedAssets.TryGetValue(key, out var handle))
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
            loadedAssets.Remove(key);
            Debug.Log($"Released asset: {key}");
        }
    }

    public void ReleaseScene(string sceneKey)
    {
        if (loadedScenes.TryGetValue(sceneKey, out var sceneHandle))
        {
            if (sceneHandle.IsValid())
            {
                Addressables.UnloadSceneAsync(sceneHandle);
            }
            loadedScenes.Remove(sceneKey);
            Debug.Log($"Released scene: {sceneKey}");
        }
    }

    public void Cleanup()
    {
        Debug.Log("Cleaning up AddressableManager...");

        // Очищаем обычные ассеты
        foreach (var kvp in loadedAssets)
        {
            var handle = kvp.Value;
            if (handle.IsValid())
            {
                try
                {
                    if (handle.Result is GameObject)
                    {
                        Addressables.ReleaseInstance(handle);
                    }
                    else
                    {
                        Addressables.Release(handle);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error releasing asset {kvp.Key}: {e.Message}");
                }
            }
        }

        // Очищаем сцены
        foreach (var kvp in loadedScenes)
        {
            var sceneHandle = kvp.Value;
            if (sceneHandle.IsValid())
            {
                try
                {
                    Addressables.UnloadSceneAsync(sceneHandle);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error unloading scene {kvp.Key}: {e.Message}");
                }
            }
        }

        loadedAssets.Clear();
        loadedScenes.Clear();
        Debug.Log("AddressableManager cleanup completed");
    }

    // Метод для получения информации о загруженных ресурсах
    public string GetLoadedAssetsInfo()
    {
        var info = $"Loaded Assets ({loadedAssets.Count}):\n";
        foreach (var kvp in loadedAssets)
        {
            var status = kvp.Value.IsValid() ? kvp.Value.Status.ToString() : "Invalid";
            info += $"- {kvp.Key}: {status}\n";
        }

        info += $"Loaded Scenes ({loadedScenes.Count}):\n";
        foreach (var kvp in loadedScenes)
        {
            var status = kvp.Value.IsValid() ? kvp.Value.Status.ToString() : "Invalid";
            info += $"- {kvp.Key}: {status}\n";
        }

        return info;
    }
}