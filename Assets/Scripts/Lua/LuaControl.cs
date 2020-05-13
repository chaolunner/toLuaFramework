using LuaInterface;
using UnityEngine;

public interface ILuaControl
{
    void Start(LuaState luaState);
    void OnDestroy(LuaState luaState);
}

public class LuaControl : MonoBehaviour, ILuaControl
{
    public string ClassName;
    private LuaFunction luaFunc;

    private void Start()
    {
        LuaControlManager.AddControl(this);
    }

    private void OnDestroy()
    {
        LuaControlManager.RemoveControl(this);
    }

    public virtual void Start(LuaState luaState)
    {
        luaState.Require(ClassName);
        luaFunc = luaState.GetFunction(ClassName + ".Start");
        luaFunc.Call(this.gameObject);
        luaFunc.Dispose();
        luaFunc = null;
    }

    public virtual void OnDestroy(LuaState luaState)
    {
        luaFunc = luaState.GetFunction(ClassName + ".OnDestroy");
        luaFunc.Call(this.gameObject);
        luaFunc.Dispose();
        luaFunc = null;
    }
}
