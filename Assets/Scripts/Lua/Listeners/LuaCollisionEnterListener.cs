using UnityEngine;
using System;

[DisallowMultipleComponent]
public class LuaCollisionEnterListener : MonoBehaviour
{
    public event Action<Collision> OnEvent;

    private void OnCollisionEnter(Collision collision)
    {
        OnEvent?.Invoke(collision);
    }
}
