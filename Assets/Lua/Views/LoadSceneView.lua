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
    self.animator = self.canvas:GetComponent("Animator")
    self.text = self.canvas.transform:Find("Progress/Text"):GetComponent(typeof(Text))
    GameObject.DontDestroyOnLoad(self.canvas)
    GameObject.DontDestroyOnLoad(self.eventSystem)
    self.isInitialized = true
end

function LoadSceneView:FadeOut()
    self.animator:SetInteger("State", 1)
end

function LoadSceneView:Load()
    self.animator:SetInteger("State", 2)
end

function LoadSceneView:SetProgress(progress)
    self.animator:SetFloat("Progress", progress)
    self.text.text = string.format("%.2f%%", progress * 100)
end

function LoadSceneView:FadeIn()
    self.animator:SetInteger("State", 0)
end

return LoadSceneView
