﻿//this source code was auto-generated by tolua#, do not modify it
using System;
using LuaInterface;

public class LuaAddressablesWrap
{
	public static void Register(LuaState L)
	{
		L.BeginStaticLibs("LuaAddressables");
		L.RegFunction("LoadAssetAsync", LoadAssetAsync);
		L.RegFunction("Release", Release);
		L.RegFunction("LoadSceneAsync", LoadSceneAsync);
		L.RegFunction("UnloadSceneAsync", UnloadSceneAsync);
		L.EndStaticLibs();
	}

	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	static int LoadAssetAsync(IntPtr L)
	{
		try
		{
			ToLua.CheckArgsCount(L, 1);
			object arg0 = ToLua.ToVarObject(L, 1);
			UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object> o = LuaAddressables.LoadAssetAsync(arg0);
			ToLua.PushValue(L, o);
			return 1;
		}
		catch (Exception e)
		{
			return LuaDLL.toluaL_exception(L, e);
		}
	}

	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	static int Release(IntPtr L)
	{
		try
		{
			ToLua.CheckArgsCount(L, 1);
			UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object> arg0 = StackTraits<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<object>>.Check(L, 1);
			LuaAddressables.Release(arg0);
			return 0;
		}
		catch (Exception e)
		{
			return LuaDLL.toluaL_exception(L, e);
		}
	}

	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	static int LoadSceneAsync(IntPtr L)
	{
		try
		{
			int count = LuaDLL.lua_gettop(L);

			if (count == 1)
			{
				object arg0 = ToLua.ToVarObject(L, 1);
				UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance> o = LuaAddressables.LoadSceneAsync(arg0);
				ToLua.PushValue(L, o);
				return 1;
			}
			else if (count == 2)
			{
				object arg0 = ToLua.ToVarObject(L, 1);
				UnityEngine.SceneManagement.LoadSceneMode arg1 = (UnityEngine.SceneManagement.LoadSceneMode)ToLua.CheckObject(L, 2, typeof(UnityEngine.SceneManagement.LoadSceneMode));
				UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance> o = LuaAddressables.LoadSceneAsync(arg0, arg1);
				ToLua.PushValue(L, o);
				return 1;
			}
			else if (count == 3)
			{
				object arg0 = ToLua.ToVarObject(L, 1);
				UnityEngine.SceneManagement.LoadSceneMode arg1 = (UnityEngine.SceneManagement.LoadSceneMode)ToLua.CheckObject(L, 2, typeof(UnityEngine.SceneManagement.LoadSceneMode));
				bool arg2 = LuaDLL.luaL_checkboolean(L, 3);
				UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance> o = LuaAddressables.LoadSceneAsync(arg0, arg1, arg2);
				ToLua.PushValue(L, o);
				return 1;
			}
			else if (count == 4)
			{
				object arg0 = ToLua.ToVarObject(L, 1);
				UnityEngine.SceneManagement.LoadSceneMode arg1 = (UnityEngine.SceneManagement.LoadSceneMode)ToLua.CheckObject(L, 2, typeof(UnityEngine.SceneManagement.LoadSceneMode));
				bool arg2 = LuaDLL.luaL_checkboolean(L, 3);
				int arg3 = (int)LuaDLL.luaL_checknumber(L, 4);
				UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance> o = LuaAddressables.LoadSceneAsync(arg0, arg1, arg2, arg3);
				ToLua.PushValue(L, o);
				return 1;
			}
			else
			{
				return LuaDLL.luaL_throw(L, "invalid arguments to method: LuaAddressables.LoadSceneAsync");
			}
		}
		catch (Exception e)
		{
			return LuaDLL.toluaL_exception(L, e);
		}
	}

	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	static int UnloadSceneAsync(IntPtr L)
	{
		try
		{
			int count = LuaDLL.lua_gettop(L);

			if (count == 1 && TypeChecker.CheckTypes<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance>>(L, 1))
			{
				UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance> arg0 = StackTraits<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance>>.To(L, 1);
				UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance> o = LuaAddressables.UnloadSceneAsync(arg0);
				ToLua.PushValue(L, o);
				return 1;
			}
			else if (count == 1 && TypeChecker.CheckTypes<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>(L, 1))
			{
				UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle arg0 = StackTraits<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>.To(L, 1);
				UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance> o = LuaAddressables.UnloadSceneAsync(arg0);
				ToLua.PushValue(L, o);
				return 1;
			}
			else if (count == 1 && TypeChecker.CheckTypes<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance>(L, 1))
			{
				UnityEngine.ResourceManagement.ResourceProviders.SceneInstance arg0 = StackTraits<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance>.To(L, 1);
				UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance> o = LuaAddressables.UnloadSceneAsync(arg0);
				ToLua.PushValue(L, o);
				return 1;
			}
			else if (count == 2 && TypeChecker.CheckTypes<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance>, bool>(L, 1))
			{
				UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance> arg0 = StackTraits<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance>>.To(L, 1);
				bool arg1 = LuaDLL.lua_toboolean(L, 2);
				UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance> o = LuaAddressables.UnloadSceneAsync(arg0, arg1);
				ToLua.PushValue(L, o);
				return 1;
			}
			else if (count == 2 && TypeChecker.CheckTypes<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle, bool>(L, 1))
			{
				UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle arg0 = StackTraits<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>.To(L, 1);
				bool arg1 = LuaDLL.lua_toboolean(L, 2);
				UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance> o = LuaAddressables.UnloadSceneAsync(arg0, arg1);
				ToLua.PushValue(L, o);
				return 1;
			}
			else if (count == 2 && TypeChecker.CheckTypes<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance, bool>(L, 1))
			{
				UnityEngine.ResourceManagement.ResourceProviders.SceneInstance arg0 = StackTraits<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance>.To(L, 1);
				bool arg1 = LuaDLL.lua_toboolean(L, 2);
				UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance> o = LuaAddressables.UnloadSceneAsync(arg0, arg1);
				ToLua.PushValue(L, o);
				return 1;
			}
			else
			{
				return LuaDLL.luaL_throw(L, "invalid arguments to method: LuaAddressables.UnloadSceneAsync");
			}
		}
		catch (Exception e)
		{
			return LuaDLL.toluaL_exception(L, e);
		}
	}
}

