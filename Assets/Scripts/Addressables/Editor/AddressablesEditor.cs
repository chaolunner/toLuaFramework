using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets;
using UnityEditor;
using UnityEngine;
using System.IO;

public class AddressablesEditor
{
    static string toLuaDir = Application.dataPath + "/Temp/ToLua";

    [MenuItem("Tools/Addressables/Clean/All")]
    public static void CleanAll()
    {
        var remoteBuildPath = Application.dataPath + "/../" + AddressableAssetSettingsDefaultObject.Settings.RemoteCatalogBuildPath.GetValue(AddressableAssetSettingsDefaultObject.Settings);
        if (Directory.Exists(remoteBuildPath))
        {
            Directory.Delete(remoteBuildPath, true);
        }

        if (Directory.Exists(LuaConst.luaResDir))
        {
            Directory.Delete(LuaConst.luaResDir, true);
        }

        if (Directory.Exists(toLuaDir))
        {
            Directory.Delete(toLuaDir, true);
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
        BuildToLua();
        AddressableAssetSettings.BuildPlayerContent();
    }

    [MenuItem("Tools/Addressables/Build ToLua")]
    public static void BuildToLua()
    {
        if (Directory.Exists(toLuaDir))
        {
            Directory.Delete(toLuaDir, true);
        }

        Directory.CreateDirectory(toLuaDir);

        CopyLuaBytesFiles(LuaConst.luaDir, toLuaDir);
        CopyLuaBytesFiles(LuaConst.toluaDir, toLuaDir);

        AssetDatabase.Refresh();

        var toLuaGroup = AddressableAssetSettingsDefaultObject.Settings.FindGroup("ToLua");
        if (toLuaGroup == null)
        {
            toLuaGroup = AddressableAssetSettingsDefaultObject.Settings.CreateGroup("ToLua", false, false, false, null, typeof(BundledAssetGroupSchema));
            toLuaGroup.AddSchema<ContentUpdateGroupSchema>().StaticContent = true;
            var bundledAssetGroupSchema = toLuaGroup.GetSchema<BundledAssetGroupSchema>();
            bundledAssetGroupSchema.BuildPath.SetVariableByName(toLuaGroup.Settings, AddressableAssetSettings.kLocalBuildPath);
            bundledAssetGroupSchema.LoadPath.SetVariableByName(toLuaGroup.Settings, AddressableAssetSettings.kLocalLoadPath);
            AssetDatabase.Refresh();
        }

        var guid = AssetDatabase.AssetPathToGUID("Assets/Temp/ToLua");
        var toLuaEntry = AddressableAssetSettingsDefaultObject.Settings.CreateOrMoveEntry(guid, toLuaGroup);
        toLuaEntry.SetLabel("lua", true);
        toLuaEntry.address = "ToLua";
    }

    static void CopyLuaBytesFiles(string sourceDir, string destDir, bool appendext = true, string searchPattern = "*.lua", SearchOption option = SearchOption.AllDirectories)
    {
        if (!Directory.Exists(sourceDir))
        {
            return;
        }

        string[] files = Directory.GetFiles(sourceDir, searchPattern, option);
        int len = sourceDir.Length;

        if (sourceDir[len - 1] == '/' || sourceDir[len - 1] == '\\')
        {
            --len;
        }

        for (int i = 0; i < files.Length; i++)
        {
            string str = files[i].Remove(0, len);
            string dest = destDir + "/" + str;
            if (appendext) dest += ".bytes";
            string dir = Path.GetDirectoryName(dest);
            Directory.CreateDirectory(dir);
            File.Copy(files[i], dest, true);
        }
    }
}
