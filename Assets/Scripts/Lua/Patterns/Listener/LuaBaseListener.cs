using System.Collections.Generic;
using LuaInterface;
using UnityEngine;

[DisallowMultipleComponent]
public class LuaBaseListener : MonoBehaviour
{
    private Dictionary<LuaFunction, LuaTable> listenerMap = new Dictionary<LuaFunction, LuaTable>();

    private void OnDestroy()
    {
        RemoveAllListeners();
    }

    protected void Call()
    {
        var e = listenerMap.GetEnumerator();
        while (e.MoveNext())
        {
            var func = e.Current.Key;
            if (func == null) { continue; }
            var obj = e.Current.Value;
            func.Call(obj);
        }
    }

    protected void Call<T1>(T1 arg1)
    {
        var e = listenerMap.GetEnumerator();
        while (e.MoveNext())
        {
            var func = e.Current.Key;
            if (func == null) { continue; }
            var obj = e.Current.Value;
            func.Call(obj, arg1);
        }
    }

    protected void Call<T1, T2>(T1 arg1, T2 arg2)
    {
        var e = listenerMap.GetEnumerator();
        while (e.MoveNext())
        {
            var func = e.Current.Key;
            if (func == null) { continue; }
            var obj = e.Current.Value;
            func.Call(obj, arg1, arg2);
        }
    }

    public void AddListener(LuaFunction func, LuaTable obj)
    {
        if (listenerMap.ContainsKey(func))
        {
            listenerMap[func] = obj;
        }
        else
        {
            listenerMap.Add(func, obj);
        }
    }

    public void RemoveListener(LuaFunction func)
    {
        if (listenerMap.ContainsKey(func))
        {
            func.Dispose();
            listenerMap[func].Dispose();
            listenerMap.Remove(func);
        }
    }

    public void RemoveAllListeners()
    {
        var e = listenerMap.GetEnumerator();
        while (e.MoveNext())
        {
            e.Current.Key.Dispose();
            e.Current.Value.Dispose();
        }
        listenerMap.Clear();
    }
}
