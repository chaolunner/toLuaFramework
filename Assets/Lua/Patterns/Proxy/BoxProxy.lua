BoxProxy = class(Proxy)

function BoxProxy:ctor()
    self.pool = Pool.new()
    table.insert(self.pool.OnClear, Action.new(self, BoxProxy.OnClear))
    self.currentBox = nil
    self.boxDataMap = {}
    self.OnInitCompleted = {}
end

function BoxProxy:OnRegister()
    self:super("OnRegister")
    coroutine.start(BoxProxy.InitAsync, self)
end

function BoxProxy:OnRemove()
    self:super("OnRemove")
end

function BoxProxy:InitAsync()
    local handle = LuaAddressables.LoadAssetAsync("Jump_Jump/Box.prefab")
    while not handle.IsDone do
        coroutine.step()
    end
    for i = 1, 10 do
        self.pool:Push(GameObject.Instantiate(handle.Result))
    end

    if self.OnInitCompleted then
        for k, v in ipairs(self.OnInitCompleted) do
            v:Invoke()
        end
    end
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
