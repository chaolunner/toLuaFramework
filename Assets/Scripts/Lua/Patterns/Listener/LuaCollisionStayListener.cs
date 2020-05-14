using UnityEngine;

[DisallowMultipleComponent]
public class LuaCollisionStayListener : LuaCollisionListener
{
    private void OnCollisionStay(Collision collision)
    {
        Call(collision);
    }
}
