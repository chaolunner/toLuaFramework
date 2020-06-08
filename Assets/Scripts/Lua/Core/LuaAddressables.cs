using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

public static class LuaAddressables
{
    public static AsyncOperationHandle<object> LoadAssetAsync(object key)
    {
        return Addressables.LoadAssetAsync<object>(key);
    }

    public static void Release(AsyncOperationHandle<object> handle)
    {
        Addressables.Release(handle);
    }

    public static AsyncOperationHandle<SceneInstance> LoadSceneAsync(object key, LoadSceneMode loadMode = LoadSceneMode.Single, bool activateOnLoad = true, int priority = 100)
    {
        return Addressables.LoadSceneAsync(key, loadMode, activateOnLoad, priority);
    }

    public static AsyncOperationHandle<SceneInstance> UnloadSceneAsync(AsyncOperationHandle<SceneInstance> handle, bool autoReleaseHandle = true)
    {
        return Addressables.UnloadSceneAsync(handle, autoReleaseHandle);
    }

    public static AsyncOperationHandle<SceneInstance> UnloadSceneAsync(AsyncOperationHandle handle, bool autoReleaseHandle = true)
    {
        return Addressables.UnloadSceneAsync(handle, autoReleaseHandle);
    }

    public static AsyncOperationHandle<SceneInstance> UnloadSceneAsync(SceneInstance scene, bool autoReleaseHandle = true)
    {
        return Addressables.UnloadSceneAsync(scene, autoReleaseHandle);
    }
}
