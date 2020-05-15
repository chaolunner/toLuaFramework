using UnityEngine;

[DisallowMultipleComponent]
public class LuaCollisionEnterListener : LuaBaseListener
{
    private void OnCollisionEnter(Collision collision)
    {
        Call(collision);
    }
}
