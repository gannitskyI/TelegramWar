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
        if (string.IsNullOrEmpty(key))
        {
            Debug.LogError("���� ������ null ��� ������");
            return null;
        }

        if (loadedAssets.TryGetValue(key, out var existingHandle) && existingHandle.IsValid() && existingHandle.Status == AsyncOperationStatus.Succeeded)
            return existingHandle.Result as T;

        loadedAssets.Remove(key);
        var handle = Addressables.LoadAssetAsync<T>(key);

        try
        {
            await handle.Task;
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                loadedAssets[key] = handle;
                return handle.Result;
            }

            Debug.LogError($"�� ������� ��������� �����: {key}, ������: {handle.Status}");
            if (handle.IsValid())
                Addressables.Release(handle);
            return null;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"���������� ��� �������� ������ {key}: {e.Message}, ������: {handle.Status}");
            return null;
        }
    }

    public async Task<GameObject> InstantiateAsync(string key, Vector3 position = default, Quaternion rotation = default)
    {
        if (string.IsNullOrEmpty(key))
        {
            Debug.LogError("���� ������ null ��� ������");
            return null;
        }

        var handle = Addressables.InstantiateAsync(key, position, rotation);
        var timeoutTask = Task.Delay(5000);
        var completedTask = await Task.WhenAny(handle.Task, timeoutTask);

        if (completedTask == timeoutTask)
        {
            Debug.LogError($"������� �������� ������: {key}");
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

        Debug.LogError($"�� ������� ������� ���������: {key}, ������: {handle.Status}");
        if (handle.IsValid())
            Addressables.Release(handle);
        return null;
    }

    public AsyncOperationHandle<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance> LoadSceneAsync(string sceneKey)
    {
        if (string.IsNullOrEmpty(sceneKey))
        {
            Debug.LogError("���� ����� null ��� ������");
            return default;
        }

        if (loadedScenes.TryGetValue(sceneKey, out var existingHandle) && existingHandle.IsValid())
            return existingHandle;

        loadedScenes.Remove(sceneKey);
        var handle = Addressables.LoadSceneAsync(sceneKey, LoadSceneMode.Single);
        loadedScenes[sceneKey] = handle;
        return handle;
    }

    public void ReleaseAsset(GameObject instance)
    {
        if (instance == null)
            return;

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
            Debug.Log($"���������� �����: {key}");
        }
    }

    public void ReleaseScene(string sceneKey)
    {
        if (loadedScenes.TryGetValue(sceneKey, out var sceneHandle) && sceneHandle.IsValid())
        {
            Addressables.UnloadSceneAsync(sceneHandle);
            loadedScenes.Remove(sceneKey);
            Debug.Log($"����������� �����: {sceneKey}");
        }
    }

    public void Cleanup()
    {
        Debug.Log("������� AddressableManager...");
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
                catch (System.Exception e)
                {
                    Debug.LogError($"������ ������������ ������ {kvp.Key}: {e.Message}");
                }
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
                catch (System.Exception e)
                {
                    Debug.LogError($"������ �������� ����� {kvp.Key}: {e.Message}");
                }
            }
        }

        loadedAssets.Clear();
        loadedScenes.Clear();
        Debug.Log("������� AddressableManager ���������");
    }

    public string GetLoadedAssetsInfo()
    {
        var info = $"����������� ������ ({loadedAssets.Count}):\n";
        foreach (var kvp in loadedAssets)
            info += $"- {kvp.Key}: {(kvp.Value.IsValid() ? kvp.Value.Status.ToString() : "��������������")}\n";

        info += $"����������� ����� ({loadedScenes.Count}):\n";
        foreach (var kvp in loadedScenes)
            info += $"- {kvp.Key}: {(kvp.Value.IsValid() ? kvp.Value.Status.ToString() : "��������������")}\n";

        return info;
    }
}