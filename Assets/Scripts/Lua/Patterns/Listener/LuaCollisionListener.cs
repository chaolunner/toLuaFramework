using UnityEngine;
using System;

[DisallowMultipleComponent]
public class LuaCollisionListener : MonoBehaviour
{
    private event Action<Collision> events;

    protected void Call(Collision collision)
    {
        events?.Invoke(collision);
    }

    public void AddListener(Action<Collision> call)
    {
        events += call;
    }

    public void RemoveListener(Action<Collision> call)
    {
        events -= call;
    }

    public void RemoveAllListeners()
    {
        if (events == null) return;
        var delegates = events.GetInvocationList();
        for (int i = 0; i < delegates.Length; i++)
        {
            events -= delegates[i] as Action<Collision>;
        }
    }
}
