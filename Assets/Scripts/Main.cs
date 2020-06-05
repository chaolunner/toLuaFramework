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
        CoroutineManager.DoCoroutine(InitializeAsync());
    }

    private static IEnumerator InitializeAsync()
    {
        yield return CoroutineManager.DoCoroutine(LuaFacade.UpdateLocalScripts());
        yield return CoroutineManager.DoCoroutine(LuaFacade.UpdateLocalProtos());
        LuaFacade.Initialize();
        AddressablesUpdater.RequestDownloadHandle = OnRequestDownload;
        AddressablesUpdater.OnDownload += OnDownload;
        AddressablesUpdater.AfterDownloadHandle = OnAfterDownload;
        AddressablesUpdater.OnCompleted += OnCompleted;
        new GameObject("AddressablesUpdater").AddComponent<AddressablesUpdater>();
    }

    private static IEnumerator OnRequestDownload(long downloadSize)
    {
        hotUpdateClass = LuaFacade.GetTable("HotUpdate");
        hotUpdateObject = hotUpdateClass.Invoke<LuaTable>("new");
        hotUpdateClass.Call("Initialize", hotUpdateObject, downloadSize);
        yield return null;
        while (!hotUpdateClass.Invoke<LuaTable, bool>("Response", hotUpdateObject))
        {
            yield return null;
        }
        if (hotUpdateClass.Invoke<LuaTable, bool>("Result", hotUpdateObject))
        {
            AddressablesUpdater.Result = RequestDownloadResult.Agree;
        }
        else
        {
            AddressablesUpdater.Result = RequestDownloadResult.Disagree;
        }
    }

    private static IEnumerator OnAfterDownload()
    {
        yield return CoroutineManager.DoCoroutine(LuaFacade.UpdateLocalScripts(true));
        yield return CoroutineManager.DoCoroutine(LuaFacade.UpdateLocalProtos(true));
    }

    private static void OnDownload(long downloadedSize, long downloadSize)
    {
        if (hotUpdateClass == null || hotUpdateObject == null) { return; }
        hotUpdateClass.Call("Download", hotUpdateObject, downloadedSize, downloadSize);
    }

    private static void OnCompleted()
    {
        if (hotUpdateClass != null)
        {
            if (hotUpdateObject != null)
            {
                hotUpdateClass.Call("OnDestroy", hotUpdateObject);
                hotUpdateObject.Dispose();
                hotUpdateObject = null;
            }
            hotUpdateClass.Dispose();
            hotUpdateClass = null;
            GameObject.DestroyImmediate(LuaClient.Instance.gameObject);
            LuaFacade.Initialize();
        }
        LuaFacade.SendNotification("StartUp");
    }
}
