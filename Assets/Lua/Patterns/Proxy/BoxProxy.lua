BoxProxy = class(Proxy)

function BoxProxy:ctor()
    self.pool = Pool.new()
    table.insert(self.pool.OnClear, Action.new(self, BoxProxy.OnClear))
    self.currentBox = nil
    self.boxDataMap = {}
    self.isInitialized = false
end

function BoxProxy:OnRegister()
    self:super("OnRegister")
    coroutine.start(BoxProxy.InitializeAsync, self)
end

function BoxProxy:OnRemove()
    self:super("OnRemove")
end

function BoxProxy:InitializeAsync()
    local handle = LuaAddressables.LoadAssetAsync("Jump_Jump/Prefabs/Box.prefab")
    while not handle.IsDone do
        coroutine.step()
    end
    for i = 1, 10 do
        self.pool:Push(GameObject.Instantiate(handle.Result))
    end
    self.isInitialized = true
end

function BoxProxy:Clear()
    self.pool:Clear()
end

function BoxProxy:OnClear(items)
    for k, v in pairs(items) do
        v:SetActive(false)
    end
end

return BoxProxy
