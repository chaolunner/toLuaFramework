using UnityEngine;

[DisallowMultipleComponent]
public class LuaCollisionStayListener : LuaBaseListener
{
    private void OnCollisionStay(Collision collision)
    {
        Call(collision);
    }
}
