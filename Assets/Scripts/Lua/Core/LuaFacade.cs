using UnityEngine.AddressableAssets;
using PureMVC.Patterns.Observer;
using PureMVC.Interfaces;
using LuaInterface;
using UnityEngine;
using System.IO;

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
        };
    }

    public static void Require(string type)
    {
        luaState.Require(type);
    }

    public static LuaTable GetTable(string type)
    {
        Require(type);
        return luaState.GetTable(type);
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
