using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.AddressableAssets;

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
}
