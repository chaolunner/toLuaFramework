using UnityEngine.AddressableAssets;
using LuaInterface;
using UnityEngine;

public static class LuaAddressables
{
    public static void LoadGameObjectAsync(object key, LuaTable cls, LuaTable obj, string name)
    {
        Addressables.LoadAssetAsync<GameObject>(key).Completed += handle =>
        {
            if (handle.Result == null) { return; }
            cls.Call(name, obj, handle.Result);
        };
    }
}
