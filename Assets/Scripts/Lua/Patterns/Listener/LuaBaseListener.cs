using System.Collections.Generic;
using LuaInterface;
using UnityEngine;

[DisallowMultipleComponent]
public class LuaBaseListener : MonoBehaviour
{
    private Dictionary<LuaTable, Dictionary<LuaTable, string>> listenerMap = new Dictionary<LuaTable, Dictionary<LuaTable, string>>();

    protected void Call<T>(T arg)
    {
        var e1 = listenerMap.GetEnumerator();
        while (e1.MoveNext())
        {
            var cls = e1.Current.Key;
            if (cls == null) { continue; }
            var e2 = e1.Current.Value.GetEnumerator();
            while (e2.MoveNext())
            {
                var obj = e2.Current.Key;
                var name = e2.Current.Value;
                if (obj != null && !string.IsNullOrEmpty(name))
                {
                    cls.Call(name, obj, arg);
                }
            }
        }
    }

    public void AddListener(LuaTable cls, LuaTable obj, string name)
    {
        if (listenerMap.ContainsKey(cls))
        {
            if (!listenerMap[cls].ContainsKey(obj) && !string.IsNullOrEmpty(name))
            {
                listenerMap[cls].Add(obj, name);
            }
        }
        else
        {
            var dict = new Dictionary<LuaTable, string>();
            dict.Add(obj, name);
            listenerMap.Add(cls, dict);
        }
    }

    public void RemoveListener(LuaTable cls, LuaTable obj)
    {
        if (listenerMap.ContainsKey(cls))
        {
            if (obj == null)
            {
                listenerMap.Remove(cls);
            }
            else if (listenerMap[cls].ContainsKey(obj))
            {
                listenerMap[cls].Remove(obj);
            }
        }
    }

    public void RemoveAllListeners()
    {
        listenerMap.Clear();
    }
}
