using UnityEngine.AddressableAssets;
using UnityEngine;
using System;

public static class LuaAddressables
{
    public static void LoadGameObjectAsync(object key, Action<GameObject> action)
    {
        Addressables.LoadAssetAsync<GameObject>(key).Completed += handle =>
        {
            if (handle.Result == null) { return; }
            action?.Invoke(handle.Result);
        };
    }
}
