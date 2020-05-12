using System.Collections.Generic;
using System.Collections;
using LuaInterface;
using UnityEngine;

public class LuaControlManager : MonoBehaviour
{
    private static LuaControlManager instance;
    private LuaState luaState;
    private List<ILuaControl> controls;
    private List<ILuaControlUpdate> controlUpdates;

    public static LuaControlManager Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject("LuaControlManager");
                instance = go.AddComponent<LuaControlManager>();
            }
            return instance;
        }
    }

    public static void AddControl(object control)
    {
        if (typeof(ILuaControl).IsAssignableFrom(control.GetType()))
        {
            Instance.controls.Add(control as ILuaControl);
        }
        if (typeof(ILuaControlUpdate).IsAssignableFrom(control.GetType()))
        {
            Instance.controlUpdates.Add(control as ILuaControlUpdate);
        }
    }

    public static void RemoveControl(object control)
    {
        if (typeof(ILuaControl).IsAssignableFrom(control.GetType()))
        {
            instance.controls.Remove(control as ILuaControl);
        }
        if (typeof(ILuaControlUpdate).IsAssignableFrom(control.GetType()) && instance.controlUpdates.Contains(control as ILuaControlUpdate))
        {
            instance.controlUpdates.Remove(control as ILuaControlUpdate);
        }
    }

    void Awake()
    {
        instance = this;
        DontDestroyOnLoad(gameObject);
        controls = new List<ILuaControl>();
        controlUpdates = new List<ILuaControlUpdate>();
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
            controls[i].OnStart(luaState);
        }
    }

    private void Update()
    {
        if (!LuaInstaller.IsDone) { return; }
        for (int i = 0; i < controlUpdates.Count; i++)
        {
            controlUpdates[i].OnUpdate();
        }
    }
}
