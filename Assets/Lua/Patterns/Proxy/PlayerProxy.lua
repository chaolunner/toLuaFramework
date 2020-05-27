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
    local handle = LuaAddressables.LoadAssetAsync("Jump_Jump/Player.prefab")
    while not handle.IsDone do
        coroutine.step()
    end
    self.player = GameObject.Instantiate(handle.Result)
    self.rigidbody = self.player:GetComponent("Rigidbody")
    self.head = self.player.transform:Find("Head")
    self.body = self.player.transform:Find("Body")

    if self.OnSpawn then
        for k, v in ipairs(self.OnSpawn) do
            v:Invoke()
        end
    end
end

return PlayerProxy
