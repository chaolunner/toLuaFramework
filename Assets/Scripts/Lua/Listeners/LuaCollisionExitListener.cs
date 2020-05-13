using UnityEngine;
using System;

[DisallowMultipleComponent]
public class LuaCollisionExitListener : MonoBehaviour
{
    public event Action<Collision> OnEvent;

    private void OnCollisionExit(Collision collision)
    {
        OnEvent?.Invoke(collision);
    }
}
