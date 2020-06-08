LoadSceneMediator = class(Mediator)

local LoadSceneView = require("Views.LoadSceneView")
local LoadSceneMode = require("UnityEngine.SceneManagement.LoadSceneMode")

local function onSceneLoaded(scene, mode)
    LuaFacade.SendNotification("SceneLoaded", {scene = scene, mode = mode})
end

local function onSceneUnloaded(scene)
    LuaFacade.SendNotification("SceneUnloaded", {scene})
end

function LoadSceneMediator:ListNotificationInterests()
    self:super("ListNotificationInterests")
    return {"LoadScenes", "SceneLoaded", "SceneUnloaded", "SetupScene"}
end

function LoadSceneMediator:HandleNotification(notification)
    self:super("HandleNotification")
    if notification.Name == "LoadScenes" then
        local levels = notification.Body.levels
        local assetbundle = notification.Body.assetbundle
        coroutine.start(LoadSceneMediator.LoadScenesAsync, self, levels, assetbundle)
    elseif notification.Name == "SceneLoaded" then
        local scene = notification.Body.scene
        local mode = notification.Body.mode
        if self.scenes[scene.name] then
            self.scenes[scene.name].load()
        end
    elseif notification.Name == "SceneUnloaded" then
        local scene = notification.Body[1]
        if self.scenes[scene.name] then
            self.scenes[scene.name].unload()
        end
    elseif notification.Name == "SetupScene" then
        self.scenes[notification.Body.name] = {
            load = notification.Body.load,
            unload = notification.Body.unload
        }
    end
end

function LoadSceneMediator:OnRegister()
    self:super("OnRegister")
    self.scenes = {}
    self.loadSceneView = LoadSceneView.new()
    self.loadSceneView:Initialize()
    SceneManager.sceneLoaded = SceneManager.sceneLoaded + onSceneLoaded
    SceneManager.sceneUnloaded = SceneManager.sceneUnloaded + onSceneUnloaded
end

function LoadSceneMediator:OnRemove()
    self:super("OnRemove")
    SceneManager.sceneLoaded = SceneManager.sceneLoaded - onSceneLoaded
    SceneManager.sceneUnloaded = SceneManager.sceneUnloaded - onSceneUnloaded
end

local function loadSceneAsync(levels, assetbundle)
    local handles = {}
    for k, v in ipairs(levels) do
        if assetbundle then
            if k == 1 then
                handles[k] = LuaAddressables.LoadSceneAsync(v, LoadSceneMode.Single, false)
            else
                handles[k] = LuaAddressables.LoadSceneAsync(v, LoadSceneMode.Additive, false)
            end
        else
            if k == 1 then
                handles[k] = SceneManager.LoadSceneAsync(v, LoadSceneMode.Single)
            else
                handles[k] = SceneManager.LoadSceneAsync(v, LoadSceneMode.Additive)
            end
            handles[k].allowSceneActivation = false
        end
    end
    return handles
end

local function isDone(handles, assetbundle)
    for k, v in ipairs(handles) do
        if assetbundle then
            if not v.IsDone then
                return false
            end
        else
            if not v.isDone then
                return false
            end
        end
    end
    return true
end

local function getProgress(handles, assetbundle)
    local progress = 0
    local count = 0
    for k, v in ipairs(handles) do
        if assetbundle then
            progress = progress + v.PercentComplete
        else
            progress = progress + v.progress
        end
        count = count + 1
    end
    return progress / count
end

function LoadSceneMediator:LoadScenesAsync(levels, assetbundle)
    while not self.loadSceneView.isInitialized do
        coroutine.step()
    end
    self.loadSceneView:FadeOut()
    coroutine.wait(1)
    self.loadSceneView:Load()
    local handles = loadSceneAsync(levels, assetbundle)
    local progress = 0
    while not isDone(handles, assetbundle) and progress < 0.89999 do
        progress = Mathf.Clamp(progress + Time.deltaTime, progress, getProgress(handles, assetbundle))
        self.loadSceneView:SetProgress(progress)
        coroutine.step()
    end
    if assetbundle then
        for k, v in ipairs(handles) do
            v.Result:ActivateAsync()
        end
    else
        for k, v in ipairs(handles) do
            v.allowSceneActivation = true
        end
    end
    while not isDone(handles, assetbundle) or progress < 1 do
        progress = Mathf.Clamp(progress + Time.deltaTime, progress, getProgress(handles, assetbundle))
        self.loadSceneView:SetProgress(progress)
        coroutine.step()
    end
    self.loadSceneView:FadeIn()
end

return LoadSceneMediator
