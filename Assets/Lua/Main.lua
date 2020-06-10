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
    View = require("Views.View")
    pb = require("pb")

    require("lua-protobuf.test")

    GameObject = UnityEngine.GameObject
    Transform = UnityEngine.Transform
    Random = UnityEngine.Random
    Input = UnityEngine.Input
    Camera = UnityEngine.Camera
    MaterialPropertyBlock = UnityEngine.MaterialPropertyBlock
    SceneManager = UnityEngine.SceneManagement.SceneManager
    Shader = UnityEngine.Shader

    LuaFacade.RegisterCommand("Patterns.Command.StartUpCommand", "StartUp")

    print("logic start")
end

--场景切换通知
function OnLevelWasLoaded(level)
    collectgarbage("collect")
    Time.timeSinceLevelLoad = 0
end

function OnApplicationQuit()
    LuaFacade.RemoveCommand("StartUp")
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

function file_exists(path)
    local file = io.open(path, "rb")
    if file then
        file:close()
    end
    return file ~= nil
end

function load_pb(name)
    local pathMap = {UnityEngine.Application.persistentDataPath, "/Proto/", name, ".pb"}
    local path = table.concat(pathMap)
    if (file_exists(path)) then
        pb.loadfile(path)
    else
        pathMap[1] = UnityEngine.Application.dataPath
        pathMap[4] = ".pb.bytes"
        path = table.concat(pathMap)
        if (file_exists(path)) then
            pb.loadfile(path)
        end
    end
end
