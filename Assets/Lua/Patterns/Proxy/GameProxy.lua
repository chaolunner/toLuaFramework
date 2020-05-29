GameProxy = class(Proxy)

function GameProxy:OnRegister()
    self:super("OnRegister")
    self.score = 0
end

function GameProxy:OnRemove()
    self:super("OnRemove")
end

return GameProxy
