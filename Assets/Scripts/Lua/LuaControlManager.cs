using System.Collections.Generic;
using System.Collections;
using LuaInterface;
using UnityEngine;

public class LuaControlManager : MonoBehaviour
{
    private static LuaControlManager instance;
    private static bool isDisposed = false;
    private LuaState luaState;
    private List<ILuaControl> controls;

    public static LuaControlManager Instance
    {
        get
        {
            if (instance == null && !isDisposed)
            {
                var go = new GameObject("LuaControlManager");
                instance = go.AddComponent<LuaControlManager>();
            }
            return instance;
        }
    }

    public static void AddControl(ILuaControl control)
    {
        if (!isDisposed && !Instance.controls.Contains(control))
        {
            if (Instance.luaState != null)
            {
                control.Start(Instance.luaState);
            }
            Instance.controls.Add(control);
        }
    }

    public static void RemoveControl(ILuaControl control)
    {
        if (!isDisposed && Instance.controls.Contains(control))
        {
            if (Instance.luaState != null)
            {
                control.OnDestroy(Instance.luaState);
            }
            Instance.controls.Remove(control);
        }
    }

    private void Awake()
    {
        instance = this;
        DontDestroyOnLoad(gameObject);
        controls = new List<ILuaControl>();
    }

    private IEnumerator Start()
    {
        while (!LuaInstaller.IsDone)
        {
            yield return null;
        }
        luaState = LuaClient.GetMainState();
        for (int i = 0; i < controls.Count; i++)
        {
            controls[i].Start(luaState);
        }
    }

    private void OnDestroy()
    {
        Destroy();
    }

    private void OnApplicationQuit()
    {
        Destroy();
    }

    private void Destroy()
    {
        while (controls != null && controls.Count > 0)
        {
            RemoveControl(controls[0]);
        }
        luaState = null;
        instance = null;
        isDisposed = true;
    }
}
