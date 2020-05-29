LoadSceneView = class(View)

local Text = require("TMPro.TMP_Text")

function LoadSceneView:ctor()
    self.isInitialized = false
end

function LoadSceneView:Initialize()
    coroutine.start(LoadSceneView.InitializeAsync, self)
end

function LoadSceneView:InitializeAsync()
    local canvasHandle = LuaAddressables.LoadAssetAsync("Internal/Prefabs/LoadSceneCanvas.prefab")
    local eventSystemHandle = LuaAddressables.LoadAssetAsync("Internal/Prefabs/EventSystem.prefab")
    while not canvasHandle.IsDone or not eventSystemHandle.IsDone do
        coroutine.step()
    end
    self.canvas = GameObject.Instantiate(canvasHandle.Result)
    self.eventSystem = GameObject.Instantiate(eventSystemHandle.Result)
    GameObject.DontDestroyOnLoad(self.canvas)
    GameObject.DontDestroyOnLoad(self.eventSystem)
    self.isInitialized = true
end

return LoadSceneView
