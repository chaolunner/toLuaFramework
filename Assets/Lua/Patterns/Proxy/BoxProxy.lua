BoxProxy = class(Proxy)

function BoxProxy:ctor()
    self.pool = Pool.new()
    self.currentBox = nil
    self.OnGenerateBox = {}
end

function BoxProxy:OnRegister()
    self:super("OnRegister")
    LuaAddressables.LoadGameObjectAsync("Jump_Jump/Box", BoxProxy, self, "GenerateBox")
end

function BoxProxy:OnRemove()
    self:super("OnRemove")
end

function BoxProxy:GenerateBox(prefab)
    for i = 1, 10 do
        self.pool:Push(GameObject.Instantiate(prefab))
    end

    if self.OnGenerateBox then
        for k, v in ipairs(self.OnGenerateBox) do
            v:Invoke()
        end
    end
end

return BoxProxy
