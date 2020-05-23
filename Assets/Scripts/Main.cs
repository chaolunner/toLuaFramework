using UnityEngine;

public static class Main
{
    [RuntimeInitializeOnLoadMethod]
    private static void Initialize()
    {
#if UNITY_EDITOR
        UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings.DisableCatalogUpdateOnStartup = true;
#endif
        AddressablesUpdater.OnCompleted += LuaFacade.Initialize;
        new GameObject("AddressablesUpdater").AddComponent<AddressablesUpdater>();
    }
}
