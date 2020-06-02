using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets;
using UnityEditor;
using UnityEngine;
using System.IO;

public class AddressablesEditor
{
    static string toLuaDir = Application.dataPath + "/Source/Lua";
    static string addressablesDir = Application.persistentDataPath + "/com.unity.addressables";

    [MenuItem("Tools/Addressables/Clean/All")]
    public static void CleanAll()
    {
        var remoteBuildPath = Application.dataPath + "/../" + AddressableAssetSettingsDefaultObject.Settings.RemoteCatalogBuildPath.GetValue(AddressableAssetSettingsDefaultObject.Settings);
        if (Directory.Exists(remoteBuildPath))
        {
            Directory.Delete(remoteBuildPath, true);
        }

        if (Directory.Exists(toLuaDir))
        {
            Directory.Delete(toLuaDir, true);
        }

        if (Directory.Exists(ProtoConst.protoDir))
        {
            Directory.Delete(ProtoConst.protoDir, true);
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
        if (Directory.Exists(addressablesDir))
        {
            Directory.Delete(addressablesDir, true);
        }

        if (Directory.Exists(ProtoConst.protoResDir))
        {
            Directory.Delete(ProtoConst.protoResDir, true);
        }

        if (Directory.Exists(LuaConst.luaResDir))
        {
            Directory.Delete(LuaConst.luaResDir, true);
        }

        Caching.ClearCache();
    }

    [MenuItem("Tools/Addressables/Build/All")]
    public static void BuildAll()
    {
        BuildLuaAndProto();
        AddressableAssetSettings.BuildPlayerContent();
    }

    [MenuItem("Tools/Addressables/Check for Content Update")]
    public static void CheckForContentUpdate()
    {
        BuildLuaAndProto();
        var buildPath = ContentUpdateScript.GetContentStateDataPath(false);
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        var entrys = ContentUpdateScript.GatherModifiedEntries(settings, buildPath);
        if (entrys.Count == 0) { return; }
        var groupName = string.Format("UpdateGroup_{0}", System.DateTime.Now.ToString("yyyyMMdd"));
        ContentUpdateScript.CreateContentUpdateGroup(settings, entrys, groupName);
    }

    [MenuItem("Tools/Addressables/Build/Content Update")]
    public static void BuildContentUpdate()
    {
        var buildPath = ContentUpdateScript.GetContentStateDataPath(false);
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        ContentUpdateScript.BuildContentUpdate(settings, buildPath);
    }

    [MenuItem("Tools/Addressables/Build/Lua + Proto")]
    public static void BuildLuaAndProto()
    {
        BuildLua();
        BuildProto();
    }

    [MenuItem("Tools/Addressables/Build/Lua Only")]
    public static void BuildLua()
    {
        if (Directory.Exists(toLuaDir))
        {
            Directory.Delete(toLuaDir, true);
        }

        Directory.CreateDirectory(toLuaDir);

        CopyLuaBytesFiles(LuaConst.luaDir, toLuaDir);
        CopyLuaBytesFiles(LuaConst.toluaDir, toLuaDir);

        AssetDatabase.Refresh();

        var toLuaGroup = FindOrCreateGroup("ToLua");

        var guid = AssetDatabase.AssetPathToGUID("Assets/Source/Lua");
        var toLuaEntry = AddressableAssetSettingsDefaultObject.Settings.CreateOrMoveEntry(guid, toLuaGroup);
        toLuaEntry.SetLabel("lua", true);
        toLuaEntry.address = "ToLua";
    }

    static AddressableAssetGroup FindOrCreateGroup(string groupName)
    {
        var group = AddressableAssetSettingsDefaultObject.Settings.FindGroup(groupName);
        if (group == null)
        {
            group = AddressableAssetSettingsDefaultObject.Settings.CreateGroup(groupName, false, false, false, null, typeof(BundledAssetGroupSchema));
            group.AddSchema<ContentUpdateGroupSchema>().StaticContent = true;
            var bundledAssetGroupSchema = group.GetSchema<BundledAssetGroupSchema>();
            bundledAssetGroupSchema.BuildPath.SetVariableByName(group.Settings, AddressableAssetSettings.kLocalBuildPath);
            bundledAssetGroupSchema.LoadPath.SetVariableByName(group.Settings, AddressableAssetSettings.kLocalLoadPath);
            AssetDatabase.Refresh();
        }
        return group;
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

    [MenuItem("Tools/Addressables/Build/Proto Only")]
    public static void BuildProto()
    {
        ProcessUtil.RunBat(ProtoConst.workingDir + "build.bat", ".bytes", ProtoConst.workingDir);

        while (!Directory.Exists(ProtoConst.protoDir))
        {
            AssetDatabase.Refresh();
        }

        var protoGroup = FindOrCreateGroup("Proto");

        var guid = AssetDatabase.AssetPathToGUID("Assets/Proto");
        var protoEntry = AddressableAssetSettingsDefaultObject.Settings.CreateOrMoveEntry(guid, protoGroup);
        protoEntry.SetLabel(ProtoConst.label, true);
        protoEntry.address = ProtoConst.address;
    }
}
