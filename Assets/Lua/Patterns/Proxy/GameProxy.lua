GameProxy = class(Proxy)

function GameProxy:ctor()
    self.OnGameStart = {}
end

function GameProxy:OnRegister()
    self:super("OnRegister")
    coroutine.start(self.StartGameAsync, self)
end

function GameProxy:OnRemove()
    self:super("OnRemove")
end

function GameProxy:StartGameAsync()
    local canvasHandle = LuaAddressables.LoadAssetAsync("Jump_Jump/Canvas.prefab")
    local eventSystemHandle = LuaAddressables.LoadAssetAsync("Jump_Jump/EventSystem.prefab")
    while not canvasHandle.IsDone or not eventSystemHandle.IsDone do
        coroutine.step()
    end
    self.canvas = GameObject.Instantiate(canvasHandle.Result)
    self.eventSystem = GameObject.Instantiate(eventSystemHandle.Result)

    if self.OnGameStart then
        for k, v in ipairs(self.OnGameStart) do
            v:Invoke()
        end
    end
end

return GameProxy
