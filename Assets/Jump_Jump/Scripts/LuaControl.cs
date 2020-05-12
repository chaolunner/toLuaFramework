using LuaInterface;
using UnityEngine;

public interface ILuaControl
{
    void OnStart(LuaState luaState);
}

public interface ILuaControlUpdate
{
    void OnUpdate(LuaState luaState);
}

public class LuaControl : MonoBehaviour, ILuaControl
{
    private void Start()
    {
        LuaControlManager.AddControl(this);
    }

    public virtual void OnStart(LuaState luaState) { }

    private void OnDestroy()
    {
        LuaControlManager.RemoveControl(this);
    }
}
