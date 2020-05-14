using PureMVC.Patterns.Proxy;

public class LuaProxy : Proxy
{
    public LuaProxy(string proxyName, object data = null) : base(proxyName, data)
    {
    }

    public override void OnRegister()
    {
        LuaFacade.Require(ProxyName).Call("OnRegister");
    }

    public override void OnRemove()
    {
        LuaFacade.Require(ProxyName).Call("OnRemove");
    }
}
