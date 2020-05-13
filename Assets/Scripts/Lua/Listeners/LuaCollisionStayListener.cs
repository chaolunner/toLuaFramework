using UnityEngine;
using System;

[DisallowMultipleComponent]
public class LuaCollisionStayListener : MonoBehaviour
{
    public event Action<Collision> OnEvent;

    private void OnCollisionStay(Collision collision)
    {
        OnEvent?.Invoke(collision);
    }
}
