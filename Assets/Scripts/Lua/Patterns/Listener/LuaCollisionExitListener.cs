using UnityEngine;

[DisallowMultipleComponent]
public class LuaCollisionExitListener : LuaCollisionListener
{
    private void OnCollisionExit(Collision collision)
    {
        Call(collision);
    }
}
