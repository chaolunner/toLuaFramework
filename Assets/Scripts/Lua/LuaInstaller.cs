using UnityEngine.AddressableAssets;
using UnityEngine;
using System.IO;

public class LuaInstaller
{
    public static bool IsDone;

    [RuntimeInitializeOnLoadMethod]
    private static void Initialize()
    {
        Addressables.LoadAssetsAsync<TextAsset>("lua", null).Completed += handle =>
        {
            if (handle.Result == null) { return; }

            if (!Directory.Exists(LuaConst.luaResDir))
            {
                Directory.CreateDirectory(LuaConst.luaResDir);
            }

            for (int i = 0; i < handle.Result.Count; i++)
            {
                File.WriteAllText(string.Format("{0}/{1}", LuaConst.luaResDir, handle.Result[i].name), handle.Result[i].text);
            }

            var go = new GameObject("LuaClient");
            var luaClient = go.AddComponent<LuaClient>();
            GameObject.DontDestroyOnLoad(go);

            IsDone = true;
        };
    }
}
