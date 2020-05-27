using System.Collections.Generic;
using LuaInterface;
using UnityEngine;

[DisallowMultipleComponent]
public class LuaBaseListener : MonoBehaviour
{
    private Dictionary<LuaFunction, LuaTable> listenerMap = new Dictionary<LuaFunction, LuaTable>();

    protected void Call<T>(T arg)
    {
        var e1 = listenerMap.GetEnumerator();
        while (e1.MoveNext())
        {
            var func = e1.Current.Key;
            if (func == null) { continue; }
            var obj = e1.Current.Value;
            func.Call(obj, arg);
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
            listenerMap.Remove(func);
        }
    }

    public void RemoveAllListeners()
    {
        listenerMap.Clear();
    }
}
