PlayerProxy = class(Proxy)

function PlayerProxy:ctor()
    self.OnSpawn = {}
end

function PlayerProxy:OnRegister()
    self:super("OnRegister")
    coroutine.start(self.SpawnAsync, self)
end

function PlayerProxy:OnRemove()
    self:super("OnRemove")
end

function PlayerProxy:SpawnAsync()
    local playerHandle = LuaAddressables.LoadAssetAsync("Jump_Jump/Player.prefab")
    local effectHandle = LuaAddressables.LoadAssetAsync("Jump_Jump/ChargeEffect.prefab")
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

    if self.OnSpawn then
        for k, v in ipairs(self.OnSpawn) do
            v:Invoke()
        end
    end
end

return PlayerProxy
