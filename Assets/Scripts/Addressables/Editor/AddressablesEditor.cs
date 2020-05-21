using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets;
using UnityEditor;
using UnityEngine;
using System.IO;

public class AddressablesEditor
{
    [MenuItem("Tools/Addressables/Clean/All")]
    public static void CleanAll()
    {
        var remoteBuildPath = Application.dataPath + "/../" + AddressableAssetSettingsDefaultObject.Settings.RemoteCatalogBuildPath.GetValue(AddressableAssetSettingsDefaultObject.Settings);
        if (Directory.Exists(remoteBuildPath))
        {
            Directory.Delete(remoteBuildPath, true);
        }

        foreach (var dataBuilder in AddressableAssetSettingsDefaultObject.Settings.DataBuilders)
        {
            AddressableAssetSettings.CleanPlayerContent(dataBuilder as IDataBuilder);
        }

        CleanCache();
    }

    [MenuItem("Tools/Addressables/Clean/Cache")]
    public static void CleanCache()
    {
        Caching.ClearCache();
    }

    [MenuItem("Tools/Addressables/Build Player Content")]
    public static void BuildPlayerContent()
    {
        AddressableAssetSettings.BuildPlayerContent();
    }
}
