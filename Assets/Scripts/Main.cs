using System.Collections;
using LuaInterface;
using UnityEngine;

public static class Main
{
    private static LuaTable hotUpdateClass;
    private static LuaTable hotUpdateObject;

    [RuntimeInitializeOnLoadMethod]
    private static void Initialize()
    {
#if UNITY_EDITOR
        UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings.DisableCatalogUpdateOnStartup = true;
#endif
        LuaFacade.Initialize();
        AddressablesUpdater.RequestDownloadHandle = OnRequestDownload;
        AddressablesUpdater.OnDownload += OnDownload;
        AddressablesUpdater.AfterDownloadHandle = OnAfterDownload;
        AddressablesUpdater.OnCompleted += OnCompleted;
        new GameObject("AddressablesUpdater").AddComponent<AddressablesUpdater>();
    }

    private static IEnumerator OnRequestDownload(long downloadSize)
    {
        if (LuaClient.Instance)
        {
            yield return CoroutineManager.DoCoroutine(LuaFacade.UpdateLocalScripts());
            hotUpdateClass = LuaFacade.GetTable("HotUpdate");
            hotUpdateObject = hotUpdateClass.Invoke<LuaTable>("new");
            hotUpdateClass.Call("Initialize", hotUpdateObject, downloadSize);
            yield return null;
            while (hotUpdateClass.Invoke<LuaTable, long, bool>("Request", hotUpdateObject, downloadSize))
            {
                yield return null;
            }
            AddressablesUpdater.Result = hotUpdateClass.Invoke<LuaTable, bool>("Result", hotUpdateObject) ? RequestDownloadResult.Agree : RequestDownloadResult.Disagree;
        }
        else
        {
            AddressablesUpdater.Result = RequestDownloadResult.Agree;
        }
    }

    private static IEnumerator OnAfterDownload()
    {
        yield return CoroutineManager.DoCoroutine(LuaFacade.UpdateLocalScripts(true));
        yield return CoroutineManager.DoCoroutine(LuaFacade.UpdateLocalProtos());
    }

    private static void OnDownload(long downloadedSize, long downloadSize)
    {
        if (LuaClient.Instance)
        {
            hotUpdateClass.Call("Download", hotUpdateObject, downloadedSize, downloadSize);
        }
    }

    private static void OnCompleted()
    {
        if (LuaClient.Instance)
        {
            GameObject.DestroyImmediate(LuaClient.Instance.gameObject);
        }
        if (hotUpdateClass != null)
        {
            hotUpdateClass.Dispose();
            hotUpdateClass = null;
        }
        if (hotUpdateObject != null)
        {
            hotUpdateObject.Dispose();
            hotUpdateObject = null;
        }
        LuaFacade.Initialize();
        LuaFacade.SendNotification("StartUp");
    }
}
