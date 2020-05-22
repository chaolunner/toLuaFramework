using UnityEngine.AddressableAssets;
using PureMVC.Patterns.Observer;
using PureMVC.Interfaces;
using LuaInterface;
using UnityEngine;
using System.IO;
using System.Linq;

public class LuaFacade
{
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

    [RuntimeInitializeOnLoadMethod]
    private static void Initialize()
    {
        if (!Directory.Exists(LuaConst.luaResDir))
        {
            Directory.CreateDirectory(LuaConst.luaResDir);
        }

        Addressables.LoadResourceLocationsAsync("lua", typeof(TextAsset)).Completed += handle1 =>
        {
            if (handle1.Result == null) { return; }
            int waitLoadCount = handle1.Result.Count;
            for (int i = 0; i < handle1.Result.Count; i++)
            {
                string key = handle1.Result[i].PrimaryKey;
                Addressables.LoadAssetAsync<TextAsset>(key).Completed += handle2 =>
                {
                    if (handle2.Result == null) { waitLoadCount--; return; }
                    if (key.StartsWith("ToLua"))
                    {
                        string path = string.Format("{0}/{1}", LuaConst.luaResDir, key.Substring(5, key.Length - 11));
                        string dir = Path.GetDirectoryName(path);
                        if (!Directory.Exists(dir)) { Directory.CreateDirectory(dir); }
                        File.WriteAllText(path, handle2.Result.text);
                    }
                    else
                    {
                        File.WriteAllText(string.Format("{0}/{1}", LuaConst.luaResDir, handle2.Result.name), handle2.Result.text);
                    }
                    if (--waitLoadCount <= 0)
                    {
                        var go = new GameObject("LuaClient");
                        var luaClient = go.AddComponent<LuaClient>();
                        GameObject.DontDestroyOnLoad(go);
                    }
                };
            }
        };
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

    public static void HasMediator(string mediatorName)
    {
        facade.HasMediator(mediatorName);
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
        facade.RegisterCommand(notificationName, () => { return new LuaCommand(commandName); });
    }

    public static void HasCommand(string notificationName)
    {
        facade.HasCommand(notificationName);
    }

    public static void RemoveCommand(string notificationName)
    {
        facade.RemoveCommand(notificationName);
    }

    public static void RegisterProxy(string proxyName, object data = null)
    {
        facade.RegisterProxy(new LuaProxy(proxyName, data));
    }

    public static LuaTable RetrieveProxy(string proxyName)
    {
        return (facade.RetrieveProxy(proxyName) as LuaProxy).Proxy;
    }

    public static void RemoveProxy(string proxyName)
    {
        facade.RemoveProxy(proxyName);
    }
}
