using LuaInterface;
using UnityEngine;

public class LuaPlayerControl : LuaControl
{
    public override void OnStart(LuaState luaState)
    {
        luaState.Require("PlayerControl");
        var func = luaState.GetFunction("PlayerControl.Start");
        func.Call();
        func.Dispose();
        func = null;
    }
}
