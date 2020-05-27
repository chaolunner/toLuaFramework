BoxProxy = class(Proxy)

function BoxProxy:ctor()
    self.pool = Pool.new()
    self.currentBox = nil
    self.OnGenerateBox = {}
end

function BoxProxy:OnRegister()
    self:super("OnRegister")
    coroutine.start(self.GenerateBoxAsync, self)
end

function BoxProxy:OnRemove()
    self:super("OnRemove")
end

function BoxProxy:GenerateBoxAsync()
    local handle = LuaAddressables.LoadAssetAsync("Jump_Jump/Box.prefab")
    while not handle.IsDone do
        coroutine.step()
    end
    for i = 1, 10 do
        self.pool:Push(GameObject.Instantiate(handle.Result))
    end

    if self.OnGenerateBox then
        for k, v in ipairs(self.OnGenerateBox) do
            v:Invoke()
        end
    end
end

return BoxProxy
