PlayerProxy = class(Proxy)

function PlayerProxy:ctor()
    self.isInitialized = false
end

function PlayerProxy:OnRegister()
    self:super("OnRegister")
    coroutine.start(PlayerProxy.InitializeAsync, self)
end

function PlayerProxy:OnRemove()
    self:super("OnRemove")
end

function PlayerProxy:InitializeAsync()
    local playerHandle = LuaAddressables.LoadAssetAsync("Jump_Jump/Prefabs/Player.prefab")
    local effectHandle = LuaAddressables.LoadAssetAsync("Jump_Jump/Prefabs/Effect.prefab")
    while not playerHandle.IsDone or not effectHandle.IsDone do
        coroutine.step()
    end
    self.player = GameObject.Instantiate(playerHandle.Result)
    self.rigidbody = self.player:GetComponent("Rigidbody")
    self.head = self.player.transform:Find("Head")
    self.body = self.player.transform:Find("Body")
    self.effect = GameObject.Instantiate(effectHandle.Result)
    self.effect.transform:SetParent(self.player.transform)
    self.effect.transform.localPosition = Vector3.zero
    self.particleSystem = self.effect:GetComponent("ParticleSystem")
    self.isInitialized = true
end

return PlayerProxy
