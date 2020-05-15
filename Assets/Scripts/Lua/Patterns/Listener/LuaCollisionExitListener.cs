using UnityEngine;

[DisallowMultipleComponent]
public class LuaCollisionExitListener : LuaBaseListener
{
    private void OnCollisionExit(Collision collision)
    {
        Call(collision);
    }
}
