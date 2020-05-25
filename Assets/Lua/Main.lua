--主入口函数。从这里开始lua逻辑
function Main()
    Class = require("Class")
    Base = require("Base")
    Action = require("Action")
    Array = require("Array")
    Pool = require("Pool")
    Command = require("Patterns.Command")
    Mediator = require("Patterns.Mediator")
    Proxy = require("Patterns.Proxy")

    require("lua-protobuf.test")

    GameObject = UnityEngine.GameObject
    Transform = UnityEngine.Transform
    Random = UnityEngine.Random
    Input = UnityEngine.Input
    Camera = UnityEngine.Camera
    MaterialPropertyBlock = UnityEngine.MaterialPropertyBlock

    LuaFacade.RegisterCommand("Patterns.Command.StartUpCommand", "StartUp")
    LuaFacade.SendNotification("StartUp")

    print("logic start")
end

--场景切换通知
function OnLevelWasLoaded(level)
    collectgarbage("collect")
    Time.timeSinceLevelLoad = 0
end

function OnApplicationQuit()
end

function class(base)
    return Class.class(base)
end

function GetOrCreateComponent(go, type)
    component = go:GetComponent(type)
    if component == nil then
        component = go:AddComponent(type)
    end
    return component
end
