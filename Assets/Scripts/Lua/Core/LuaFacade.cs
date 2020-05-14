using UnityEngine.AddressableAssets;
using PureMVC.Interfaces;
using LuaInterface;
using UnityEngine;
using System.IO;

public class LuaFacade
{
    private string type;

    private static IFacade Facade
    {
        get
        {
            return PureMVC.Patterns.Facade.Facade.GetInstance(() => new PureMVC.Patterns.Facade.Facade());
        }
    }

    private static LuaState LuaState
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

    public static LuaFacade DoFile(string type)
    {
        LuaState.DoFile(type);
        return new LuaFacade() { type = type };
    }

    public static LuaFacade Require(string type)
    {
        LuaState.Require(type);
        return new LuaFacade() { type = type };
    }

    public void Call(string func)
    {
        var luaFunc = LuaState.GetFunction(type + "." + func);
        luaFunc.Call();
        luaFunc.Dispose();
    }

    public void Call<T1>(string func, T1 arg1)
    {
        var luaFunc = LuaState.GetFunction(type + "." + func);
        luaFunc.Call(arg1);
        luaFunc.Dispose();
    }

    public R1 Invoke<R1>(string func)
    {
        var luaFunc = LuaState.GetFunction(type + "." + func);
        R1 ret1 = luaFunc.Invoke<R1>();
        luaFunc.Dispose();
        return ret1;
    }

    public static void RegisterMediator(string mediatorName, object viewComponent = null)
    {
        Facade.RegisterMediator(new LuaMediator(mediatorName, viewComponent));
    }

    public static LuaTable RetrieveMediator(string mediatorName)
    {
        return LuaState.GetTable(mediatorName);
    }

    public static void HasMediator(string mediatorName)
    {
        Facade.HasMediator(mediatorName);
    }

    public static void RemoveMediator(string mediatorName)
    {
        Facade.RemoveMediator(mediatorName);
    }

    public static void SendNotification(string notificationName, object body = null, string type = null)
    {
        Facade.SendNotification(notificationName, body, type);
    }

    public static void RegisterCommand(string commandName, string notificationName)
    {
        Facade.RegisterCommand(notificationName, () => { return new LuaCommand(commandName); });
    }

    public static void HasCommand(string notificationName)
    {
        Facade.HasCommand(notificationName);
    }

    public static void RemoveCommand(string notificationName)
    {
        Facade.RemoveCommand(notificationName);
    }

    public static void RegisterProxy(string proxyName, object data = null)
    {
        Facade.RegisterProxy(new LuaProxy(proxyName, data));
    }

    public static LuaTable RetrieveProxy(string proxyName)
    {
        return LuaState.GetTable(proxyName);
    }

    public static void RemoveProxy(string proxyName)
    {
        Facade.RemoveProxy(proxyName);
    }
}
