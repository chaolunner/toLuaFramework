PlayerMediator = class(Mediator)

require("LuaCollisionEnterListener")
local KeyCode = UnityEngine.KeyCode

function PlayerMediator:ListNotificationInterests()
    self:super("ListNotificationInterests")
    return {}
end

function PlayerMediator:HandleNotification(notification)
    self:super("HandleNotification")
end

function PlayerMediator:OnRegister()
    self:super("OnRegister")
    LuaFacade.RegisterProxy("Patterns.Proxy.PlayerProxy")
    self.playerProxy = LuaFacade.RetrieveProxy("Patterns.Proxy.PlayerProxy")
    table.insert(self.playerProxy.OnSpawn, Action.new(self, PlayerMediator.Spawn))
end

function PlayerMediator:OnRemove()
    self:super("OnRemove")
end

function PlayerMediator:Spawn()
    self.playerProxy.player.transform.position = Vector3(0, 0.25, 0)
    GetOrCreateComponent(self.playerProxy.player, typeof(LuaCollisionEnterListener)):AddListener(
        PlayerMediator,
        self,
        "OnCollisionEnter"
    )
    self.OnUpdate = UpdateBeat:CreateListener(PlayerMediator.Update, self)
    UpdateBeat:AddListener(self.OnUpdate)
    LuaFacade.SendNotification("PlayerSpawn", {player = self.playerProxy.player})
end

function PlayerMediator:Update()
    if Input.GetKeyDown(KeyCode.Space) then
        self.startTime = Time.time
    end
    if Input.GetKeyUp(KeyCode.Space) then
        --DoTween恢复人物
        self.playerProxy.body.transform:DOScale(0.1, 0.5)
        self.playerProxy.head.transform:DOLocalMoveY(0.27, 0.5)
        self:StartJump(Time.time - self.startTime)
    end
    if Input.GetKey(KeyCode.Space) then
        --人物压缩效果
        if self.playerProxy.body.localScale.y < 0.11 and self.playerProxy.body.localScale.y > 0.05 then
            self.playerProxy.body.localScale =
                self.playerProxy.body.localScale + Vector3(1, -1, 1) * 0.05 * Time.deltaTime
            self.playerProxy.head.localPosition =
                self.playerProxy.head.localPosition + Vector3(0, -1, 0) * 0.05 * Time.deltaTime
        end
    end
end

function PlayerMediator:StartJump(time)
    --跳跃逻辑，这里的time可以理解为我们按下按钮的时间
    self.playerProxy.rigidbody:AddForce(Vector3(1, 1, 0) * time * 7, UnityEngine.ForceMode.Impulse)
end

function PlayerMediator:OnCollisionEnter(collision)
    local body = {player = self.playerProxy.player, hit = collision.collider.gameObject}
    LuaFacade.SendNotification("PlayerJumped", body)
end

return PlayerMediator
