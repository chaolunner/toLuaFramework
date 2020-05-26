using System.Collections;
using UnityEngine;

public static class Main
{
    [RuntimeInitializeOnLoadMethod]
    private static void Initialize()
    {
#if UNITY_EDITOR
        UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings.DisableCatalogUpdateOnStartup = true;
#endif
        AddressablesUpdater.RequestDownloadHandle = OnRequestDownload;
        AddressablesUpdater.AfterDownloadHandle = OnAfterDownload;
        AddressablesUpdater.OnCompleted += LuaFacade.Initialize;
        new GameObject("AddressablesUpdater").AddComponent<AddressablesUpdater>();
    }

    private static RequestDownloadFeedback OnRequestDownload()
    {
        return RequestDownloadFeedback.Agree;
    }

    private static IEnumerator OnAfterDownload()
    {
        yield return CoroutineManager.DoCoroutine(LuaFacade.UpdateLocalScripts());
    }
}
