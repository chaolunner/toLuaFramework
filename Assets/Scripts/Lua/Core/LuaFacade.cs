using UnityEngine.AddressableAssets;
using System.Collections.Generic;
using PureMVC.Patterns.Observer;
using System.Collections;
using PureMVC.Interfaces;
using LuaInterface;
using UnityEngine;
using System.IO;

public class LuaFacade
{
    private static Dictionary<string, LuaCommand> commandMap = new Dictionary<string, LuaCommand>();
    private const int UseExistingBuild = 2;

    private static IFacade facade
    {
        get
        {
            return PureMVC.Patterns.Facade.Facade.GetInstance(() => new PureMVC.Patterns.Facade.Facade());
        }
    }

    private static LuaState luaState
    {
        get
        {
            return LuaClient.GetMainState();
        }
    }

    public static IEnumerator UpdateLocalScripts(bool forceUpdate = false)
    {
        if (!Directory.Exists(LuaConst.luaResDir))
        {
            Directory.CreateDirectory(LuaConst.luaResDir);
        }

        var resHandle = Addressables.LoadResourceLocationsAsync("lua", typeof(TextAsset));
        yield return resHandle;
        for (int i = 0; i < resHandle.Result.Count; i++)
        {
            string key = resHandle.Result[i].PrimaryKey;
            if (!key.StartsWith("ToLua")) { continue; }
            string path = string.Format("{0}/{1}", LuaConst.luaResDir, key.Substring(5, key.Length - 11));
            if (!forceUpdate && File.Exists(path))
            {
                var downloadSizeHandle = Addressables.GetDownloadSizeAsync(key);
                yield return downloadSizeHandle;
                if (downloadSizeHandle.Result == 0) { Addressables.Release(downloadSizeHandle); continue; }
            }
            var loadHandle = Addressables.LoadAssetAsync<TextAsset>(key);
            yield return loadHandle;
            string dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir)) { Directory.CreateDirectory(dir); }
            File.WriteAllText(path, loadHandle.Result.text);
            Addressables.Release(loadHandle);
        }
        Addressables.Release(resHandle);
    }

    public static IEnumerator UpdateLocalProtos(bool forceUpdate = false)
    {
        if (!Directory.Exists(ProtoConst.protoResDir))
        {
            Directory.CreateDirectory(ProtoConst.protoResDir);
        }

        var resHandle = Addressables.LoadResourceLocationsAsync(ProtoConst.label, typeof(TextAsset));
        yield return resHandle;
        for (int i = 0; i < resHandle.Result.Count; i++)
        {
            string key = resHandle.Result[i].PrimaryKey;
            if (!key.StartsWith(ProtoConst.address)) { continue; }
            string path = string.Format("{0}/{1}", ProtoConst.protoResDir, key.Substring(5, key.Length - 11));
            if (!forceUpdate && File.Exists(path))
            {
                var downloadSizeHandle = Addressables.GetDownloadSizeAsync(key);
                yield return downloadSizeHandle;
                if (downloadSizeHandle.Result == 0) { Addressables.Release(downloadSizeHandle); continue; }
            }
            var loadHandle = Addressables.LoadAssetAsync<TextAsset>(key);
            yield return loadHandle;
            string dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir)) { Directory.CreateDirectory(dir); }
            File.WriteAllBytes(path, loadHandle.Result.bytes);
            Addressables.Release(loadHandle);
        }
        Addressables.Release(resHandle);
    }

    public static void Initialize()
    {
#if UNITY_EDITOR
        if (UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings.ActivePlayModeDataBuilderIndex == UseExistingBuild && !Directory.Exists(LuaConst.luaResDir)) { return; }
#else
        if (!Directory.Exists(LuaConst.luaResDir)) { return; }
#endif
        var go = new GameObject("LuaClient");
        go.AddComponent<LuaClient>();
        GameObject.DontDestroyOnLoad(go);
    }

    public static void Require(string type)
    {
        luaState.Require(type);
    }

    public static LuaTable GetTable(string type)
    {
        Require(type);
        int startIndex = type.LastIndexOf('.') + 1;
        var name = type.Substring(startIndex, type.Length - startIndex);
        return luaState.GetTable(name);
    }

    public static LuaTable New(string type)
    {
        return GetTable(type).Invoke<LuaTable>("new");
    }

    public static void RegisterMediator(string mediatorName, object viewComponent = null)
    {
        facade.RegisterMediator(new LuaMediator(mediatorName, viewComponent));
    }

    public static LuaTable RetrieveMediator(string mediatorName)
    {
        return (facade.RetrieveMediator(mediatorName) as LuaMediator).Mediator;
    }

    public static bool HasMediator(string mediatorName)
    {
        return facade.HasMediator(mediatorName);
    }

    public static void RemoveMediator(string mediatorName)
    {
        facade.RemoveMediator(mediatorName);
    }

    public static void SendNotification(string notificationName, LuaTable body = null, string type = null)
    {
        facade.NotifyObservers(new Notification(notificationName, body, type));
    }

    public static void RegisterCommand(string commandName, string notificationName)
    {
        if (!HasCommand(notificationName))
        {
            var cmd = new LuaCommand(commandName);
            commandMap.Add(notificationName, cmd);
            facade.RegisterCommand(notificationName, () => { return cmd; });
        }
    }

    public static bool HasCommand(string notificationName)
    {
        return facade.HasCommand(notificationName);
    }

    public static void RemoveCommand(string notificationName)
    {
        if (HasCommand(notificationName))
        {
            facade.RemoveCommand(notificationName);
            commandMap[notificationName].OnRemove();
            commandMap.Remove(notificationName);
        }
    }

    public static void RegisterProxy(string proxyName, object data = null)
    {
        facade.RegisterProxy(new LuaProxy(proxyName, data));
    }

    public static LuaTable RetrieveProxy(string proxyName)
    {
        return (facade.RetrieveProxy(proxyName) as LuaProxy).Proxy;
    }

    public static bool HasProxy(string proxyName)
    {
        return facade.HasProxy(proxyName);
    }

    public static void RemoveProxy(string proxyName)
    {
        facade.RemoveProxy(proxyName);
    }
}
