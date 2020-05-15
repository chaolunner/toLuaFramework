using PureMVC.Patterns.Proxy;
using LuaInterface;

public class LuaProxy : Proxy
{
    private LuaTable proxyClass;
    private LuaTable proxyObject;

    public LuaTable Proxy
    {
        get { return proxyObject; }
    }

    public LuaProxy(string proxyName, object data = null) : base(proxyName, data)
    {
        proxyClass = LuaFacade.GetTable(proxyName);
        proxyObject = LuaFacade.New(proxyName);
    }

    public override void OnRegister()
    {
        proxyClass.Call("OnRegister", proxyObject);
    }

    public override void OnRemove()
    {
        proxyClass.Call("OnRemove", proxyObject);
        proxyClass.Dispose();
        proxyObject.Dispose();
    }
}
