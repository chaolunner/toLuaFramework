using UnityEngine;

[DisallowMultipleComponent]
public class LuaCollisionEnterListener : LuaCollisionListener
{
    private void OnCollisionEnter(Collision collision)
    {
        Call(collision);
    }
}
