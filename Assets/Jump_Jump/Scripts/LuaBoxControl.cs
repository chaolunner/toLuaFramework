using LuaInterface;
using UnityEngine;

public class LuaBoxControl : LuaControl
{
    public override void OnStart(LuaState luaState)
    {
        luaState.Require("BoxControl");
        var func = luaState.GetFunction("BoxControl.Start");
        func.Call();
        func.Dispose();
        func = null;
    }
}
