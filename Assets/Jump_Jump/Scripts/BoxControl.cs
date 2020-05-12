using System.Collections;
using LuaInterface;
using UnityEngine;

public class BoxControl : MonoBehaviour
{
    private LuaState luaState;

    IEnumerator Start()
    {
        while (!LuaInstaller.IsDone)
        {
            yield return null;
        }
        luaState = LuaClient.GetMainState();
        luaState.Require("BoxControl");
        LuaFunction func = luaState.GetFunction("BoxControl.Start");
        func.Call();
        func.Dispose();
        func = null;
    }
}
